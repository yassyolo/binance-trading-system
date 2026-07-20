namespace TradingSystem.Binance;

public sealed class BinanceApiException : Exception
{
    public BinanceApiException(int statusCode,  string responseBody)
        : base($"Binance request failed. Status = {statusCode},  Body = {responseBody}")
    {
        StatusCode  =  statusCode;
        ResponseBody  =  responseBody;
    }

    public int StatusCode {  get;  }
    public string ResponseBody {  get;  }
}
