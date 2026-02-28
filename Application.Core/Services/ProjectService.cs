using Application.Core.DTOs.Projects;
using Application.Core.Interfaces.Services;
using Domain.Common;
using Domain.Entities;
using Domain.Interfaces;

namespace Application.Core.Services;

/// <summary>
/// Service de Projects
/// Contém toda a lógica de negócio relacionada a projetos
/// Implementa IProjectService seguindo Dependency Inversion Principle
/// </summary>
public class ProjectService : IProjectService
{
    private readonly IProjectRepository _projectRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ProjectService(IProjectRepository projectRepository, IUnitOfWork unitOfWork)
    {
        _projectRepository = projectRepository;
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Cria um novo projeto
    /// </summary>
    public async Task<Result<ProjectDto>> CreateAsync(
        CreateProjectRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // 1. Validar se usuário existe
            var userExists = await _projectRepository.UserExistsAsync(request.UserId, cancellationToken);
            if (!userExists)
            {
                return Result<ProjectDto>.Failure(
                    "Usuário não encontrado. Informe um usuário válido para criar o projeto.");
            }

            // 2. Validar se nome já existe
            var nameExists = await _projectRepository.NameExistsAsync(request.Name, cancellationToken);
            if (nameExists)
            {
                return Result<ProjectDto>.Failure(
                    "Nome de projeto já cadastrado. Escolha outro nome para o projeto.");
            }

            // 3. Criar a entidade
            var project = new Project
            {
                Name = request.Name,
                Objective = request.Objective,
                Description = request.Description,
                Status = string.IsNullOrEmpty(request.Status) ? "Draft" : request.Status,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                UserId = request.UserId
            };

            // 4. Adicionar ao repositório
            await _projectRepository.AddAsync(project, cancellationToken);

            // 5. Salvar alterações
            await _unitOfWork.CommitAsync(cancellationToken);

            // 6. Recarregar projeto com dados do usuário
            project = await _projectRepository.GetByIdAsync(project.Id, cancellationToken);

            // 7. Mapear para DTO e retornar
            var projectDto = MapToDto(project!);
            return Result<ProjectDto>.Success(projectDto, "Projeto criado com sucesso");
        }
        catch (Exception ex)
        {
            // TODO: Implementar logging aqui
            return Result<ProjectDto>.Failure($"Erro ao criar projeto: {ex.Message}");
        }
    }

    /// <summary>
    /// Busca projeto por ID
    /// </summary>
    public async Task<Result<ProjectDto>> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var project = await _projectRepository.GetByIdAsync(id, cancellationToken);

            if (project == null)
            {
                return Result<ProjectDto>.Failure(
                    $"Projeto não encontrado. Não foi encontrado projeto com ID {id}");
            }

            var projectDto = MapToDto(project);
            return Result<ProjectDto>.Success(projectDto);
        }
        catch (Exception ex)
        {
            // TODO: Implementar logging aqui
            return Result<ProjectDto>.Failure($"Erro ao buscar projeto: {ex.Message}");
        }
    }

    /// <summary>
    /// Lista todos os projetos ativos
    /// </summary>
    public async Task<Result<IEnumerable<ProjectDto>>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var projects = await _projectRepository.GetActiveProjectsAsync(cancellationToken);
            var projectDtos = projects.Select(MapToDto).ToList();

            return Result<IEnumerable<ProjectDto>>.Success(
                projectDtos,
                $"{projectDtos.Count} projeto(s) encontrado(s)");
        }
        catch (Exception ex)
        {
            // TODO: Implementar logging aqui
            return Result<IEnumerable<ProjectDto>>.Failure($"Erro ao listar projetos: {ex.Message}");
        }
    }

    /// <summary>
    /// Atualiza informações do projeto
    /// </summary>
    public async Task<Result<ProjectDto>> UpdateAsync(
        UpdateProjectRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // 1. Buscar projeto existente
            var project = await _projectRepository.GetByIdAsync(request.Id, cancellationToken);
            if (project == null)
            {
                return Result<ProjectDto>.Failure(
                    $"Projeto não encontrado. Não foi encontrado projeto com ID {request.Id}");
            }

            // 2. Validar se está ativo
            if (!project.IsActive)
            {
                return Result<ProjectDto>.Failure("Projeto está desativado e não pode ser atualizado");
            }

            // 3. Validar mudança de nome (se mudou, verificar se novo nome já existe)
            if (project.Name != request.Name)
            {
                var nameExists = await _projectRepository.NameExistsAsync(request.Name, cancellationToken);
                if (nameExists)
                {
                    return Result<ProjectDto>.Failure(
                        "Nome de projeto já cadastrado. Escolha outro nome.");
                }
            }

            // 4. Aplicar alterações
            project.UpdateInfo(request.Name, request.Objective, request.Description, request.StartDate, request.EndDate);
            project.UpdateStatus(request.Status);

            // 5. Persistir alterações
            await _projectRepository.UpdateAsync(project, cancellationToken);
            await _unitOfWork.CommitAsync(cancellationToken);

            // 6. Retornar DTO
            return Result<ProjectDto>.Success(MapToDto(project), "Projeto atualizado com sucesso");
        }
        catch (Exception ex)
        {
            // TODO: Implementar logging aqui
            return Result<ProjectDto>.Failure($"Erro ao atualizar projeto: {ex.Message}");
        }
    }

    /// <summary>
    /// Desativa um projeto (soft delete)
    /// </summary>
    public async Task<Result> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // 1. Buscar projeto existente
            var project = await _projectRepository.GetByIdAsync(id, cancellationToken);
            if (project == null)
            {
                return Result.Failure($"Projeto não encontrado. Não foi encontrado projeto com ID {id}");
            }

            // 2. Soft delete
            if (!project.IsActive)
            {
                return Result.Success("Projeto já está desativado");
            }

            project.SoftDelete();

            // 3. Persistir alterações
            await _projectRepository.UpdateAsync(project, cancellationToken);
            await _unitOfWork.CommitAsync(cancellationToken);

            return Result.Success("Projeto desativado com sucesso");
        }
        catch (Exception ex)
        {
            // TODO: Implementar logging aqui
            return Result.Failure($"Erro ao desativar projeto: {ex.Message}");
        }
    }

    /// <summary>
    /// Busca projetos por nome
    /// </summary>
    public async Task<Result<IEnumerable<ProjectDto>>> FindByNameAsync(
        string name,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var normalized = name.Trim();
            if (string.IsNullOrWhiteSpace(normalized))
            {
                return Result<IEnumerable<ProjectDto>>.Failure("Nome é obrigatório");
            }

            var projects = await _projectRepository.FindByNameAsync(normalized, cancellationToken);
            var projectDtos = projects.Select(MapToDto).ToList();
            
            return Result<IEnumerable<ProjectDto>>.Success(
                projectDtos,
                $"{projectDtos.Count} projeto(s) encontrado(s)");
        }
        catch (Exception ex)
        {
            // TODO: Implementar logging aqui
            return Result<IEnumerable<ProjectDto>>.Failure($"Erro ao buscar projetos por nome: {ex.Message}");
        }
    }

    /// <summary>
    /// Busca projetos por status
    /// </summary>
    public async Task<Result<IEnumerable<ProjectDto>>> FindByStatusAsync(
        string status,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(status))
            {
                return Result<IEnumerable<ProjectDto>>.Failure("Status é obrigatório");
            }

            var projects = await _projectRepository.FindByStatusAsync(status, cancellationToken);
            var projectDtos = projects.Select(MapToDto).ToList();
            
            return Result<IEnumerable<ProjectDto>>.Success(
                projectDtos,
                $"{projectDtos.Count} projeto(s) encontrado(s) com status '{status}'");
        }
        catch (Exception ex)
        {
            // TODO: Implementar logging aqui
            return Result<IEnumerable<ProjectDto>>.Failure($"Erro ao buscar projetos por status: {ex.Message}");
        }
    }

    /// <summary>
    /// Verifica se nome de projeto já está em uso
    /// </summary>
    public async Task<bool> NameExistsAsync(
        string name,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var normalized = name.Trim();
            if (string.IsNullOrWhiteSpace(normalized))
            {
                return false;
            }

            return await _projectRepository.NameExistsAsync(normalized, cancellationToken);
        }
        catch
        {
            // TODO: Implementar logging aqui
            return false;
        }
    }

    /// <summary>
    /// Alterna status do projeto entre Active e Inactive automaticamente
    /// </summary>
    public async Task<Result<ProjectDto>> ToggleStatusAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // 1. Buscar projeto existente
            var project = await _projectRepository.GetByIdAsync(id, cancellationToken);
            if (project == null)
            {
                return Result<ProjectDto>.Failure(
                    $"Projeto não encontrado. Não foi encontrado projeto com ID {id}");
            }

            // 2. Validar se está ativo (IsActive = true)
            if (!project.IsActive)
            {
                return Result<ProjectDto>.Failure(
                    "Projeto está desativado (deletado) e não pode ter status alterado");
            }

            // 3. Determinar novo status automaticamente (toggle)
            var newStatus = project.Status == "Active" ? "Inactive" : "Active";

            // 4. Alterar status
            project.UpdateStatus(newStatus);

            // 5. Persistir alterações
            await _projectRepository.UpdateAsync(project, cancellationToken);
            await _unitOfWork.CommitAsync(cancellationToken);

            // 6. Retornar DTO com mensagem apropriada
            var message = newStatus == "Active" 
                ? "Projeto ativado com sucesso" 
                : "Projeto inativado com sucesso";
                
            return Result<ProjectDto>.Success(MapToDto(project), message);
        }
        catch (Exception ex)
        {
            // TODO: Implementar logging aqui
            return Result<ProjectDto>.Failure($"Erro ao alterar status do projeto: {ex.Message}");
        }
    }

    /// <summary>
    /// Mapeia entidade Project para ProjectDto
    /// </summary>
    private static ProjectDto MapToDto(Project project)
    {
        return new ProjectDto
        {
            Id = project.Id,
            Name = project.Name,
            Objective = project.Objective,
            Description = project.Description,
            Status = project.Status,
            StartDate = project.StartDate,
            EndDate = project.EndDate,
            UserId = project.UserId,
            UserName = project.User?.Name, // Nome do usuário que criou
            IsActive = project.IsActive,
            CreatedAt = project.CreatedAt,
            UpdatedAt = project.UpdatedAt
        };
    }
}
