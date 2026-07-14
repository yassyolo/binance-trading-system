namespace UserStreamService.Configuration;

public sealed class UserStreamOptions
{
    public const string SectionName = "UserStream";

    public bool PublishRaw { get; set; } = true;
    public int ReconnectDelaySeconds { get; set; } = 5;
    public int ListenKeyKeepAliveSeconds { get; set; } = 1800;
    public int MinDowntimeForHealingSeconds { get; set; } = 30;
    public int HealingCooldownSeconds { get; set; } = 60;
    public string HealingSymbol { get; set; } = "BTCUSDC";
    public int KeepAliveIntervalSeconds { get; set; } = 30;
    public int ReceiveBufferSizeBytes { get; set; } = 32 * 1024;
}
