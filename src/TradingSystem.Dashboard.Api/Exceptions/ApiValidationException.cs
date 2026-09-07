namespace TradingSystem.Dashboard.Api.Exceptions;

public sealed class ApiValidationException(IReadOnlyDictionary<string,  string[]> errors) : Exception("One or more validation errors occurred.")
{
    public IReadOnlyDictionary<string, string[]> Errors { get; } = errors;
}