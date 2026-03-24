using Domain.Entities;

namespace Domain.Interfaces;

/// <summary>
/// Interface espec�fica para reposit�rio de Projects
/// Herda opera��es gen�ricas e adiciona m�todos espec�ficos
/// </summary>
public interface IProjectRepository : IRepository<Project>
{
    Task<IEnumerable<Project>> FindByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<IEnumerable<Project>> FindByStatusAsync(string status, CancellationToken cancellationToken = default);
    Task<bool> NameExistsAsync(string name, CancellationToken cancellationToken = default);
    Task<IEnumerable<Project>> GetActiveProjectsAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<Project>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Project>> GetActiveProjectsByCompanyIdAsync(Guid companyId, CancellationToken cancellationToken = default);
    Task<bool> UserExistsAsync(Guid userId, CancellationToken cancellationToken = default);

    // Métodos diretos para ProjectDetails (evita conflito de estado do EF Core)
    Task AddProjectDetailsAsync(ProjectDetails details, CancellationToken cancellationToken = default);
    Task AddComplianceAsync(ProjectCompliance compliance, CancellationToken cancellationToken = default);
    Task AddUnavailablePeriodAsync(ProjectUnavailablePeriod period, CancellationToken cancellationToken = default);
    Task<ProjectDetails?> GetProjectDetailsByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default);

    // Métodos para entidades de contexto do projeto
    Task AddDependencyAsync(ProjectDependencies dependency, CancellationToken cancellationToken = default);
    Task AddIntegrationAsync(ProjectIntegrations integration, CancellationToken cancellationToken = default);
    Task AddSensitiveDataAsync(ProjectSensitiveData sensitiveData, CancellationToken cancellationToken = default);
}
