using System.Net;

namespace TradingSystem.Binance.Exceptions;

public sealed class BinanceApiException : Exception
{
    public BinanceApiException(HttpStatusCode statusCode,  string responseBody, string? operation = null)
        : base($"Binance request failed. Operation: {operation}, Status = {statusCode}, Body = {responseBody}")
    {
        StatusCode = statusCode;
        ResponseBody = responseBody;
    }

    public HttpStatusCode StatusCode { get; }
    
    public string ResponseBody { get; }
}
