namespace TradingSystem.Dashboard.Api.Exceptions;

public sealed class ApiConflictException(string message) : Exception(message);