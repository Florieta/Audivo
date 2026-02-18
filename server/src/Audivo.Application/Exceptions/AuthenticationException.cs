namespace Audivo.Application.Exceptions;

/// <summary>
/// Thrown when authentication fails (invalid credentials, expired token, etc.).
/// Mapped to HTTP 401 by the global exception handler.
/// </summary>
public sealed class AuthenticationException : Exception
{
    public AuthenticationException(string message) : base(message) { }
}
