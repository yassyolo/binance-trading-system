namespace TradingViewWebhookService.Models;

public sealed record PublishResult(
    bool Succeeded,
    int StatusCode,
    string? Error,
    TradingViewSignalResponse? Response)
{
    public static PublishResult Rejected(int statusCode, string error)
        => new(false, statusCode, error, null);

    public static PublishResult Accepted(TradingViewSignalResponse response)
        => new(true, StatusCodes.Status202Accepted, null, response);
}
