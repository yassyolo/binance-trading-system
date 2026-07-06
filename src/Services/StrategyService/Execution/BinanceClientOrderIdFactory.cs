namespace StrategyService.Execution;

public static class BinanceClientOrderIdFactory
{
    public static string CreateShortId()
        => Guid.NewGuid().ToString("N")[..8];

    public static string Create(string botName, string prefix, string shortId)
    {
        var shortBot = botName.Length > 8 ? botName[..8] : botName;
        var value = $"{shortBot}_{prefix}_{shortId}";

        return value[..Math.Min(32, value.Length)];
    }
}
