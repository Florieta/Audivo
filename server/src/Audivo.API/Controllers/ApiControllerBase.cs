using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace Audivo.API.Controllers;

/// <summary>
/// Shared base class for all Audivo API controllers.
/// Applies common routing, versioning, and provides helper utilities available to every controller.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public abstract class ApiControllerBase : ControllerBase
{
    /// <summary>
    /// Resolves the authenticated user's ID from the JWT claims.
    /// </summary>
    /// <exception cref="UnauthorizedAccessException">
    /// Thrown if the claim is absent — this should never occur on <c>[Authorize]</c>-protected endpoints.
    /// </exception>
    protected string GetUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException();
}
