namespace Audivo.Application.Exceptions;

/// <summary>
/// Thrown when a requested resource is not found.
/// Mapped to HTTP 404 by the global exception handler.
/// </summary>
public sealed class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }

    public NotFoundException(string name, object key)
        : base($"{name} with key '{key}' was not found.") { }
}
