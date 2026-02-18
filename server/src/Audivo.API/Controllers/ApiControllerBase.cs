using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace Audivo.API.Controllers;

/// <summary>
/// Base controller providing common functionality for all API controllers.
/// All controllers inherit from this to get consistent routing and behavior.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public abstract class ApiControllerBase : ControllerBase
{
}
