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
}
