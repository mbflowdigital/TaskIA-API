using Microsoft.AspNetCore.Mvc;

namespace Application.Controllers;

/// <summary>
/// Controller Health Check
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class HealthController : ControllerBase
{
    /// <summary>
    /// Verifica se a API está funcionando
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public IActionResult Get()
    {
        return Ok(new 
        { 
            Status = "Online",
            Message = "TaskIA API está funcionando!",
            Timestamp = DateTime.UtcNow
        });
    }
}
