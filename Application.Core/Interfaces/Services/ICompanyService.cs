using Application.Core.DTOs.Companies;
using Application.Core.DTOs.Users;
using Domain.Common;

namespace Application.Core.Interfaces.Services;

/// <summary>
/// Interface do serviço de empresas
/// </summary>
public interface ICompanyService
{
    Task<Result<CompanyDto>> CreateAsync(CreateCompanyRequest request, CancellationToken cancellationToken = default);
    Task<Result<CompanyDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result<CompanyDto>> UpdateAsync(UpdateCompanyRequest request, CancellationToken cancellationToken = default);
    Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result<IEnumerable<UserDto>>> GetUsersByCompanyAsync(Guid id, CancellationToken cancellationToken = default);
}
