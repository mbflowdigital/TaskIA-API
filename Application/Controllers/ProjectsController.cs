using Application.Core.DTOs.Projects.Requests;
using Application.Core.DTOs.Projects.Responses;
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
    [HttpGet("{projectId:guid}")]
    [ProducesResponseType(typeof(Result<ProjectDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetById(
        Guid projectId,
        CancellationToken cancellationToken)
    {
        var (actorUserId, actorRole) = GetActorContext();
        var result = await _projectService.GetByIdAsync(projectId, actorUserId, actorRole, cancellationToken);
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
    [HttpDelete("{projectId:guid}")]
    [ProducesResponseType(typeof(Result), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Delete(
        Guid projectId,
        CancellationToken cancellationToken)
    {
        var (actorUserId, actorRole) = GetActorContext();
        var result = await _projectService.DeleteAsync(projectId, actorUserId, actorRole, cancellationToken);
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
    [HttpPatch("{projectId:guid}/status")]
    [ProducesResponseType(typeof(Result<ProjectDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ToggleStatus(
        Guid projectId,
        CancellationToken cancellationToken)
    {
        var (actorUserId, actorRole) = GetActorContext();
        var result = await _projectService.ToggleStatusAsync(projectId, actorUserId, actorRole, cancellationToken);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    

    /// <summary>
    /// Adiciona um membro ao projeto
    /// </summary>
    [HttpPost("{projectId:guid}/members")]
    [ProducesResponseType(typeof(Result<ProjectMemberDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AddMember(
        Guid projectId,
        [FromBody] CreateProjectMemberRequest request,
        CancellationToken cancellationToken)
    {
        var (actorUserId, actorRole) = GetActorContext();
        var result = await _projectService.AddMemberAsync(projectId, request, actorUserId, actorRole, cancellationToken);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Remove um membro do projeto
    /// </summary>
    [HttpDelete("{projectId:guid}/members/{memberId:guid}")]
    [ProducesResponseType(typeof(Result), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RemoveMember(
        Guid projectId,
        Guid memberId,
        CancellationToken cancellationToken)
    {
        var (actorUserId, actorRole) = GetActorContext();
        var result = await _projectService.RemoveMemberAsync(projectId, memberId, actorUserId, actorRole, cancellationToken);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Lista todos os membros ativos de um projeto
    /// </summary>
    [HttpGet("{projectId:guid}/members")]
    [ProducesResponseType(typeof(Result<IEnumerable<ProjectMemberDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetProjectMembers(
        Guid projectId,
        CancellationToken cancellationToken)
    {
        var (actorUserId, actorRole) = GetActorContext();
        var result = await _projectService.GetProjectMembersAsync(projectId, actorUserId, actorRole, cancellationToken);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    

    /// <summary>
    /// Cria detalhes e configurações do projeto
    /// </summary>
    [HttpPost("{projectId:guid}/details")]
    [ProducesResponseType(typeof(Result<ProjectDetailsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateProjectDetails(
        Guid projectId,
        [FromBody] CreateProjectDetailsRequest request,
        CancellationToken cancellationToken)
    {
        var (actorUserId, actorRole) = GetActorContext();
        var result = await _projectService.CreateProjectDetailsAsync(projectId, request, actorUserId, actorRole, cancellationToken);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Atualiza detalhes e configurações do projeto
    /// </summary>
    [HttpPut("{projectId:guid}/details")]
    [ProducesResponseType(typeof(Result<ProjectDetailsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateProjectDetails(
        Guid projectId,
        [FromBody] UpdateProjectDetailsRequest request,
        CancellationToken cancellationToken)
    {
        var (actorUserId, actorRole) = GetActorContext();
        var result = await _projectService.UpdateProjectDetailsAsync(projectId, request, actorUserId, actorRole, cancellationToken);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Obtém detalhes e configurações do projeto
    /// </summary>
    [HttpGet("{projectId:guid}/details")]
    [ProducesResponseType(typeof(Result<ProjectDetailsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetProjectDetails(
        Guid projectId,
        CancellationToken cancellationToken)
    {
        var (actorUserId, actorRole) = GetActorContext();
        var result = await _projectService.GetProjectDetailsAsync(projectId, actorUserId, actorRole, cancellationToken);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

  

    /// <summary>
    /// Adiciona um compliance ao projeto
    /// </summary>
    [HttpPost("{projectId:guid}/details/compliances")]
    [ProducesResponseType(typeof(Result<ProjectComplianceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AddCompliance(
        Guid projectId,
        [FromBody] CreateProjectComplianceRequest request,
        CancellationToken cancellationToken)
    {
        var (actorUserId, actorRole) = GetActorContext();
        var result = await _projectService.AddComplianceAsync(projectId, request, actorUserId, actorRole, cancellationToken);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Remove um compliance do projeto
    /// </summary>
    [HttpDelete("{projectId:guid}/details/compliances/{complianceId:guid}")]
    [ProducesResponseType(typeof(Result), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RemoveCompliance(
        Guid projectId,
        Guid complianceId,
        CancellationToken cancellationToken)
    {
        var (actorUserId, actorRole) = GetActorContext();
        var result = await _projectService.RemoveComplianceAsync(projectId, complianceId, actorUserId, actorRole, cancellationToken);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

 

    /// <summary>
    /// Adiciona um período indisponível ao projeto
    /// </summary>
    [HttpPost("{projectId:guid}/details/unavailable-periods")]
    [ProducesResponseType(typeof(Result<ProjectUnavailablePeriodDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AddUnavailablePeriod(
        Guid projectId,
        [FromBody] CreateProjectUnavailablePeriodRequest request,
        CancellationToken cancellationToken)
    {
        var (actorUserId, actorRole) = GetActorContext();
        var result = await _projectService.AddUnavailablePeriodAsync(projectId, request, actorUserId, actorRole, cancellationToken);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Remove um período indisponível do projeto
    /// </summary>
    [HttpDelete("{projectId:guid}/details/unavailable-periods/{periodId:guid}")]
    [ProducesResponseType(typeof(Result), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RemoveUnavailablePeriod(
        Guid projectId,
        Guid periodId,
        CancellationToken cancellationToken)
    {
        var (actorUserId, actorRole) = GetActorContext();
        var result = await _projectService.RemoveUnavailablePeriodAsync(projectId, periodId, actorUserId, actorRole, cancellationToken);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }



    /// <summary>
    /// Cria as configurações de execução do projeto (experiência, nível de detalhe, prioridades, etc.)
    /// </summary>
    [HttpPost("{projectId:guid}/execution-settings")]
    [ProducesResponseType(typeof(Result<ProjectExecutionSettingsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateExecutionSettings(
        Guid projectId,
        [FromBody] CreateProjectExecutionSettingsRequest request,
        CancellationToken cancellationToken)
    {
        var (actorUserId, actorRole) = GetActorContext();
        var result = await _projectService.CreateExecutionSettingsAsync(projectId, request, actorUserId, actorRole, cancellationToken);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Atualiza as configurações de execução do projeto
    /// </summary>
    [HttpPut("{projectId:guid}/execution-settings")]
    [ProducesResponseType(typeof(Result<ProjectExecutionSettingsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateExecutionSettings(
        Guid projectId,
        [FromBody] UpdateProjectExecutionSettingsRequest request,
        CancellationToken cancellationToken)
    {
        var (actorUserId, actorRole) = GetActorContext();
        var result = await _projectService.UpdateExecutionSettingsAsync(projectId, request, actorUserId, actorRole, cancellationToken);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Retorna as configurações de execução do projeto com prioridades ordenadas e nomes reais dos enums
    /// </summary>
    [HttpGet("{projectId:guid}/execution-settings")]
    [ProducesResponseType(typeof(Result<ProjectExecutionSettingsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetExecutionSettings(
        Guid projectId,
        CancellationToken cancellationToken)
    {
        var (actorUserId, actorRole) = GetActorContext();
        var result = await _projectService.GetExecutionSettingsAsync(projectId, actorUserId, actorRole, cancellationToken);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Retorna a visão completa do projeto com todas as seções ativas e todos os campos enum como texto
    /// </summary>
    [HttpGet("{projectId:guid}/complete")]
    [ProducesResponseType(typeof(Result<ProjectCompleteDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetComplete(
        Guid projectId,
        CancellationToken cancellationToken)
    {
        var (actorUserId, actorRole) = GetActorContext();
        var result = await _projectService.GetCompleteAsync(projectId, actorUserId, actorRole, cancellationToken);
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
