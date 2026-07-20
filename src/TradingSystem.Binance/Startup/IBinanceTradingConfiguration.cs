namespace TradingSystem.Binance.Startup;

public interface IBinanceTradingConfiguration
{
    string BotName {  get;  }
    string Symbol {  get;  }
    int Leverage {  get;  }
}
