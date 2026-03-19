using Application.Core.DTOs.Projects;
using Application.Core.Interfaces.Services;
using Domain.Common;
using Domain.Entities;
using Domain.Enums;
using Domain.Interfaces;

namespace Application.Core.Services;

public class ProjectService : IProjectService
{
    private readonly IProjectRepository _projectRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ProjectService(IProjectRepository projectRepository, IUserRepository userRepository, IUnitOfWork unitOfWork)
    {
        _projectRepository = projectRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<ProjectDto>> CreateAsync(
        CreateProjectRequest request,
        Guid? actorUserId,
        UserRole? actorRole,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var actorResult = await ResolveActorAsync(actorUserId, actorRole, cancellationToken);
            if (!actorResult.IsSuccess)
                return Result<ProjectDto>.Failure(actorResult.Message);
            var actor = actorResult.Data!;

            var userExists = await _projectRepository.UserExistsAsync(request.UserId, cancellationToken);
            if (!userExists)
                return Result<ProjectDto>.Failure("Usuário não encontrado. Informe um usuário válido para criar o projeto.");

            var nameExists = await _projectRepository.NameExistsAsync(request.Name, cancellationToken);
            if (nameExists)
                return Result<ProjectDto>.Failure("Nome de projeto já cadastrado. Escolha outro nome para o projeto.");

            Guid? projectCompanyId = null;
            if (actorRole == UserRole.ADM_MASTER)
            {
                var owner = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
                projectCompanyId = owner?.CompanyId;
            }
            else
            {
                projectCompanyId = actor.CompanyId;
            }

            var project = new Project
            {
                Name = request.Name,
                Objective = request.Objective,
                Description = request.Description,
                Status = string.IsNullOrEmpty(request.Status) ? "Draft" : request.Status,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                UserId = request.UserId,
                CompanyId = projectCompanyId
            };

            await _projectRepository.AddAsync(project, cancellationToken);
            await _unitOfWork.CommitAsync(cancellationToken);

            project = await _projectRepository.GetByIdAsync(project.Id, cancellationToken);
            return Result<ProjectDto>.Success(MapToDto(project!), "Projeto criado com sucesso");
        }
        catch (Exception ex)
        {
            return Result<ProjectDto>.Failure($"Erro ao criar projeto: {ex.Message}");
        }
    }

    public async Task<Result<ProjectDto>> GetByIdAsync(
        Guid id,
        Guid? actorUserId,
        UserRole? actorRole,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var actorResult = await ResolveActorAsync(actorUserId, actorRole, cancellationToken);
            if (!actorResult.IsSuccess)
                return Result<ProjectDto>.Failure(actorResult.Message);
            var actor = actorResult.Data;

            var project = await _projectRepository.GetByIdAsync(id, cancellationToken);
            if (project == null)
                return Result<ProjectDto>.Failure($"Projeto não encontrado. Não foi encontrado projeto com ID {id}");

            if (actorRole != UserRole.ADM_MASTER && actor?.CompanyId != project.CompanyId)
                return Result<ProjectDto>.Failure("Sem permissão para acessar projeto de outra empresa.");

            return Result<ProjectDto>.Success(MapToDto(project));
        }
        catch (Exception ex)
        {
            return Result<ProjectDto>.Failure($"Erro ao buscar projeto: {ex.Message}");
        }
    }

    public async Task<Result<IEnumerable<ProjectDto>>> GetAllAsync(
        Guid? actorUserId,
        UserRole? actorRole,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var actorResult = await ResolveActorAsync(actorUserId, actorRole, cancellationToken);
            if (!actorResult.IsSuccess)
                return Result<IEnumerable<ProjectDto>>.Failure(actorResult.Message);
            var actor = actorResult.Data;

            IEnumerable<Project> projects = actorRole == UserRole.ADM_MASTER
                ? await _projectRepository.GetActiveProjectsAsync(cancellationToken)
                : await _projectRepository.GetActiveProjectsByCompanyIdAsync(actor!.CompanyId!.Value, cancellationToken);

            var projectDtos = projects.Select(MapToDto).ToList();
            return Result<IEnumerable<ProjectDto>>.Success(projectDtos, $"{projectDtos.Count} projeto(s) encontrado(s)");
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<ProjectDto>>.Failure($"Erro ao listar projetos: {ex.Message}");
        }
    }

    public async Task<Result<ProjectDto>> UpdateAsync(
        UpdateProjectRequest request,
        Guid? actorUserId,
        UserRole? actorRole,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var actorResult = await ResolveActorAsync(actorUserId, actorRole, cancellationToken);
            if (!actorResult.IsSuccess)
                return Result<ProjectDto>.Failure(actorResult.Message);
            var actor = actorResult.Data;

            var project = await _projectRepository.GetByIdAsync(request.Id, cancellationToken);
            if (project == null)
                return Result<ProjectDto>.Failure($"Projeto não encontrado. Não foi encontrado projeto com ID {request.Id}");

            if (actorRole != UserRole.ADM_MASTER && actor?.CompanyId != project.CompanyId)
                return Result<ProjectDto>.Failure("Sem permissão para editar projeto de outra empresa.");

            if (!project.IsActive)
                return Result<ProjectDto>.Failure("Projeto está desativado e não pode ser atualizado");

            if (project.Name != request.Name)
            {
                var nameExists = await _projectRepository.NameExistsAsync(request.Name, cancellationToken);
                if (nameExists)
                    return Result<ProjectDto>.Failure("Nome de projeto já cadastrado. Escolha outro nome.");
            }

            project.UpdateInfo(request.Name, request.Objective, request.Description, request.StartDate, request.EndDate);
            project.UpdateStatus(request.Status);

            await _projectRepository.UpdateAsync(project, cancellationToken);
            await _unitOfWork.CommitAsync(cancellationToken);

            return Result<ProjectDto>.Success(MapToDto(project), "Projeto atualizado com sucesso");
        }
        catch (Exception ex)
        {
            return Result<ProjectDto>.Failure($"Erro ao atualizar projeto: {ex.Message}");
        }
    }

    public async Task<Result> DeleteAsync(
        Guid id,
        Guid? actorUserId,
        UserRole? actorRole,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var actorResult = await ResolveActorAsync(actorUserId, actorRole, cancellationToken);
            if (!actorResult.IsSuccess)
                return Result.Failure(actorResult.Message);
            var actor = actorResult.Data;

            var project = await _projectRepository.GetByIdAsync(id, cancellationToken);
            if (project == null)
                return Result.Failure($"Projeto não encontrado. Não foi encontrado projeto com ID {id}");

            if (actorRole != UserRole.ADM_MASTER && actor?.CompanyId != project.CompanyId)
                return Result.Failure("Sem permissão para desativar projeto de outra empresa.");

            if (!project.IsActive)
                return Result.Success("Projeto já está desativado");

            project.SoftDelete();
            await _projectRepository.UpdateAsync(project, cancellationToken);
            await _unitOfWork.CommitAsync(cancellationToken);
            return Result.Success("Projeto desativado com sucesso");
        }
        catch (Exception ex)
        {
            return Result.Failure($"Erro ao desativar projeto: {ex.Message}");
        }
    }

    public async Task<Result<IEnumerable<ProjectDto>>> FindByNameAsync(
        string name,
        Guid? actorUserId,
        UserRole? actorRole,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var actorResult = await ResolveActorAsync(actorUserId, actorRole, cancellationToken);
            if (!actorResult.IsSuccess)
                return Result<IEnumerable<ProjectDto>>.Failure(actorResult.Message);
            var actor = actorResult.Data;

            var normalized = name.Trim();
            if (string.IsNullOrWhiteSpace(normalized))
                return Result<IEnumerable<ProjectDto>>.Failure("Nome é obrigatório");

            var projects = await _projectRepository.FindByNameAsync(normalized, cancellationToken);
            if (actorRole != UserRole.ADM_MASTER)
                projects = projects.Where(p => p.CompanyId == actor?.CompanyId);

            var projectDtos = projects.Select(MapToDto).ToList();
            return Result<IEnumerable<ProjectDto>>.Success(projectDtos, $"{projectDtos.Count} projeto(s) encontrado(s)");
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<ProjectDto>>.Failure($"Erro ao buscar projetos por nome: {ex.Message}");
        }
    }

    public async Task<Result<IEnumerable<ProjectDto>>> FindByStatusAsync(
        string status,
        Guid? actorUserId,
        UserRole? actorRole,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var actorResult = await ResolveActorAsync(actorUserId, actorRole, cancellationToken);
            if (!actorResult.IsSuccess)
                return Result<IEnumerable<ProjectDto>>.Failure(actorResult.Message);
            var actor = actorResult.Data;

            if (string.IsNullOrWhiteSpace(status))
                return Result<IEnumerable<ProjectDto>>.Failure("Status é obrigatório");

            var projects = await _projectRepository.FindByStatusAsync(status, cancellationToken);
            if (actorRole != UserRole.ADM_MASTER)
                projects = projects.Where(p => p.CompanyId == actor?.CompanyId);

            var projectDtos = projects.Select(MapToDto).ToList();
            return Result<IEnumerable<ProjectDto>>.Success(projectDtos, $"{projectDtos.Count} projeto(s) encontrado(s) com status '{status}'");
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<ProjectDto>>.Failure($"Erro ao buscar projetos por status: {ex.Message}");
        }
    }

    public async Task<bool> NameExistsAsync(string name, CancellationToken cancellationToken = default)
    {
        try
        {
            var normalized = name.Trim();
            if (string.IsNullOrWhiteSpace(normalized))
                return false;
            return await _projectRepository.NameExistsAsync(normalized, cancellationToken);
        }
        catch
        {
            return false;
        }
    }

    public async Task<Result<ProjectDto>> ToggleStatusAsync(
        Guid id,
        Guid? actorUserId,
        UserRole? actorRole,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var actorResult = await ResolveActorAsync(actorUserId, actorRole, cancellationToken);
            if (!actorResult.IsSuccess)
                return Result<ProjectDto>.Failure(actorResult.Message);
            var actor = actorResult.Data;

            var project = await _projectRepository.GetByIdAsync(id, cancellationToken);
            if (project == null)
                return Result<ProjectDto>.Failure($"Projeto não encontrado. Não foi encontrado projeto com ID {id}");

            if (actorRole != UserRole.ADM_MASTER && actor?.CompanyId != project.CompanyId)
                return Result<ProjectDto>.Failure("Sem permissão para alterar status de projeto de outra empresa.");

            if (!project.IsActive)
                return Result<ProjectDto>.Failure("Projeto está desativado (deletado) e não pode ter status alterado");

            var newStatus = project.Status == "Active" ? "Inactive" : "Active";
            project.UpdateStatus(newStatus);

            await _projectRepository.UpdateAsync(project, cancellationToken);
            await _unitOfWork.CommitAsync(cancellationToken);

            var message = newStatus == "Active" ? "Projeto ativado com sucesso" : "Projeto inativado com sucesso";
            return Result<ProjectDto>.Success(MapToDto(project), message);
        }
        catch (Exception ex)
        {
            return Result<ProjectDto>.Failure($"Erro ao alterar status do projeto: {ex.Message}");
        }
    }

    private static ProjectDto MapToDto(Project project)
    {
        return new ProjectDto
        {
            Id = project.Id,
            CompanyId = project.CompanyId,
            CompanyName = project.Company?.Name,
            Name = project.Name,
            Objective = project.Objective,
            Description = project.Description,
            Status = project.Status,
            StartDate = project.StartDate,
            EndDate = project.EndDate,
            UserId = project.UserId,
            UserName = project.User?.Name,
            IsActive = project.IsActive,
            CreatedAt = project.CreatedAt,
            UpdatedAt = project.UpdatedAt
        };
    }

    private async Task<Result<User?>> ResolveActorAsync(Guid? actorUserId, UserRole? actorRole, CancellationToken cancellationToken)
    {
        if (!actorRole.HasValue)
            return Result<User?>.Failure("Perfil do usuário não identificado para operação de projetos.");

        if (!actorUserId.HasValue)
            return Result<User?>.Failure("Usuário logado não identificado para operação de projetos.");

        var actor = await _userRepository.GetByIdAsync(actorUserId.Value, cancellationToken);
        if (actor == null || !actor.IsActive)
            return Result<User?>.Failure("Usuário logado inválido ou inativo.");

        if (actorRole != UserRole.ADM_MASTER && actor.CompanyId == null)
            return Result<User?>.Failure("Usuário sem empresa vinculada não pode acessar projetos de empresa.");

        return Result<User?>.Success(actor);
    }
}
