using Application.Core.DTOs.Projects;
using Domain.Common;
using Domain.Enums;

namespace Application.Core.Interfaces.Services;

/// <summary>
/// Interface do servi�o de Projects
/// Define contrato para l�gica de neg�cio relacionada a projetos
/// </summary>
public interface IProjectService
{
    Task<Result<ProjectDto>> CreateAsync(CreateProjectRequest request, Guid? actorUserId, UserRole? actorRole, CancellationToken cancellationToken = default);
    Task<Result<ProjectDto>> GetByIdAsync(Guid id, Guid? actorUserId, UserRole? actorRole, CancellationToken cancellationToken = default);
    Task<Result<IEnumerable<ProjectDto>>> GetAllAsync(Guid? actorUserId, UserRole? actorRole, CancellationToken cancellationToken = default);
    Task<Result<ProjectDto>> UpdateAsync(UpdateProjectRequest request, Guid? actorUserId, UserRole? actorRole, CancellationToken cancellationToken = default);
    Task<Result> DeleteAsync(Guid id, Guid? actorUserId, UserRole? actorRole, CancellationToken cancellationToken = default);
    Task<Result<IEnumerable<ProjectDto>>> FindByNameAsync(string name, Guid? actorUserId, UserRole? actorRole, CancellationToken cancellationToken = default);
    Task<Result<IEnumerable<ProjectDto>>> FindByStatusAsync(string status, Guid? actorUserId, UserRole? actorRole, CancellationToken cancellationToken = default);
    Task<bool> NameExistsAsync(string name, CancellationToken cancellationToken = default);
    Task<Result<ProjectDto>> ToggleStatusAsync(Guid id, Guid? actorUserId, UserRole? actorRole, CancellationToken cancellationToken = default);

    // Métodos para gerenciar membros
    Task<Result<ProjectMemberDto>> AddMemberAsync(Guid projectId, CreateProjectMemberRequest request, Guid? actorUserId, UserRole? actorRole, CancellationToken cancellationToken = default);
    Task<Result> RemoveMemberAsync(Guid projectId, Guid memberId, Guid? actorUserId, UserRole? actorRole, CancellationToken cancellationToken = default);
    Task<Result<IEnumerable<ProjectMemberDto>>> GetProjectMembersAsync(Guid projectId, Guid? actorUserId, UserRole? actorRole, CancellationToken cancellationToken = default);

    // Métodos para gerenciar detalhes do projeto
    Task<Result<ProjectDetailsDto>> CreateProjectDetailsAsync(Guid projectId, CreateProjectDetailsRequest request, Guid? actorUserId, UserRole? actorRole, CancellationToken cancellationToken = default);
    Task<Result<ProjectDetailsDto>> UpdateProjectDetailsAsync(Guid projectId, UpdateProjectDetailsRequest request, Guid? actorUserId, UserRole? actorRole, CancellationToken cancellationToken = default);
    Task<Result<ProjectDetailsDto>> GetProjectDetailsAsync(Guid projectId, Guid? actorUserId, UserRole? actorRole, CancellationToken cancellationToken = default);

    // Métodos para gerenciar compliance
    Task<Result<ProjectComplianceDto>> AddComplianceAsync(Guid projectId, CreateProjectComplianceRequest request, Guid? actorUserId, UserRole? actorRole, CancellationToken cancellationToken = default);
    Task<Result> RemoveComplianceAsync(Guid projectId, Guid complianceId, Guid? actorUserId, UserRole? actorRole, CancellationToken cancellationToken = default);

    // Métodos para gerenciar períodos indisponíveis
    Task<Result<ProjectUnavailablePeriodDto>> AddUnavailablePeriodAsync(Guid projectId, CreateProjectUnavailablePeriodRequest request, Guid? actorUserId, UserRole? actorRole, CancellationToken cancellationToken = default);
    Task<Result> RemoveUnavailablePeriodAsync(Guid projectId, Guid periodId, Guid? actorUserId, UserRole? actorRole, CancellationToken cancellationToken = default);

    // Métodos para gerenciar configurações de execução do projeto
    Task<Result<ProjectExecutionSettingsDto>> CreateExecutionSettingsAsync(Guid projectId, CreateProjectExecutionSettingsRequest request, Guid? actorUserId, UserRole? actorRole, CancellationToken cancellationToken = default);
    Task<Result<ProjectExecutionSettingsDto>> UpdateExecutionSettingsAsync(Guid projectId, UpdateProjectExecutionSettingsRequest request, Guid? actorUserId, UserRole? actorRole, CancellationToken cancellationToken = default);
    Task<Result<ProjectExecutionSettingsDto>> GetExecutionSettingsAsync(Guid projectId, Guid? actorUserId, UserRole? actorRole, CancellationToken cancellationToken = default);

    // Visão completa do projeto com todos os enums resolvidos para texto
    Task<Result<ProjectCompleteDto>> GetCompleteAsync(Guid id, Guid? actorUserId, UserRole? actorRole, CancellationToken cancellationToken = default);
}
