using TradingSystem.Dashboard.Api.Exceptions;
using TradingSystem.Dashboard.Contracts.Models.Backtesting;
using TradingSystem.Dashboard.Contracts.Models.Bots;
using TradingSystem.Dashboard.Contracts.Models.Enums;
using TradingSystem.Dashboard.Contracts.Models.Optimization;
using TradingSystem.ReplayEngine.Models;
using TradingSystem.ReplayEngine.Models.Enums;

namespace TradingSystem.Dashboard.Api.Validation;

public static class RequestValidation
{
    private static readonly HashSet<string> AllowedIntervals = new(
        ["1m", "3m", "5m", "15m", "30m", "1h", "2h", "4h", "6h", "8h", "12h", "1d"],
        StringComparer.OrdinalIgnoreCase);

    public static void Validate(
        UpdateBotConfigurationRequest request)
    {
        var errors = NewErrors();

        Positive(
            errors,
            nameof(request.ExpectedVersion),
            request.ExpectedVersion);

        Required(
            errors,
            nameof(request.StrategyType),
            request.StrategyType,
            128);

        Symbol(errors, request.Symbol);

        Required(
            errors,
            nameof(request.SignalSource),
            request.SignalSource,
            64);

        Positive(
            errors,
            nameof(request.Quantity),
            request.Quantity);

        Range(
            errors,
            nameof(request.Leverage),
            request.Leverage,
            1,
            125);

        PositiveWhenPresent(
            errors,
            nameof(request.PriceDistance),
            request.PriceDistance);

        PositiveWhenPresent(
            errors,
            nameof(request.ProfitDistance),
            request.ProfitDistance);

        if (request.OrderSideLimit is <= 0 or > 100)
        {
            Add(
                errors,
                nameof(request.OrderSideLimit),
                "OrderSideLimit must be between 1 and 100.");
        }

        Range(
            errors,
            nameof(request.CooldownSeconds),
            request.CooldownSeconds,
            0,
            86_400);

        Required(
            errors,
            nameof(request.Reason),
            request.Reason,
            500);

        Throw(errors);
    }

    public static void Validate(
        BotCommandRequest request)
    {
        var errors = NewErrors();

        Required(
            errors,
            nameof(request.Reason),
            request.Reason,
            500);

        var dangerous =
            request.Command is BotCommandType.EmergencyStop
                or BotCommandType.ClosePosition
                or BotCommandType.CancelTakeProfit
                or BotCommandType.RecreateTakeProfit;

        if (dangerous && !request.Confirmed)
        {
            Add(
                errors,
                nameof(request.Confirmed),
                "Confirmation is required for this command.");
        }

        var positionCommand =
            request.Command is BotCommandType.ClosePosition
                or BotCommandType.CancelTakeProfit
                or BotCommandType.RecreateTakeProfit;

        if (positionCommand &&
            string.IsNullOrWhiteSpace(request.PositionId))
        {
            Add(
                errors,
                nameof(request.PositionId),
                "PositionId is required for a position command.");
        }

        Throw(errors);
    }

    public static void Validate(
        BacktestRequest request)
    {
        var errors = NewErrors();

        Bot(errors, request.BotName);
        Symbol(errors, request.Symbol);
        Period(errors, request.FromUtc, request.ToUtc);

        Positive(
            errors,
            nameof(request.InitialBalance),
            request.InitialBalance);

        Range(
            errors,
            nameof(request.CommissionPercent),
            request.CommissionPercent,
            0,
            10);

        Range(
            errors,
            nameof(request.SlippagePercent),
            request.SlippagePercent,
            0,
            10);

        Required(
            errors,
            nameof(request.SignalSource),
            request.SignalSource,
            64);

        if (request.Parameters is null)
        {
            Add(
                errors,
                nameof(request.Parameters),
                "Parameters are required.");
        }
        else if (request.Parameters.Count > 100)
        {
            Add(
                errors,
                nameof(request.Parameters),
                "No more than 100 parameters are allowed.");
        }

        Throw(errors);
    }

    public static void Validate(
        OptimizationRequest request)
    {
        var errors = NewErrors();

        Bot(errors, request.BotName);
        Symbol(errors, request.Symbol);
        Period(errors, request.FromUtc, request.ToUtc);

        Positive(
            errors,
            nameof(request.InitialBalance),
            request.InitialBalance);

        Required(
            errors,
            nameof(request.SignalSource),
            request.SignalSource,
            64);

        Range(
            errors,
            nameof(request.TopResults),
            request.TopResults,
            1,
            1000);

        if (request.Ranges is null ||
            request.Ranges.Count is 0 or > 20)
        {
            Add(
                errors,
                nameof(request.Ranges),
                "Between 1 and 20 ranges are required.");
        }
        else
        {
            var duplicateNames =
                request.Ranges
                    .Where(range =>
                        !string.IsNullOrWhiteSpace(range.Name))
                    .GroupBy(
                        range => range.Name,
                        StringComparer.OrdinalIgnoreCase)
                    .Where(group => group.Count() > 1)
                    .Select(group => group.Key)
                    .ToArray();

            if (duplicateNames.Length > 0)
            {
                Add(
                    errors,
                    nameof(request.Ranges),
                    $"Range names must be unique: {string.Join(", ", duplicateNames)}.");
            }

            foreach (var range in request.Ranges)
            {
                if (string.IsNullOrWhiteSpace(range.Name) ||
                    range.Name.Length > 100)
                {
                    Add(
                        errors,
                        nameof(request.Ranges),
                        "Every range needs a valid name.");
                }

                if (range.Step <= 0)
                {
                    Add(
                        errors,
                        nameof(request.Ranges),
                        $"Step for {range.Name} must be greater than zero.");
                }

                if (range.To < range.From)
                {
                    Add(
                        errors,
                        nameof(request.Ranges),
                        $"To for {range.Name} must be greater than or equal to From.");
                }
            }
        }

        if (request.WalkForward &&
            (request.TrainBars is <= 0 ||
             request.TestBars is <= 0 ||
             request.StepBars is <= 0))
        {
            Add(
                errors,
                nameof(request.WalkForward),
                "Positive TrainBars, TestBars and StepBars are required for walk-forward optimization.");
        }

        Throw(errors);
    }

    public static void Validate(
        CreateReplayRequest request)
    {
        var errors = NewErrors();

        if (string.IsNullOrWhiteSpace(request.Name) ||
            request.Name.Length > 150)
        {
            Add(
                errors,
                nameof(request.Name),
                "Replay name is required and must be at most 150 characters.");
        }

        if (request.FromGlobalPosition.HasValue &&
            request.ToGlobalPosition.HasValue &&
            request.FromGlobalPosition >
            request.ToGlobalPosition)
        {
            Add(
                errors,
                "range",
                "FromGlobalPosition must not be greater than ToGlobalPosition.");
        }

        if (request.FromUtc.HasValue &&
            request.ToUtc.HasValue &&
            request.FromUtc >= request.ToUtc)
        {
            Add(
                errors,
                "range",
                "FromUtc must be earlier than ToUtc.");
        }

        if (request.Mode == ReplayMode.StrategyComparison &&
            (string.IsNullOrWhiteSpace(
                 request.CandidateStrategyPluginId) ||
             string.IsNullOrWhiteSpace(
                 request.CandidateStrategyVersion)))
        {
            Add(
                errors,
                "candidateStrategy",
                "Strategy comparison requires candidate plugin id and version.");
        }

        Throw(errors);
    }

    public static void ValidateBotName(
        string botName)
    {
        var errors = NewErrors();

        Bot(errors, botName);

        Throw(errors);
    }

    public static void ValidateChart(
        string symbol,
        string interval,
        DateTime fromUtc,
        DateTime toUtc)
    {
        var errors = NewErrors();

        Symbol(errors, symbol);
        Period(errors, fromUtc, toUtc);

        if (string.IsNullOrWhiteSpace(interval) ||
            !AllowedIntervals.Contains(interval.Trim()))
        {
            Add(
                errors,
                nameof(interval),
                "Unsupported candle interval.");
        }

        if (toUtc - fromUtc > TimeSpan.FromDays(366))
        {
            Add(
                errors,
                nameof(toUtc),
                "Chart range cannot exceed 366 days.");
        }

        Throw(errors);
    }

    public static int PageSize(
        int value,
        int defaultValue = 100,
        int maximum = 500)
    {
        if (value == 0)
            return defaultValue;

        if (value < 1 ||
            value > maximum)
        {
            throw new ApiValidationException(
                new Dictionary<string, string[]>
                {
                    ["take"] =
                        [$"take must be between 1 and {maximum}."]
                });
        }

        return value;
    }

    public static int Skip(
        int value)
    {
        if (value < 0)
        {
            throw new ApiValidationException(
                new Dictionary<string, string[]>
                {
                    ["skip"] =
                        ["skip cannot be negative."]
                });
        }

        return value;
    }

    private static Dictionary<string, List<string>>
        NewErrors() =>
        new(StringComparer.OrdinalIgnoreCase);

    private static void Add(
        Dictionary<string, List<string>> errors,
        string key,
        string value) =>
        (errors.TryGetValue(key, out var list)
            ? list
            : errors[key] = [])
        .Add(value);

    private static void Throw(
        Dictionary<string, List<string>> errors)
    {
        if (errors.Count > 0)
        {
            throw new ApiValidationException(
                errors.ToDictionary(
                    item => item.Key,
                    item => item.Value.ToArray()));
        }
    }

    private static void Required(
        Dictionary<string, List<string>> errors,
        string name,
        string? value,
        int max)
    {
        if (string.IsNullOrWhiteSpace(value))
            Add(errors, name, $"{name} is required.");
        else if (value.Length > max)
            Add(
                errors,
                name,
                $"{name} cannot exceed {max} characters.");
    }

    private static void Positive(
        Dictionary<string, List<string>> errors,
        string name,
        decimal value)
    {
        if (value <= 0)
            Add(
                errors,
                name,
                $"{name} must be greater than zero.");
    }

    private static void Positive(
        Dictionary<string, List<string>> errors,
        string name,
        long value)
    {
        if (value <= 0)
            Add(
                errors,
                name,
                $"{name} must be greater than zero.");
    }

    private static void PositiveWhenPresent(
        Dictionary<string, List<string>> errors,
        string name,
        decimal? value)
    {
        if (value.HasValue &&
            value.Value <= 0)
        {
            Add(
                errors,
                name,
                $"{name} must be greater than zero.");
        }
    }

    private static void Range(
        Dictionary<string, List<string>> errors,
        string name,
        decimal value,
        decimal min,
        decimal max)
    {
        if (value < min ||
            value > max)
        {
            Add(
                errors,
                name,
                $"{name} must be between {min} and {max}.");
        }
    }

    private static void Range(
        Dictionary<string, List<string>> errors,
        string name,
        int value,
        int min,
        int max)
    {
        if (value < min ||
            value > max)
        {
            Add(
                errors,
                name,
                $"{name} must be between {min} and {max}.");
        }
    }

    private static void Symbol(
        Dictionary<string, List<string>> errors,
        string symbol)
    {
        if (string.IsNullOrWhiteSpace(symbol) ||
            symbol.Length > 24 ||
            !symbol.All(char.IsLetterOrDigit))
        {
            Add(
                errors,
                nameof(symbol),
                "Symbol must contain 1-24 letters or digits.");
        }
    }

    private static void Bot(
        Dictionary<string, List<string>> errors,
        string botName)
    {
        if (string.IsNullOrWhiteSpace(botName) ||
            botName.Length > 64 ||
            !botName.All(
                character =>
                    char.IsLetterOrDigit(character) ||
                    character is '-' or '_'))
        {
            Add(
                errors,
                nameof(botName),
                "Bot name contains invalid characters.");
        }
    }

    private static void Period(Dictionary<string, List<string>> errors, DateTime from, DateTime to)
    {
        if (from >= to)
            Add(errors, nameof(to), "ToUtc must be later than FromUtc.");

        if (to - from > TimeSpan.FromDays(3660))
            Add(errors, nameof(to), "Requested period is too large.");
    }
}
