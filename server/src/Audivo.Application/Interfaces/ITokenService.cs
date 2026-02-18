using System.Security.Claims;
using Audivo.Core.Entities;

namespace Audivo.Application.Interfaces;

/// <summary>
/// Generates and validates tokens (access + refresh).
/// Implemented in Infrastructure.
/// </summary>
public interface ITokenService
{
    string GenerateAccessToken(ApplicationUser user, IList<string> roles);

    string GenerateRefreshToken();

    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
}
