using Microsoft.Extensions.Options;
using TradingSystem.Application.Positions.Contracts;
using TradingSystem.BotRuntime.Configuration.Contracts;
using TradingSystem.Domain.Positions;
using TradingSystem.Reconciliation.Configuration;
using TradingSystem.Reconciliation.Contracts;
using TradingSystem.Reconciliation.Models;
using TradingSystem.Reconciliation.Models.Enums;

namespace TradingSystem.Reconciliation.Services;

public sealed class PositionReconciliationService(
	IOptions<ReconciliationOptions> options,
	IPositionStore localStore,
	IExchangeStateProvider exchange,
	IHealingActionExecutor healer,
	IReconciliationFindingStore findingStore,
	IBotRuntimeConfigurationProvider runtimeConfigurations)
{
	private readonly ReconciliationOptions _options = options.Value;

	public async Task<ReconciliationRunResult> RunAsync(CancellationToken ct)
	{
		var startedAtUtc = DateTime.UtcNow;
		var findings = new List<ReconciliationFinding>();
		
		var liveBots = await ResolveLiveBotsAsync(ct);
		if (liveBots.Count == 0)
		{
			var emptyResult = new ReconciliationRunResult(startedAtUtc, DateTime.UtcNow, findings, 0);

			await findingStore.SaveRunAsync(emptyResult, ct);
			return emptyResult;
		}

		foreach (var symbol in _options.Symbols.Distinct(StringComparer.OrdinalIgnoreCase))
		{
			ct.ThrowIfCancellationRequested();

			var remote = await exchange.GetAsync(symbol, ct);
			var localPositions = await LoadLocalPositionsAsync(symbol, liveBots, ct);

			DetectPositionQuantityFindings(symbol, localPositions, remote.Positions, findings);
			DetectLocalPositionAndProtectiveOrderFindings(localPositions, remote, findings);
			DetectOrphanOrders(symbol, localPositions, remote.Orders, findings);
		}

		var healedCount = await ExecuteAllowedHealingActionsAsync(findings, ct);
		
		var result = new ReconciliationRunResult(startedAtUtc, DateTime.UtcNow, findings, healedCount);
		await findingStore.SaveRunAsync(result, ct);
		
		return result;
	}

	private async Task<IReadOnlyCollection<string>> ResolveLiveBotsAsync(CancellationToken ct)
	{
		var result = new List<string>();

		foreach (var bot in _options.Bots.Distinct(StringComparer.OrdinalIgnoreCase))
		{
			ct.ThrowIfCancellationRequested();

			var config = await runtimeConfigurations.GetAsync(bot, ct);
			if (config is null)
				continue;

			if (config.Environment.Equals("Demo", StringComparison.OrdinalIgnoreCase) ||
				config.Environment.Equals("Production", StringComparison.OrdinalIgnoreCase))
			{
				result.Add(bot);
			}
		}

		return result;
	}

	private async Task<IReadOnlyList<BotPosition>> LoadLocalPositionsAsync(string symbol, IReadOnlyCollection<string> liveBots, CancellationToken ct)
	{
		var result = new List<BotPosition>();

		foreach (var bot in liveBots)
		{
			ct.ThrowIfCancellationRequested();
			
			var positions = await localStore.GetAllAsync(bot, ct);

			result.AddRange(positions.Where(p => !p.Closed && p.Symbol.Equals(symbol, StringComparison.OrdinalIgnoreCase)));
		}

		return result;
	}

	private void DetectPositionQuantityFindings(
		string symbol,
		IReadOnlyCollection<BotPosition> localPositions,
		IReadOnlyCollection<ExchangePositionSnapshot> remotePositions,
		ICollection<ReconciliationFinding> findings)
	{
		foreach (var side in new[] { "Long", "Short" })
		{
			var localForSide = localPositions
				.Where(position => position.Side.ToString().Equals(side, StringComparison.OrdinalIgnoreCase))
				.ToArray();

			var localQuantity = localForSide.Sum(position => Math.Abs(position.RemainingQuantity));
			var remoteQuantity = remotePositions
				.Where(p =>
					p.Symbol.Equals(symbol, StringComparison.OrdinalIgnoreCase) &&
					p.Side.Equals(side, StringComparison.OrdinalIgnoreCase))
				.Sum(position => Math.Abs(position.Quantity));

			if (localQuantity == 0 && remoteQuantity > _options.QuantityTolerance)
			{
				findings.Add(new ReconciliationFinding(
					Guid.NewGuid(), DateTime.UtcNow, "UNKNOWN", symbol, null,
					ReconciliationFindingType.OrphanExchangePosition,
					ReconciliationSeverity.Critical,
					$"Exchange {side} quantity {remoteQuantity} has no local owner.",
					HealingActionType.RequestManualReview,
					false));
				continue;
			}

			if (Math.Abs(remoteQuantity - localQuantity) <= _options.QuantityTolerance)
				continue;

			var botNames = string.Join(
				", ",
				localForSide.Select(x => x.BotName).Distinct(StringComparer.OrdinalIgnoreCase));

			findings.Add(new ReconciliationFinding(
				Guid.NewGuid(),
				DateTime.UtcNow,
				string.IsNullOrWhiteSpace(botNames) ? "UNKNOWN" : botNames,
				symbol,
				null,
				ReconciliationFindingType.QuantityMismatch,
				ReconciliationSeverity.Critical,
				$"Aggregated {side} quantity mismatch. Local = {localQuantity}, exchange = {remoteQuantity}.",
				HealingActionType.RequestManualReview,
				false));
		}
	}

	private void DetectLocalPositionAndProtectiveOrderFindings(
		IReadOnlyCollection<BotPosition> localPositions,
		ExchangeStateSnapshot remote,
		ICollection<ReconciliationFinding> findings)
	{
		var ordersByClientId = remote.Orders
			.Where(order => !string.IsNullOrWhiteSpace(order.ClientOrderId))
			.GroupBy(order => order.ClientOrderId, StringComparer.OrdinalIgnoreCase)
			.ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

		foreach (var position in localPositions)
		{
			var hasRelatedOrder = EnumerateClientOrderIds(position).Any(ordersByClientId.ContainsKey);
			var hasRemoteSidePosition = remote.Positions.Any(remotePosition =>
				remotePosition.Symbol.Equals(position.Symbol, StringComparison.OrdinalIgnoreCase) &&
				remotePosition.Side.Equals(position.Side.ToString(), StringComparison.OrdinalIgnoreCase) &&
				Math.Abs(remotePosition.Quantity) > _options.QuantityTolerance);

			if (!hasRemoteSidePosition && !hasRelatedOrder)
			{
				findings.Add(New(
					position,
					ReconciliationFindingType.StaleLocalPosition,
					ReconciliationSeverity.Warning,
					"Local p has no exchange p or open orders.",
					HealingActionType.DeleteStaleLocalPosition,
					_options.AutoHealStaleLocalPositions));
			}

			if (!position.TpExecuted &&
				!string.IsNullOrWhiteSpace(position.TpClientId) &&
				!ordersByClientId.ContainsKey(position.TpClientId))
			{
				findings.Add(New(
					position,
					ReconciliationFindingType.MissingTakeProfit,
					ReconciliationSeverity.Critical,
					"Expected take-profit order is missing.",
					HealingActionType.RecreateTakeProfit,
					_options.AutoHealProtectiveOrders));
			}

			if (position.ProtectiveActive &&
				!position.SlExecuted &&
				!string.IsNullOrWhiteSpace(position.SlClientId) &&
				!ordersByClientId.ContainsKey(position.SlClientId))
			{
				findings.Add(New(
					position,
					ReconciliationFindingType.MissingStopLoss,
					ReconciliationSeverity.Critical,
					"Expected stop-loss order is missing.",
					HealingActionType.RecreateStopLoss,
					_options.AutoHealProtectiveOrders));
			}
		}
	}

	private static void DetectOrphanOrders(
		string symbol,
		IReadOnlyCollection<BotPosition> localPositions,
		IReadOnlyCollection<ExchangeOrderSnapshot> remoteOrders,
		ICollection<ReconciliationFinding> findings)
	{
		var knownClientOrderIds = localPositions
			.SelectMany(EnumerateClientOrderIds)
			.ToHashSet(StringComparer.OrdinalIgnoreCase);

		foreach (var order in remoteOrders.Where(order =>
					 order.ClientOrderId.StartsWith("BOT", StringComparison.OrdinalIgnoreCase) &&
					 !knownClientOrderIds.Contains(order.ClientOrderId)))
		{
			findings.Add(new ReconciliationFinding(
				Guid.NewGuid(), DateTime.UtcNow, ResolveBot(order.ClientOrderId), symbol, null,
				ReconciliationFindingType.OrphanExchangeOrder,
				ReconciliationSeverity.Critical,
				$"Exchange order '{order.ClientOrderId}' is not represented in local state.",
				HealingActionType.RequestManualReview,
				false));
		}
	}

	private async Task<int> ExecuteAllowedHealingActionsAsync(
		IEnumerable<ReconciliationFinding> findings,
		CancellationToken ct)
	{
		var healedCount = 0;

		foreach (var finding in findings.Where(finding => finding.AutoHealAllowed))
		{
			ct.ThrowIfCancellationRequested();

			if (await healer.ExecuteAsync(finding, ct))
				healedCount++;
		}

		return healedCount;
	}

	private static IEnumerable<string> EnumerateClientOrderIds(BotPosition position)
	{
		var values = new[]
		{
			position.ParentClientId,
			position.TpClientId,
			position.SlClientId,
			position.Stop3ClientId,
			position.CloseClientId
		};

		return values.Where(value => !string.IsNullOrWhiteSpace(value))!;
	}

	private static string ResolveBot(string clientOrderId)
	{
		var separator = clientOrderId.IndexOf('_');
		return separator > 0
			? clientOrderId[..separator]
			: clientOrderId.Length >= 7
				? clientOrderId[..7]
				: "UNKNOWN";
	}

	private static ReconciliationFinding New(
		BotPosition position,
		ReconciliationFindingType type,
		ReconciliationSeverity severity,
		string details,
		HealingActionType action,
		bool autoHealAllowed)
		=> new(
			Guid.NewGuid(),
			DateTime.UtcNow,
			position.BotName,
			position.Symbol,
			position.ShortId,
			type,
			severity,
			details,
			action,
			autoHealAllowed);
}
