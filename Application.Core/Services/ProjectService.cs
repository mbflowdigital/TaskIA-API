using Application.Core.DTOs.Projects.Extensions;
using Application.Core.DTOs.Projects.Requests;
using Application.Core.DTOs.Projects.Responses;
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
                ResponsibleSector = request.ResponsibleSector,
                ProjectType = request.ProjectType,
                UserId = request.UserId,
                CompanyId = projectCompanyId
            };

            // Adicionar membros ao projeto
            if (request.Members != null && request.Members.Any())
            {
                foreach (var memberRequest in request.Members)
                {
                    // Validar se o usuário existe
                    var memberUserExists = await _projectRepository.UserExistsAsync(memberRequest.UserId, cancellationToken);
                    if (!memberUserExists)
                        continue;

                    var member = new ProjectMemberEntity
                    {
                        ProjectId = project.Id,
                        UserId = memberRequest.UserId,
                        ProjectFunction = memberRequest.ProjectFunction,
                        Dedication = memberRequest.Dedication,
                        Approver = memberRequest.Approver,
                        FunctionDescription = memberRequest.FunctionDescription
                    };

                    project.ProjectMembers.Add(member);
                }
            }

            await _projectRepository.AddAsync(project, cancellationToken);
            await _unitOfWork.CommitAsync(cancellationToken);

            // Atribui navegações já carregadas para evitar um GetByIdAsync pesado (10 Includes) ao DB remoto
            project.User = actor;

            return Result<ProjectDto>.Success(MapToDto(project), "Projeto criado com sucesso");
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

            project.UpdateInfo(request.Name, request.Objective, request.Description, request.StartDate, request.EndDate, request.ResponsibleSector, request.ProjectType);
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
            ResponsibleSector = project.ResponsibleSector,
            ProjectType = project.ProjectType,
            UserId = project.UserId,
            UserName = project.User?.Name,
            IsActive = project.IsActive,
            CreatedAt = project.CreatedAt,
            UpdatedAt = project.UpdatedAt,
            TaskCount = project.Tasks?.Count ?? 0,
            Members = project.ProjectMembers?
                .Where(m => m.IsActive)
                .Select(m => new ProjectMemberDto
                {
                    Id = m.Id,
                    UserId = m.UserId,
                    UserName = m.User?.Name,
                    ProjectFunction = m.ProjectFunction,
                    Dedication = m.Dedication,
                    Approver = m.Approver,
                    FunctionDescription = m.FunctionDescription,
                    IsActive = m.IsActive,
                    CreatedAt = m.CreatedAt
                }).ToList() ?? new List<ProjectMemberDto>(),
            Details = project.ProjectDetails?.ToDto(
                project.Dependencies.Where(d => d.IsActive),
                project.Integrations.Where(i => i.IsActive),
                project.SensitiveData.Where(s => s.IsActive)),
            ExecutionSettings = project.ExecutionSettings?.ToDto(
                project.PriorityRankings.Where(r => r.IsActive))
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


    public async Task<Result<ProjectMemberDto>> AddMemberAsync(
        Guid projectId,
        CreateProjectMemberRequest request,
        Guid? actorUserId,
        UserRole? actorRole,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var actorResult = await ResolveActorAsync(actorUserId, actorRole, cancellationToken);
            if (!actorResult.IsSuccess)
                return Result<ProjectMemberDto>.Failure(actorResult.Message);
            var actor = actorResult.Data;

            var project = await _projectRepository.GetByIdAsync(projectId, cancellationToken);
            if (project == null)
                return Result<ProjectMemberDto>.Failure($"Projeto não encontrado com ID {projectId}");

            if (actorRole != UserRole.ADM_MASTER && actor?.CompanyId != project.CompanyId)
                return Result<ProjectMemberDto>.Failure("Sem permissão para adicionar membros a projeto de outra empresa.");

            var memberUserExists = await _projectRepository.UserExistsAsync(request.UserId, cancellationToken);
            if (!memberUserExists)
                return Result<ProjectMemberDto>.Failure("Usuário do membro não encontrado.");

            // Verificar se o usuário já é membro do projeto
            if (project.ProjectMembers.Any(m => m.UserId == request.UserId && m.IsActive))
                return Result<ProjectMemberDto>.Failure("Usuário já é membro deste projeto.");

            var member = new ProjectMemberEntity
            {
                ProjectId = projectId,
                UserId = request.UserId,
                ProjectFunction = request.ProjectFunction,
                Dedication = request.Dedication,
                Approver = request.Approver,
                FunctionDescription = request.FunctionDescription
            };

            project.AddMember(member);
            await _unitOfWork.CommitAsync(cancellationToken);

            // Recarregar com dados do usuário
            var memberUser = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);

            var memberDto = new ProjectMemberDto
            {
                Id = member.Id,
                UserId = member.UserId,
                UserName = memberUser?.Name,
                ProjectFunction = member.ProjectFunction,
                Dedication = member.Dedication,
                Approver = member.Approver,
                FunctionDescription = member.FunctionDescription,
                IsActive = member.IsActive,
                CreatedAt = member.CreatedAt
            };

            return Result<ProjectMemberDto>.Success(memberDto, "Membro adicionado ao projeto com sucesso");
        }
        catch (Exception ex)
        {
            return Result<ProjectMemberDto>.Failure($"Erro ao adicionar membro ao projeto: {ex.Message}");
        }
    }

    public async Task<Result> RemoveMemberAsync(
        Guid projectId,
        Guid memberId,
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

            var project = await _projectRepository.GetByIdAsync(projectId, cancellationToken);
            if (project == null)
                return Result.Failure($"Projeto não encontrado com ID {projectId}");

            if (actorRole != UserRole.ADM_MASTER && actor?.CompanyId != project.CompanyId)
                return Result.Failure("Sem permissão para remover membros de projeto de outra empresa.");

            project.RemoveMember(memberId);
            await _unitOfWork.CommitAsync(cancellationToken);

            return Result.Success("Membro removido do projeto com sucesso");
        }
        catch (Exception ex)
        {
            return Result.Failure($"Erro ao remover membro do projeto: {ex.Message}");
        }
    }

    public async Task<Result<IEnumerable<ProjectMemberDto>>> GetProjectMembersAsync(
        Guid projectId,
        Guid? actorUserId,
        UserRole? actorRole,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var actorResult = await ResolveActorAsync(actorUserId, actorRole, cancellationToken);
            if (!actorResult.IsSuccess)
                return Result<IEnumerable<ProjectMemberDto>>.Failure(actorResult.Message);
            var actor = actorResult.Data;

            var project = await _projectRepository.GetByIdAsync(projectId, cancellationToken);
            if (project == null)
                return Result<IEnumerable<ProjectMemberDto>>.Failure($"Projeto não encontrado com ID {projectId}");

            if (actorRole != UserRole.ADM_MASTER && actor?.CompanyId != project.CompanyId)
                return Result<IEnumerable<ProjectMemberDto>>.Failure("Sem permissão para visualizar membros de projeto de outra empresa.");

            var membersDto = project.ProjectMembers
                .Where(m => m.IsActive)
                .Select(m => new ProjectMemberDto
                {
                    Id = m.Id,
                    UserId = m.UserId,
                    UserName = m.User?.Name,
                    ProjectFunction = m.ProjectFunction,
                    Dedication = m.Dedication,
                    Approver = m.Approver,
                    FunctionDescription = m.FunctionDescription,
                    IsActive = m.IsActive,
                    CreatedAt = m.CreatedAt
                }).ToList();

            return Result<IEnumerable<ProjectMemberDto>>.Success(membersDto);
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<ProjectMemberDto>>.Failure($"Erro ao buscar membros do projeto: {ex.Message}");
        }
    }

  

    public async Task<Result<ProjectDetailsDto>> CreateProjectDetailsAsync(
        Guid projectId,
        CreateProjectDetailsRequest request,
        Guid? actorUserId,
        UserRole? actorRole,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var actorResult = await ResolveActorAsync(actorUserId, actorRole, cancellationToken);
            if (!actorResult.IsSuccess)
                return Result<ProjectDetailsDto>.Failure(actorResult.Message);
            var actor = actorResult.Data;

            var project = await _projectRepository.GetByIdAsync(projectId, cancellationToken);
            if (project == null)
                return Result<ProjectDetailsDto>.Failure($"Projeto não encontrado com ID {projectId}");

            if (actorRole != UserRole.ADM_MASTER && actor?.CompanyId != project.CompanyId)
                return Result<ProjectDetailsDto>.Failure("Sem permissão para adicionar detalhes a projeto de outra empresa.");

            if (project.ProjectDetails != null)
                return Result<ProjectDetailsDto>.Failure("Projeto já possui detalhes cadastrados. Use a operação de atualização.");

            if (request.Orcamento == BudgetType.ValorFixo && request.ValorOrcamento == null)
                return Result<ProjectDetailsDto>.Failure("ValorOrcamento é obrigatório quando Orçamento = ValorFixo.");

            if (request.DowntimePermitido == DowntimeType.AteXHoras && request.HorasDowntime == null)
                return Result<ProjectDetailsDto>.Failure("HorasDowntime é obrigatório quando DowntimePermitido = AteXHoras.");

            var details = new ProjectDetails
            {
                ProjectId = projectId,
                TemDependenciasExternas = request.TemDependenciasExternas,
                TemIntegracoes = request.TemIntegracoes,
                Orcamento = request.Orcamento,
                ValorOrcamento = request.Orcamento == BudgetType.ValorFixo ? request.ValorOrcamento : null,
                HorarioTrabalho = request.HorarioTrabalho,
                DowntimePermitido = request.DowntimePermitido,
                HorasDowntime = request.DowntimePermitido == DowntimeType.AteXHoras ? request.HorasDowntime : null
            };

            // Adicionar compliances
            foreach (var complianceRequest in request.Compliances)
            {
                var compliance = new ProjectCompliance
                {
                    TipoCompliance = complianceRequest.TipoCompliance,
                    Observacoes = complianceRequest.Observacoes
                };
                details.Compliances.Add(compliance);
            }

            // Adicionar períodos indisponíveis
            foreach (var periodRequest in request.UnavailablePeriods)
            {
                if (periodRequest.DataFim < periodRequest.DataInicio);

                var period = new ProjectUnavailablePeriod
                {
                    DataInicio = periodRequest.DataInicio,
                    DataFim = periodRequest.DataFim,
                    Motivo = periodRequest.Motivo
                };
                details.UnavailablePeriods.Add(period);
            }

            // Associar detalhes ao projeto diretamente via repositório
            await _projectRepository.AddProjectDetailsAsync(details, cancellationToken);

            // Adicionar dependências externas
            if (details.TemDependenciasExternas && request.Dependencies.Any())
            {
                foreach (var depRequest in request.Dependencies)
                {
                    var dependency = new ProjectDependencies
                    {
                        ProjectId = projectId,
                        Nome = depRequest.Nome,
                        Descricao = depRequest.Descricao,
                        Prazo = depRequest.Prazo,
                        Criticidade = depRequest.Criticidade
                    };
                    await _projectRepository.AddDependencyAsync(dependency, cancellationToken);
                }
            }

            // Adicionar integrações
            if (details.TemIntegracoes && request.Integrations.Any())
            {
                foreach (var intRequest in request.Integrations)
                {
                    var integration = new ProjectIntegrations
                    {
                        ProjectId = projectId,
                        NomeSistema = intRequest.NomeSistema,
                        Tipo = intRequest.Tipo,
                        Criticidade = intRequest.Criticidade,
                        Status = intRequest.Status
                    };
                    await _projectRepository.AddIntegrationAsync(integration, cancellationToken);
                }
            }

            // Adicionar dados sensíveis
            if (request.SensitiveData.Any())
            {
                var temDadosPublicos = details.Compliances.Any(c => c.TipoCompliance == ComplianceType.DadosPublicos);
                if (temDadosPublicos)
                    return Result<ProjectDetailsDto>.Failure("Projeto com compliance de Dados Públicos não pode registrar dados sensíveis.");

                var tiposAdicionados = new HashSet<SensitiveDataType>();
                foreach (var sdRequest in request.SensitiveData)
                {
                    if (!tiposAdicionados.Add(sdRequest.TipoDadoSensivel))
                        continue;

                    var sensitiveData = new ProjectSensitiveData
                    {
                        ProjectId = projectId,
                        TipoDadoSensivel = sdRequest.TipoDadoSensivel
                    };
                    await _projectRepository.AddSensitiveDataAsync(sensitiveData, cancellationToken);
                }
            }

            await _unitOfWork.CommitAsync(cancellationToken);

            project = await _projectRepository.GetByIdAsync(projectId, cancellationToken);
            return Result<ProjectDetailsDto>.Success(project!.ProjectDetails!.ToDto(
                project.Dependencies.Where(d => d.IsActive),
                project.Integrations.Where(i => i.IsActive),
                project.SensitiveData.Where(s => s.IsActive)), "Detalhes do projeto criados com sucesso");
        }
        catch (Exception ex)
        {
            return Result<ProjectDetailsDto>.Failure($"Erro ao criar detalhes do projeto: {ex.Message}");
        }
    }

    public async Task<Result<ProjectDetailsDto>> UpdateProjectDetailsAsync(
        Guid projectId,
        UpdateProjectDetailsRequest request,
        Guid? actorUserId,
        UserRole? actorRole,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var actorResult = await ResolveActorAsync(actorUserId, actorRole, cancellationToken);
            if (!actorResult.IsSuccess)
                return Result<ProjectDetailsDto>.Failure(actorResult.Message);
            var actor = actorResult.Data;

            var project = await _projectRepository.GetByIdAsync(projectId, cancellationToken);
            if (project == null)
                return Result<ProjectDetailsDto>.Failure($"Projeto não encontrado com ID {projectId}");

            if (actorRole != UserRole.ADM_MASTER && actor?.CompanyId != project.CompanyId)
                return Result<ProjectDetailsDto>.Failure("Sem permissão para atualizar detalhes de projeto de outra empresa.");

            if (project.ProjectDetails == null)
                return Result<ProjectDetailsDto>.Failure("Projeto não possui detalhes cadastrados.");

            if (request.Orcamento == BudgetType.ValorFixo && request.ValorOrcamento == null)
                return Result<ProjectDetailsDto>.Failure("ValorOrcamento é obrigatório quando Orçamento = ValorFixo.");

            if (request.DowntimePermitido == DowntimeType.AteXHoras && request.HorasDowntime == null)
                return Result<ProjectDetailsDto>.Failure("HorasDowntime é obrigatório quando DowntimePermitido = AteXHoras.");

            project.ProjectDetails.UpdateOperationalSettings(
                request.TemDependenciasExternas,
                request.TemIntegracoes,
                request.Orcamento,
                request.HorarioTrabalho,
                request.DowntimePermitido,
                request.Orcamento == BudgetType.ValorFixo ? request.ValorOrcamento : null,
                request.DowntimePermitido == DowntimeType.AteXHoras ? request.HorasDowntime : null
            );

            // Sincronizar dependências externas
            if (project.ProjectDetails.TemDependenciasExternas && request.Dependencies.Any())
            {
                var nomesRequested = request.Dependencies
                    .Select(d => d.Nome.Trim().ToLowerInvariant())
                    .ToHashSet();

                foreach (var dep in project.Dependencies.Where(d => d.IsActive && !nomesRequested.Contains(d.Nome.Trim().ToLowerInvariant())).ToList())
                    dep.Deactivate();

                foreach (var depRequest in request.Dependencies)
                {
                    var existing = project.Dependencies.FirstOrDefault(d => d.Nome.Trim().ToLowerInvariant() == depRequest.Nome.Trim().ToLowerInvariant());
                    if (existing != null)
                    {
                        if (!existing.IsActive)
                            existing.Activate();
                        existing.UpdateInfo(depRequest.Nome, depRequest.Descricao, depRequest.Prazo, depRequest.Criticidade);
                    }
                    else
                    {
                        var dependency = new ProjectDependencies
                        {
                            ProjectId = projectId,
                            Nome = depRequest.Nome,
                            Descricao = depRequest.Descricao,
                            Prazo = depRequest.Prazo,
                            Criticidade = depRequest.Criticidade
                        };
                        await _projectRepository.AddDependencyAsync(dependency, cancellationToken);
                    }
                }
            }
            else
            {
                foreach (var dep in project.Dependencies.Where(d => d.IsActive).ToList())
                    dep.Deactivate();
            }

            // Sincronizar integrações
            if (project.ProjectDetails.TemIntegracoes && request.Integrations.Any())
            {
                var sistemasRequested = request.Integrations
                    .Select(i => i.NomeSistema.Trim().ToLowerInvariant())
                    .ToHashSet();

                foreach (var integration in project.Integrations.Where(i => i.IsActive && !sistemasRequested.Contains(i.NomeSistema.Trim().ToLowerInvariant())).ToList())
                    integration.Deactivate();

                foreach (var intRequest in request.Integrations)
                {
                    var existing = project.Integrations.FirstOrDefault(i => i.NomeSistema.Trim().ToLowerInvariant() == intRequest.NomeSistema.Trim().ToLowerInvariant());
                    if (existing != null)
                    {
                        if (!existing.IsActive)
                            existing.Activate();
                        existing.UpdateInfo(intRequest.NomeSistema, intRequest.Tipo, intRequest.Criticidade, intRequest.Status);
                    }
                    else
                    {
                        var integration = new ProjectIntegrations
                        {
                            ProjectId = projectId,
                            NomeSistema = intRequest.NomeSistema,
                            Tipo = intRequest.Tipo,
                            Criticidade = intRequest.Criticidade,
                            Status = intRequest.Status
                        };
                        await _projectRepository.AddIntegrationAsync(integration, cancellationToken);
                    }
                }
            }
            else
            {
                foreach (var integration in project.Integrations.Where(i => i.IsActive).ToList())
                    integration.Deactivate();
            }

            // Sincronizar dados sensíveis
            if (request.SensitiveData.Any())
            {
                var temDadosPublicos = project.ProjectDetails.Compliances
                    .Any(c => c.TipoCompliance == ComplianceType.DadosPublicos && c.IsActive);
                if (temDadosPublicos)
                    return Result<ProjectDetailsDto>.Failure("Projeto com compliance de Dados Públicos não pode registrar dados sensíveis.");

                var tiposRequested = request.SensitiveData
                    .Select(sd => sd.TipoDadoSensivel)
                    .Distinct()
                    .ToHashSet();

                foreach (var sd in project.SensitiveData.Where(s => s.IsActive && !tiposRequested.Contains(s.TipoDadoSensivel)).ToList())
                    sd.Deactivate();

                foreach (var tipo in tiposRequested)
                {
                    var existing = project.SensitiveData.FirstOrDefault(s => s.TipoDadoSensivel == tipo);
                    if (existing != null)
                    {
                        if (!existing.IsActive)
                            existing.Activate();
                    }
                    else
                    {
                        var sensitiveData = new ProjectSensitiveData
                        {
                            ProjectId = projectId,
                            TipoDadoSensivel = tipo
                        };
                        await _projectRepository.AddSensitiveDataAsync(sensitiveData, cancellationToken);
                    }
                }
            }
            else
            {
                foreach (var sd in project.SensitiveData.Where(s => s.IsActive).ToList())
                    sd.Deactivate();
            }

            await _unitOfWork.CommitAsync(cancellationToken);

            project = await _projectRepository.GetByIdAsync(projectId, cancellationToken);
            return Result<ProjectDetailsDto>.Success(project!.ProjectDetails!.ToDto(
                project.Dependencies.Where(d => d.IsActive),
                project.Integrations.Where(i => i.IsActive),
                project.SensitiveData.Where(s => s.IsActive)), "Detalhes do projeto atualizados com sucesso");
        }
        catch (Exception ex)
        {
            return Result<ProjectDetailsDto>.Failure($"Erro ao atualizar detalhes do projeto: {ex.Message}");
        }
    }

    public async Task<Result<ProjectDetailsDto>> GetProjectDetailsAsync(
        Guid projectId,
        Guid? actorUserId,
        UserRole? actorRole,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var actorResult = await ResolveActorAsync(actorUserId, actorRole, cancellationToken);
            if (!actorResult.IsSuccess)
                return Result<ProjectDetailsDto>.Failure(actorResult.Message);
            var actor = actorResult.Data;

            var project = await _projectRepository.GetByIdAsync(projectId, cancellationToken);
            if (project == null)
                return Result<ProjectDetailsDto>.Failure($"Projeto não encontrado com ID {projectId}");

            if (actorRole != UserRole.ADM_MASTER && actor?.CompanyId != project.CompanyId)
                return Result<ProjectDetailsDto>.Failure("Sem permissão para visualizar detalhes de projeto de outra empresa.");

            if (project.ProjectDetails == null)
                return Result<ProjectDetailsDto>.Failure("Projeto não possui detalhes cadastrados.");

            return Result<ProjectDetailsDto>.Success(project.ProjectDetails.ToDto(
                project.Dependencies.Where(d => d.IsActive),
                project.Integrations.Where(i => i.IsActive),
                project.SensitiveData.Where(s => s.IsActive)));
        }
        catch (Exception ex)
        {
            return Result<ProjectDetailsDto>.Failure($"Erro ao buscar detalhes do projeto: {ex.Message}");
        }
    }

    public async Task<Result<ProjectComplianceDto>> AddComplianceAsync(
        Guid projectId,
        CreateProjectComplianceRequest request,
        Guid? actorUserId,
        UserRole? actorRole,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var actorResult = await ResolveActorAsync(actorUserId, actorRole, cancellationToken);
            if (!actorResult.IsSuccess)
                return Result<ProjectComplianceDto>.Failure(actorResult.Message);
            var actor = actorResult.Data;

            var project = await _projectRepository.GetByIdAsync(projectId, cancellationToken);
            if (project == null)
                return Result<ProjectComplianceDto>.Failure($"Projeto não encontrado com ID {projectId}");

            if (actorRole != UserRole.ADM_MASTER && actor?.CompanyId != project.CompanyId)
                return Result<ProjectComplianceDto>.Failure("Sem permissão para adicionar compliance a projeto de outra empresa.");

            if (project.ProjectDetails == null)
                return Result<ProjectComplianceDto>.Failure("Projeto não possui detalhes cadastrados. Crie os detalhes primeiro.");

            var compliance = new ProjectCompliance
            {
                ProjectDetailsId = project.ProjectDetails.Id,
                TipoCompliance = request.TipoCompliance,
                Observacoes = request.Observacoes
            };

            await _projectRepository.AddComplianceAsync(compliance, cancellationToken);
            await _unitOfWork.CommitAsync(cancellationToken);

            return Result<ProjectComplianceDto>.Success(compliance.ToDto(), "Compliance adicionado com sucesso");
        }
        catch (Exception ex)
        {
            return Result<ProjectComplianceDto>.Failure($"Erro ao adicionar compliance: {ex.Message}");
        }
    }

    public async Task<Result> RemoveComplianceAsync(
        Guid projectId,
        Guid complianceId,
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

            var project = await _projectRepository.GetByIdAsync(projectId, cancellationToken);
            if (project == null)
                return Result.Failure($"Projeto não encontrado com ID {projectId}");

            if (actorRole != UserRole.ADM_MASTER && actor?.CompanyId != project.CompanyId)
                return Result.Failure("Sem permissão para remover compliance de projeto de outra empresa.");

            if (project.ProjectDetails == null)
                return Result.Failure("Projeto não possui detalhes cadastrados.");

            project.ProjectDetails.RemoveCompliance(complianceId);
            await _unitOfWork.CommitAsync(cancellationToken);

            return Result.Success("Compliance removido com sucesso");
        }
        catch (Exception ex)
        {
            return Result.Failure($"Erro ao remover compliance: {ex.Message}");
        }
    }

    public async Task<Result<ProjectUnavailablePeriodDto>> AddUnavailablePeriodAsync(
        Guid projectId,
        CreateProjectUnavailablePeriodRequest request,
        Guid? actorUserId,
        UserRole? actorRole,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var actorResult = await ResolveActorAsync(actorUserId, actorRole, cancellationToken);
            if (!actorResult.IsSuccess)
                return Result<ProjectUnavailablePeriodDto>.Failure(actorResult.Message);
            var actor = actorResult.Data;

            var project = await _projectRepository.GetByIdAsync(projectId, cancellationToken);
            if (project == null)
                return Result<ProjectUnavailablePeriodDto>.Failure($"Projeto não encontrado com ID {projectId}");

            if (actorRole != UserRole.ADM_MASTER && actor?.CompanyId != project.CompanyId)
                return Result<ProjectUnavailablePeriodDto>.Failure("Sem permissão para adicionar período indisponível a projeto de outra empresa.");

            if (project.ProjectDetails == null)
                return Result<ProjectUnavailablePeriodDto>.Failure("Projeto não possui detalhes cadastrados. Crie os detalhes primeiro.");

            if (request.DataFim < request.DataInicio)
                return Result<ProjectUnavailablePeriodDto>.Failure("Data fim não pode ser anterior à data início.");

            var period = new ProjectUnavailablePeriod
            {
                ProjectDetailsId = project.ProjectDetails.Id,
                DataInicio = request.DataInicio,
                DataFim = request.DataFim,
                Motivo = request.Motivo
            };

            await _projectRepository.AddUnavailablePeriodAsync(period, cancellationToken);
            await _unitOfWork.CommitAsync(cancellationToken);

            return Result<ProjectUnavailablePeriodDto>.Success(period.ToDto(), "Período indisponível adicionado com sucesso");
        }
        catch (Exception ex)
        {
            return Result<ProjectUnavailablePeriodDto>.Failure($"Erro ao adicionar período indisponível: {ex.Message}");
        }
    }

    public async Task<Result> RemoveUnavailablePeriodAsync(
        Guid projectId,
        Guid periodId,
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

            var project = await _projectRepository.GetByIdAsync(projectId, cancellationToken);
            if (project == null)
                return Result.Failure($"Projeto não encontrado com ID {projectId}");

            if (actorRole != UserRole.ADM_MASTER && actor?.CompanyId != project.CompanyId)
                return Result.Failure("Sem permissão para remover período indisponível de projeto de outra empresa.");

            if (project.ProjectDetails == null)
                return Result.Failure("Projeto não possui detalhes cadastrados.");

            project.ProjectDetails.RemoveUnavailablePeriod(periodId);
            await _unitOfWork.CommitAsync(cancellationToken);

            return Result.Success("Período indisponível removido com sucesso");
        }
        catch (Exception ex)
        {
            return Result.Failure($"Erro ao remover período indisponível: {ex.Message}");
        }
    }

    public async Task<Result<ProjectExecutionSettingsDto>> CreateExecutionSettingsAsync(
        Guid projectId,
        CreateProjectExecutionSettingsRequest request,
        Guid? actorUserId,
        UserRole? actorRole,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var actorResult = await ResolveActorAsync(actorUserId, actorRole, cancellationToken);
            if (!actorResult.IsSuccess)
                return Result<ProjectExecutionSettingsDto>.Failure(actorResult.Message);
            var actor = actorResult.Data;

            var project = await _projectRepository.GetByIdAsync(projectId, cancellationToken);
            if (project == null)
                return Result<ProjectExecutionSettingsDto>.Failure($"Projeto não encontrado com ID {projectId}");

            if (actorRole != UserRole.ADM_MASTER && actor?.CompanyId != project.CompanyId)
                return Result<ProjectExecutionSettingsDto>.Failure("Sem permissão para adicionar configurações a projeto de outra empresa.");

            if (project.ExecutionSettings != null)
                return Result<ProjectExecutionSettingsDto>.Failure("Projeto já possui configurações de execução. Use a operação de atualização.");

            if (request.ExperienciaEquipe == ProjectExperienceType.AlgoSimilar)
            {
                if (string.IsNullOrWhiteSpace(request.OQueDeuCerto))
                    return Result<ProjectExecutionSettingsDto>.Failure("OQueDeuCerto é obrigatório quando ExperienciaEquipe = AlgoSimilar.");
                if (string.IsNullOrWhiteSpace(request.OQueDeuErrado))
                    return Result<ProjectExecutionSettingsDto>.Failure("OQueDeuErrado é obrigatório quando ExperienciaEquipe = AlgoSimilar.");
            }

            if (request.PrioridadesOrdenadas.Select(p => p.PriorityType).Distinct().Count() != request.PrioridadesOrdenadas.Count)
                return Result<ProjectExecutionSettingsDto>.Failure("Não é permitido informar o mesmo tipo de prioridade mais de uma vez.");

            var settings = new ProjectExecutionSettings();
            settings.ProjectId = projectId;
            settings.UpdateSettings(
                request.ExperienciaEquipe,
                request.NivelDetalhePlano,
                request.FrequenciaRevisao,
                request.MaiorRisco,
                request.Observacoes,
                request.OQueDeuCerto,
                request.OQueDeuErrado);

            await _projectRepository.AddExecutionSettingsAsync(settings, cancellationToken);

            foreach (var prioRequest in request.PrioridadesOrdenadas.DistinctBy(p => p.PriorityType))
            {
                var ranking = new ProjectPriorityRanking
                {
                    ProjectId = projectId,
                    PriorityType = prioRequest.PriorityType,
                    Posicao = prioRequest.Posicao
                };
                await _projectRepository.AddPriorityRankingAsync(ranking, cancellationToken);
            }

            await _unitOfWork.CommitAsync(cancellationToken);

            project = await _projectRepository.GetByIdAsync(projectId, cancellationToken);
            return Result<ProjectExecutionSettingsDto>.Success(
                project!.ExecutionSettings!.ToDto(project.PriorityRankings.Where(r => r.IsActive)),
                "Configurações de execução criadas com sucesso");
        }
        catch (Exception ex)
        {
            return Result<ProjectExecutionSettingsDto>.Failure($"Erro ao criar configurações de execução: {ex.Message}");
        }
    }

    public async Task<Result<ProjectExecutionSettingsDto>> UpdateExecutionSettingsAsync(
        Guid projectId,
        UpdateProjectExecutionSettingsRequest request,
        Guid? actorUserId,
        UserRole? actorRole,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var actorResult = await ResolveActorAsync(actorUserId, actorRole, cancellationToken);
            if (!actorResult.IsSuccess)
                return Result<ProjectExecutionSettingsDto>.Failure(actorResult.Message);
            var actor = actorResult.Data;

            var project = await _projectRepository.GetByIdAsync(projectId, cancellationToken);
            if (project == null)
                return Result<ProjectExecutionSettingsDto>.Failure($"Projeto não encontrado com ID {projectId}");

            if (actorRole != UserRole.ADM_MASTER && actor?.CompanyId != project.CompanyId)
                return Result<ProjectExecutionSettingsDto>.Failure("Sem permissão para atualizar configurações de projeto de outra empresa.");

            if (project.ExecutionSettings == null)
                return Result<ProjectExecutionSettingsDto>.Failure("Projeto não possui configurações de execução. Use a operação de criação.");

            if (request.ExperienciaEquipe == ProjectExperienceType.AlgoSimilar)
            {
                if (string.IsNullOrWhiteSpace(request.OQueDeuCerto))
                    return Result<ProjectExecutionSettingsDto>.Failure("OQueDeuCerto é obrigatório quando ExperienciaEquipe = AlgoSimilar.");
                if (string.IsNullOrWhiteSpace(request.OQueDeuErrado))
                    return Result<ProjectExecutionSettingsDto>.Failure("OQueDeuErrado é obrigatório quando ExperienciaEquipe = AlgoSimilar.");
            }

            if (request.PrioridadesOrdenadas.Select(p => p.PriorityType).Distinct().Count() != request.PrioridadesOrdenadas.Count)
                return Result<ProjectExecutionSettingsDto>.Failure("Não é permitido informar o mesmo tipo de prioridade mais de uma vez.");

            project.ExecutionSettings.UpdateSettings(
                request.ExperienciaEquipe,
                request.NivelDetalhePlano,
                request.FrequenciaRevisao,
                request.MaiorRisco,
                request.Observacoes,
                request.OQueDeuCerto,
                request.OQueDeuErrado);

            // Sincronizar prioridades
            var tiposRequested = request.PrioridadesOrdenadas
                .Select(p => p.PriorityType)
                .ToHashSet();

            foreach (var ranking in project.PriorityRankings.Where(r => r.IsActive && !tiposRequested.Contains(r.PriorityType)).ToList())
                ranking.Deactivate();

            foreach (var prioRequest in request.PrioridadesOrdenadas.DistinctBy(p => p.PriorityType))
            {
                var existing = project.PriorityRankings.FirstOrDefault(r => r.PriorityType == prioRequest.PriorityType);
                if (existing != null)
                {
                    if (!existing.IsActive)
                        existing.Activate();
                    existing.UpdatePosicao(prioRequest.Posicao);
                }
                else
                {
                    var ranking = new ProjectPriorityRanking
                    {
                        ProjectId = projectId,
                        PriorityType = prioRequest.PriorityType,
                        Posicao = prioRequest.Posicao
                    };
                    await _projectRepository.AddPriorityRankingAsync(ranking, cancellationToken);
                }
            }

            await _unitOfWork.CommitAsync(cancellationToken);

            project = await _projectRepository.GetByIdAsync(projectId, cancellationToken);
            return Result<ProjectExecutionSettingsDto>.Success(
                project!.ExecutionSettings!.ToDto(project.PriorityRankings.Where(r => r.IsActive)),
                "Configurações de execução atualizadas com sucesso");
        }
        catch (Exception ex)
        {
            return Result<ProjectExecutionSettingsDto>.Failure($"Erro ao atualizar configurações de execução: {ex.Message}");
        }
    }

    public async Task<Result<ProjectExecutionSettingsDto>> GetExecutionSettingsAsync(
        Guid projectId,
        Guid? actorUserId,
        UserRole? actorRole,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var actorResult = await ResolveActorAsync(actorUserId, actorRole, cancellationToken);
            if (!actorResult.IsSuccess)
                return Result<ProjectExecutionSettingsDto>.Failure(actorResult.Message);
            var actor = actorResult.Data;

            var project = await _projectRepository.GetByIdAsync(projectId, cancellationToken);
            if (project == null)
                return Result<ProjectExecutionSettingsDto>.Failure($"Projeto não encontrado com ID {projectId}");

            if (actorRole != UserRole.ADM_MASTER && actor?.CompanyId != project.CompanyId)
                return Result<ProjectExecutionSettingsDto>.Failure("Sem permissão para visualizar configurações de projeto de outra empresa.");

            if (project.ExecutionSettings == null)
                return Result<ProjectExecutionSettingsDto>.Failure("Projeto não possui configurações de execução cadastradas.");

            return Result<ProjectExecutionSettingsDto>.Success(
                project.ExecutionSettings.ToDto(project.PriorityRankings.Where(r => r.IsActive)));
        }
        catch (Exception ex)
        {
            return Result<ProjectExecutionSettingsDto>.Failure($"Erro ao buscar configurações de execução: {ex.Message}");
        }
    }

    public async Task<Result<ProjectCompleteDto>> GetCompleteAsync(
        Guid id,
        Guid? actorUserId,
        UserRole? actorRole,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var actorResult = await ResolveActorAsync(actorUserId, actorRole, cancellationToken);
            if (!actorResult.IsSuccess)
                return Result<ProjectCompleteDto>.Failure(actorResult.Message);
            var actor = actorResult.Data;

            var project = await _projectRepository.GetByIdAsync(id, cancellationToken);
            if (project == null)
                return Result<ProjectCompleteDto>.Failure($"Projeto não encontrado. Não foi encontrado projeto com ID {id}");

            if (actorRole != UserRole.ADM_MASTER && actor?.CompanyId != project.CompanyId)
                return Result<ProjectCompleteDto>.Failure("Sem permissão para acessar projeto de outra empresa.");

            return Result<ProjectCompleteDto>.Success(project.ToCompleteDto());
        }
        catch (Exception ex)
        {
            return Result<ProjectCompleteDto>.Failure($"Erro ao buscar projeto completo: {ex.Message}");
        }
    }
}