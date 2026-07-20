namespace UserStreamService.Configuration;

public sealed class UserStreamServiceOptions
{
    public const string SectionName = "UserStreamService";

    public bool PublishRaw {  get;  set; } = true;

    public int ReconnectDelaySeconds {  get;  set; } = 5;

    public int MinDowntimeForHealingSeconds {  get;  set; } = 30;

    public int HealingCooldownSeconds {  get;  set; } = 60;

    public IReadOnlyCollection<string> HealingSymbols {  get;  set; } = ["BTCUSDC"];
}
