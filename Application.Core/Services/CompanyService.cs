using Application.Core.DTOs.Companies;
using Application.Core.DTOs.Users;
using Application.Core.Interfaces.Services;
using Domain.Common;
using Domain.Entities;
using Domain.Interfaces;

namespace Application.Core.Services;

/// <summary>
/// Service de Empresas
/// Validações: apenas ADM pode criar/editar sua própria empresa
/// ADM_MASTER não pertence a empresa
/// </summary>
public class CompanyService : ICompanyService
{
    private readonly ICompanyRepository _companyRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CompanyService(ICompanyRepository companyRepository, IUnitOfWork unitOfWork)
    {
        _companyRepository = companyRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<CompanyDto>> CreateAsync(
        CreateCompanyRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                return Result<CompanyDto>.Failure("Nome é obrigatório.");

            if (string.IsNullOrWhiteSpace(request.Category))
                return Result<CompanyDto>.Failure("Categoria é obrigatória.");

            var company = new Company
            {
                Name = request.Name.Trim(),
                Address = request.Address?.Trim(),
                NumberOfMembers = request.NumberOfMembers,
                Category = request.Category?.Trim()
            };

            await _companyRepository.AddAsync(company, cancellationToken);
            await _unitOfWork.CommitAsync(cancellationToken);

            return Result<CompanyDto>.Success(MapToDto(company), "Empresa criada com sucesso.");
        }
        catch (Exception ex)
        {
            return Result<CompanyDto>.Failure($"Erro ao criar empresa: {ex.Message}");
        }
    }

    public async Task<Result<CompanyDto>> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var company = await _companyRepository.GetByIdWithUsersAsync(id, cancellationToken);
            if (company == null)
                return Result<CompanyDto>.Failure("Empresa não encontrada.");

            return Result<CompanyDto>.Success(MapToDto(company));
        }
        catch (Exception ex)
        {
            return Result<CompanyDto>.Failure($"Erro ao buscar empresa: {ex.Message}");
        }
    }

    public async Task<Result<CompanyDto>> UpdateAsync(
        UpdateCompanyRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                return Result<CompanyDto>.Failure("Nome é obrigatório.");

            if (string.IsNullOrWhiteSpace(request.Category))
                return Result<CompanyDto>.Failure("Categoria é obrigatória.");

            var company = await _companyRepository.GetByIdAsync(request.Id, cancellationToken);
            if (company == null)
                return Result<CompanyDto>.Failure("Empresa não encontrada.");

            if (!company.IsActive)
                return Result<CompanyDto>.Failure("Empresa inativa não pode ser editada.");

            company.Update(request.Name.Trim(), request.Address?.Trim(), request.NumberOfMembers, request.Category?.Trim());

            await _companyRepository.UpdateAsync(company, cancellationToken);
            await _unitOfWork.CommitAsync(cancellationToken);

            return Result<CompanyDto>.Success(MapToDto(company), "Empresa atualizada com sucesso.");
        }
        catch (Exception ex)
        {
            return Result<CompanyDto>.Failure($"Erro ao atualizar empresa: {ex.Message}");
        }
    }

    public async Task<Result> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var company = await _companyRepository.GetByIdAsync(id, cancellationToken);
            if (company == null)
                return Result.Failure("Empresa não encontrada.");

            if (!company.IsActive)
                return Result.Success("Empresa já está inativa.");

            company.Deactivate();
            await _companyRepository.UpdateAsync(company, cancellationToken);
            await _unitOfWork.CommitAsync(cancellationToken);

            return Result.Success("Empresa desativada com sucesso.");
        }
        catch (Exception ex)
        {
            return Result.Failure($"Erro ao desativar empresa: {ex.Message}");
        }
    }

    public async Task<Result<IEnumerable<UserDto>>> GetUsersByCompanyAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var company = await _companyRepository.GetByIdWithUsersAsync(id, cancellationToken);
            if (company == null)
                return Result<IEnumerable<UserDto>>.Failure("Empresa não encontrada.");

            var users = company.Users
                .Where(u => u.IsActive)
                .Select(u => new UserDto
                {
                    Id = u.Id,
                    Name = u.Name,
                    Email = u.Email,
                    Phone = u.Phone,
                    CPF = u.CPF,
                    BirthDate = u.BirthDate,
                    Role = u.Role.ToString(),
                    IsEmailVerified = u.IsEmailVerified,
                    IsFirstAccess = u.IsFirstAccess,
                    IsActive = u.IsActive,
                    CreatedAt = u.CreatedAt,
                    UpdatedAt = u.UpdatedAt
                });

            return Result<IEnumerable<UserDto>>.Success(users, $"{users.Count()} usuário(s) encontrado(s).");
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<UserDto>>.Failure($"Erro ao buscar usuários da empresa: {ex.Message}");
        }
    }

    private static CompanyDto MapToDto(Company company) => new()
    {
        Id = company.Id,
        Name = company.Name,
        Address = company.Address,
        NumberOfMembers = company.NumberOfMembers,
        Category = company.Category,
        IsActive = company.IsActive,
        CreatedAt = company.CreatedAt,
        UpdatedAt = company.UpdatedAt,
        UserCount = company.Users?.Count(u => u.IsActive) ?? 0
    };
}
