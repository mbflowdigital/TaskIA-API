using Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Application.Controllers;

/// <summary>
/// Controller Health Check
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class HealthController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public HealthController(ApplicationDbContext context)
    {
        _context = context;
    }

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

    /// <summary>
    /// Verifica conexão com o banco de dados
    /// </summary>
    [HttpGet("database")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CheckDatabase()
    {
        try
        {
            // Tenta fazer uma query simples no banco
            var canConnect = await _context.Database.CanConnectAsync();
            
            if (canConnect)
            {
                var userCount = await _context.Users.CountAsync();
                
                return Ok(new 
                { 
                    Status = "Connected",
                    Message = "Conexão com banco de dados OK",
                    DatabaseName = _context.Database.GetDbConnection().Database,
                    UserCount = userCount,
                    Timestamp = DateTime.UtcNow
                });
            }
            else
            {
                return StatusCode(500, new 
                { 
                    Status = "Error",
                    Message = "Não foi possível conectar ao banco de dados",
                    Timestamp = DateTime.UtcNow
                });
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, new 
            { 
                Status = "Error",
                Message = "Erro ao verificar conexão com banco de dados",
                Error = ex.Message,
                InnerException = ex.InnerException?.Message,
                StackTrace = ex.StackTrace,
                Timestamp = DateTime.UtcNow
            });
        }
    }
}
