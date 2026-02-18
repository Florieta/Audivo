namespace Audivo.Application.Exceptions;

/// <summary>
/// Thrown when a validation rule fails at the application layer.
/// Mapped to HTTP 400 by the global exception handler.
/// </summary>
public sealed class ValidationException : Exception
{
    public IDictionary<string, string[]> Errors { get; }

    public ValidationException(IDictionary<string, string[]> errors)
        : base("One or more validation errors occurred.")
    {
        Errors = errors;
    }

    public ValidationException(string field, string message)
        : base("One or more validation errors occurred.")
    {
        Errors = new Dictionary<string, string[]>
        {
            { field, [message] }
        };
    }
}
