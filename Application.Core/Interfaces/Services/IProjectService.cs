using Application.Core.DTOs.Projects;
using Domain.Common;

namespace Application.Core.Interfaces.Services;

/// <summary>
/// Interface do serviço de Projects
/// Define contrato para lógica de negócio relacionada a projetos
/// </summary>
public interface IProjectService
{
    Task<Result<ProjectDto>> CreateAsync(CreateProjectRequest request, CancellationToken cancellationToken = default);
    Task<Result<ProjectDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result<IEnumerable<ProjectDto>>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Result<ProjectDto>> UpdateAsync(UpdateProjectRequest request, CancellationToken cancellationToken = default);
    Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result<IEnumerable<ProjectDto>>> FindByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<Result<IEnumerable<ProjectDto>>> FindByStatusAsync(string status, CancellationToken cancellationToken = default);
    Task<bool> NameExistsAsync(string name, CancellationToken cancellationToken = default);
}
