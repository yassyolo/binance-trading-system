namespace StrategyService.Execution;

public static class BinanceClientOrderIdFactory
{
    public static string CreateShortId()
        => Guid.NewGuid().ToString("N")[..8];

    public static string Create(
        string botName,
        string prefix,
        string shortId,
        int? sequence = null)
    {
        var shortBot = botName.Length > 8 ? botName[..8] : botName;
        var suffix = sequence.HasValue ? $"_T{sequence.Value}" : string.Empty;
        var value = $"{shortBot}_{prefix}_{shortId}{suffix}";

        return value[..Math.Min(32, value.Length)];
    }
}
