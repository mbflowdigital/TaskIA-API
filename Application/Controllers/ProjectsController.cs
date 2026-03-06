using Application.Core.DTOs.Projects;
using Application.Core.Interfaces.Services;
using Domain.Common;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Application.Controllers;

/// <summary>
/// Controller de Projetos
/// Gerencia opera��es CRUD de projetos
/// </summary>
[ApiController]
[Authorize]
[Route("api/[controller]")]
[Produces("application/json")]
public class ProjectsController : ControllerBase
{
    private readonly IProjectService _projectService;

    public ProjectsController(IProjectService projectService)
    {
        _projectService = projectService;
    }

    /// <summary>
    /// Cria um novo projeto
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(Result<ProjectDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        [FromBody] CreateProjectRequest request,
        CancellationToken cancellationToken)
    {
        var (actorUserId, actorRole) = GetActorContext();
        var result = await _projectService.CreateAsync(request, actorUserId, actorRole, cancellationToken);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Lista todos os projetos ativos
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(Result<IEnumerable<ProjectDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var (actorUserId, actorRole) = GetActorContext();
        var result = await _projectService.GetAllAsync(actorUserId, actorRole, cancellationToken);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Busca projeto por ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(Result<ProjectDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var (actorUserId, actorRole) = GetActorContext();
        var result = await _projectService.GetByIdAsync(id, actorUserId, actorRole, cancellationToken);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Atualiza um projeto
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(Result<ProjectDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateProjectRequest request,
        CancellationToken cancellationToken)
    {
        if (id != request.Id)
        {
            return BadRequest(Result.Failure("ID da URL diferente do ID do corpo da requisi��o"));
        }

        var (actorUserId, actorRole) = GetActorContext();
        var result = await _projectService.UpdateAsync(request, actorUserId, actorRole, cancellationToken);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Desativa um projeto (soft delete)
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(Result), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        var (actorUserId, actorRole) = GetActorContext();
        var result = await _projectService.DeleteAsync(id, actorUserId, actorRole, cancellationToken);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Busca projetos por nome
    /// </summary>
    [HttpGet("search/name")]
    [ProducesResponseType(typeof(Result<IEnumerable<ProjectDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchByName(
        [FromQuery] string name,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return BadRequest(Result.Failure("Nome � obrigat�rio"));
        }

        var (actorUserId, actorRole) = GetActorContext();
        var result = await _projectService.FindByNameAsync(name, actorUserId, actorRole, cancellationToken);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Busca projetos por status
    /// </summary>
    [HttpGet("search/status")]
    [ProducesResponseType(typeof(Result<IEnumerable<ProjectDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchByStatus(
        [FromQuery] string status,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return BadRequest(Result.Failure("Status � obrigat�rio"));
        }

        var (actorUserId, actorRole) = GetActorContext();
        var result = await _projectService.FindByStatusAsync(status, actorUserId, actorRole, cancellationToken);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Verifica se nome de projeto j� est� cadastrado
    /// </summary>
    [HttpGet("check-name")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> CheckName(
        [FromQuery] string name,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return BadRequest(new { exists = false, message = "Nome inv�lido" });
        }

        var exists = await _projectService.NameExistsAsync(name, cancellationToken);
        return Ok(new { exists, message = exists ? "Nome j� cadastrado" : "Nome dispon�vel" });
    }

    /// <summary>
    /// Alterna status do projeto entre Active e Inactive automaticamente
    /// </summary>
    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(typeof(Result<ProjectDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ToggleStatus(
        Guid id,
        CancellationToken cancellationToken)
    {
        var (actorUserId, actorRole) = GetActorContext();
        var result = await _projectService.ToggleStatusAsync(id, actorUserId, actorRole, cancellationToken);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    private (Guid? ActorUserId, UserRole? ActorRole) GetActorContext()
    {
        var userIdRaw =
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
            User.FindFirst("sub")?.Value ??
            Request.Headers["X-User-Id"].FirstOrDefault();

        var roleRaw =
            User.FindFirst(ClaimTypes.Role)?.Value ??
            Request.Headers["X-User-Role"].FirstOrDefault();

        Guid? actorUserId = null;
        if (!string.IsNullOrWhiteSpace(userIdRaw) && Guid.TryParse(userIdRaw, out var parsedId))
            actorUserId = parsedId;

        UserRole? actorRole = null;
        if (!string.IsNullOrWhiteSpace(roleRaw) && Enum.TryParse<UserRole>(roleRaw, true, out var parsedRole))
            actorRole = parsedRole;

        return (actorUserId, actorRole);
    }
}
