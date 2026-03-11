using Application.Core.DTOs.Positions;
using Application.Core.Interfaces.Services;
using Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Application.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
[Produces("application/json")]
public class PositionsController : ControllerBase
{
    private readonly IPositionService _positionService;

    public PositionsController(IPositionService positionService)
    {
        _positionService = positionService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(Result<IEnumerable<PositionDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await _positionService.GetAllAsync(cancellationToken);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }
}