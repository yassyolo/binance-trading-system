namespace TradingSystem.Dashboard.Api.Hardening;

public sealed class ApiValidationException(IReadOnlyDictionary<string,  string[]> errors)
    : Exception("One or more validation errors occurred.")
{
    public IReadOnlyDictionary<string,  string[]> Errors { get; }  =  errors;
}

public sealed class ApiConflictException(string message) : Exception(message);
public sealed class ApiNotFoundException(string message) : Exception(message);
