namespace TradingSystem.Dashboard.Api.Exceptions;

public sealed class ApiNotFoundException(string message) : Exception(message);