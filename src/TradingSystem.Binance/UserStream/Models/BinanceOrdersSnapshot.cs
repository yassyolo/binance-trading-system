using System.Text.Json;

namespace TradingSystem.Binance.UserStream.Models;

public sealed record BinanceOrdersSnapshot(JsonElement[] NormalOrders, JsonElement[] AlgoOrders);

