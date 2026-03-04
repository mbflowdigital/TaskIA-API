using Application.Core.DTOs.Projects;
using Application.Core.Interfaces.Services;
using Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace Application.Controllers;

/// <summary>
/// Controller de Projetos
/// Gerencia operações CRUD de projetos
/// </summary>
[ApiController]
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
        var result = await _projectService.CreateAsync(request, cancellationToken);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Lista todos os projetos ativos
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(Result<IEnumerable<ProjectDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await _projectService.GetAllAsync(cancellationToken);
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
        var result = await _projectService.GetByIdAsync(id, cancellationToken);
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
            return BadRequest(Result.Failure("ID da URL diferente do ID do corpo da requisição"));
        }

        var result = await _projectService.UpdateAsync(request, cancellationToken);
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
        var result = await _projectService.DeleteAsync(id, cancellationToken);
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
            return BadRequest(Result.Failure("Nome é obrigatório"));
        }

        var result = await _projectService.FindByNameAsync(name, cancellationToken);
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
            return BadRequest(Result.Failure("Status é obrigatório"));
        }

        var result = await _projectService.FindByStatusAsync(status, cancellationToken);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Verifica se nome de projeto já está cadastrado
    /// </summary>
    [HttpGet("check-name")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> CheckName(
        [FromQuery] string name,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return BadRequest(new { exists = false, message = "Nome inválido" });
        }

        var exists = await _projectService.NameExistsAsync(name, cancellationToken);
        return Ok(new { exists, message = exists ? "Nome já cadastrado" : "Nome disponível" });
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
        var result = await _projectService.ToggleStatusAsync(id, cancellationToken);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }
}
