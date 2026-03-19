using Application.Core.DTOs.Users;
using Application.Core.Interfaces.Services;
using Domain.Common;
using Domain.Entities;
using Domain.Enums;
using Domain.Interfaces;
using System.Text.Json;

namespace Application.Core.Services;

/// <summary>
/// Service de Users
/// Contém toda a lógica de negócio relacionada a usuários
/// Novas senhas sempre usarão BCrypt
/// </summary>
public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly HttpClient _httpClient;

    public UserService(
        IUserRepository userRepository, 
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        HttpClient httpClient)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _httpClient = httpClient;
    }

    /// <summary>
    /// Cria um novo usuário
    /// Senha padrão será hasheada com BCrypt
    /// </summary>
    public async Task<Result<UserDto>> CreateAsync(
        CreateUserRequest request,
        Guid? actorUserId,
        UserRole? actorRole,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var anyUsers = await _userRepository.AnyAsync(cancellationToken);

            // Regras de permissão por perfil/logado
            var actorResult = await ResolveActorAsync(actorUserId, actorRole, cancellationToken);
            if (!actorResult.IsSuccess)
                return Result<UserDto>.Failure(actorResult.Message);
            var actor = actorResult.Data;

            // 1. Validar se email já existe
            var emailExists = await _userRepository.EmailExistsAsync(request.Email, cancellationToken);
            if (emailExists)
            {
                return Result<UserDto>.Failure(
                    "Email já cadastrado. Escolha outro email para o usuário.");
            }

            // 2. Validar se CPF já existe
            var cpfExists = await _userRepository.CPFExistsAsync(request.CPF, cancellationToken);
            if (cpfExists)
            {
                return Result<UserDto>.Failure(
                    "CPF já cadastrado. Escolha outro CPF.");
            }

            // 3. Criar entidade User
            var targetRole = UserRole.USER;
            if (!string.IsNullOrWhiteSpace(request.Role))
            {
                if (!Enum.TryParse<UserRole>(request.Role, true, out targetRole))
                {
                    return Result<UserDto>.Failure("Perfil inválido. Valores aceitos: ADM_MASTER, ADM, USER");
                }
            }

            if (actorRole == UserRole.USER)
            {
                return Result<UserDto>.Failure("Usuários padrão não podem criar outros usuários.");
            }

            // ADM só pode criar USER e sempre dentro da própria empresa
            if (actorRole == UserRole.ADM)
            {
                if (targetRole != UserRole.USER)
                    return Result<UserDto>.Failure("ADM só pode criar usuários do tipo USER.");
            }

            // ADM_MASTER pode criar ADM e USER (não pode criar ADM_MASTER)
            if (actorRole == UserRole.ADM_MASTER)
            {
                var canCreate = targetRole == UserRole.ADM || targetRole == UserRole.USER;
                if (!canCreate)
                    return Result<UserDto>.Failure("ADM_MASTER só pode criar ADM ou USER.");
            }

            // Regra de bootstrap: primeiro usuário do sistema vira ADM_MASTER
            if (!anyUsers)
            {
                targetRole = UserRole.ADM_MASTER;
            }

            var user = new User
            {
                Name = request.Name,
                Email = request.Email.ToLower(),
                Phone = request.Phone,
                CPF = request.CPF.Replace(".", "").Replace("-", "").Trim(),
                BirthDate = request.BirthDate,
                RoleId = (int)targetRole,
                CompanyId = actorRole == UserRole.ADM ? actor?.CompanyId : null
            };

            // 4. Hash da senha padrão (data de nascimento) 
            var defaultPassword = user.GetDefaultPassword();
            user.PasswordHash = _passwordHasher.HashPassword(defaultPassword);

            // 5. Adicionar ao repositório
            await _userRepository.AddAsync(user, cancellationToken);

            // 6. Salvar alterações
            await _unitOfWork.CommitAsync(cancellationToken);

            // 7. Recarregar com navegação Role para o DTO
            var createdUser = await _userRepository.GetByIdAsync(user.Id, cancellationToken);
            if (createdUser == null)
                return Result<UserDto>.Failure("Erro ao buscar usuário criado");

            // 8. Retornar DTO
            return Result<UserDto>.Success(
                MapToDto(createdUser), 
                $"Usuário criado com sucesso. Senha padrão: {defaultPassword} (alterar no primeiro acesso)");
        }
        catch (Exception ex)
        {
            // TODO: Implementar logging aqui
            return Result<UserDto>.Failure($"Erro ao criar usuário: {ex.Message}");
        }
    }

    /// <summary>
    /// Busca usuário por ID
    /// </summary>
    public async Task<Result<UserDto>> GetByIdAsync(
        Guid id,
        Guid? actorUserId,
        UserRole? actorRole,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var actorResult = await ResolveActorAsync(actorUserId, actorRole, cancellationToken);
            if (!actorResult.IsSuccess)
                return Result<UserDto>.Failure(actorResult.Message);
            var actor = actorResult.Data;

            var user = await _userRepository.GetByIdAsync(id, cancellationToken);
            if (user == null)
            {
                return Result<UserDto>.Failure($"Usuário não encontrado com ID {id}");
            }

            if (actorRole == UserRole.ADM && actor?.CompanyId != user.CompanyId)
            {
                return Result<UserDto>.Failure("Sem permissão para acessar usuário de outra empresa.");
            }

            return Result<UserDto>.Success(MapToDto(user));
        }
        catch (Exception ex)
        {
            // TODO: Implementar logging aqui
            return Result<UserDto>.Failure($"Erro ao buscar usuário: {ex.Message}");
        }
    }

    /// <summary>
    /// Lista todos os usuários ativos
    /// </summary>
    public async Task<Result<IEnumerable<UserDto>>> GetAllAsync(
        Guid? actorUserId,
        UserRole? actorRole,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var actorResult = await ResolveActorAsync(actorUserId, actorRole, cancellationToken);
            if (!actorResult.IsSuccess)
                return Result<IEnumerable<UserDto>>.Failure(actorResult.Message);
            var actor = actorResult.Data;

            IEnumerable<User> users;
            
            // ADM só vê usuários da própria empresa
            if (actorRole == UserRole.ADM && actor?.CompanyId != null)
            {
                users = await _userRepository.GetByCompanyIdAsync(actor.CompanyId.Value, cancellationToken);
            }
            else
            {
                users = await _userRepository.GetAllAsync(cancellationToken);
            }

            var userDtos = users.Where(u => u.IsActive).Select(MapToDto).ToList();
            
            return Result<IEnumerable<UserDto>>.Success(
                userDtos,
                $"{userDtos.Count} usuário(s) encontrado(s)");
        }
        catch (Exception ex)
        {
            // TODO: Implementar logging aqui
            return Result<IEnumerable<UserDto>>.Failure($"Erro ao listar usuários: {ex.Message}");
        }
    }

    /// <summary>
    /// Atualiza informações do usuário
    /// </summary>
    public async Task<Result<UserDto>> UpdateAsync(
        UpdateUserRequest request,
        Guid? actorUserId,
        UserRole? actorRole,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var actorResult = await ResolveActorAsync(actorUserId, actorRole, cancellationToken);
            if (!actorResult.IsSuccess)
                return Result<UserDto>.Failure(actorResult.Message);
            var actor = actorResult.Data;

            var user = await _userRepository.GetByIdAsync(request.Id, cancellationToken);
            if (user == null)
            {
                return Result<UserDto>.Failure($"Usuário não encontrado com ID {request.Id}");
            }

            // Validação de permissão: ADM só edita usuários da própria empresa
            if (actorRole == UserRole.ADM && actor?.CompanyId != user.CompanyId)
            {
                return Result<UserDto>.Failure("ADM não pode editar usuários de outra empresa.");
            }

            if (!user.IsActive)
            {
                return Result<UserDto>.Failure("Usuário está desativado e não pode ser atualizado");
            }

            // Atualizar informações
            user.UpdateProfile(request.Name, request.Phone);

            // Persistir alterações
            await _userRepository.UpdateAsync(user, cancellationToken);
            await _unitOfWork.CommitAsync(cancellationToken);

            return Result<UserDto>.Success(MapToDto(user), "Usuário atualizado com sucesso");
        }
        catch (Exception ex)
        {
            // TODO: Implementar logging aqui
            return Result<UserDto>.Failure($"Erro ao atualizar usuário: {ex.Message}");
        }
    }

    /// <summary>
    /// Desativa um usuário (soft delete)
    /// </summary>
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

            var user = await _userRepository.GetByIdAsync(id, cancellationToken);
            if (user == null)
            {
                return Result.Failure($"Usuário não encontrado com ID {id}");
            }

            // Validação de permissão: ADM só desativa usuários da própria empresa
            if (actorRole == UserRole.ADM && actor?.CompanyId != user.CompanyId)
            {
                return Result.Failure("ADM não pode desativar usuários de outra empresa.");
            }

            if (!user.IsActive)
            {
                return Result.Success("Usuário já está desativado");
            }

            // Soft delete
            user.SoftDelete();

            // Persistir alterações
            await _userRepository.UpdateAsync(user, cancellationToken);
            await _unitOfWork.CommitAsync(cancellationToken);

            return Result.Success("Usuário desativado com sucesso");
        }
        catch (Exception ex)
        {
            // TODO: Implementar logging aqui
            return Result.Failure($"Erro ao desativar usuário: {ex.Message}");
        }
    }

    /// <summary>
    /// Busca usuários por email
    /// </summary>
    public async Task<Result<IEnumerable<UserDto>>> FindByEmailAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _userRepository.GetByEmailAsync(email, cancellationToken);
            if (user == null)
            {
                return Result<IEnumerable<UserDto>>.Success(
                    Enumerable.Empty<UserDto>(),
                    "Nenhum usuário encontrado");
            }

            var users = new List<UserDto> { MapToDto(user) };
            return Result<IEnumerable<UserDto>>.Success(users, "Usuário encontrado");
        }
        catch (Exception ex)
        {
            // TODO: Implementar logging aqui
            return Result<IEnumerable<UserDto>>.Failure($"Erro ao buscar usuário por email: {ex.Message}");
        }
    }

    /// <summary>
    /// Verifica se email já está em uso
    /// </summary>
    public async Task<bool> EmailExistsAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _userRepository.EmailExistsAsync(email, cancellationToken);
        }
        catch
        {
            // TODO: Implementar logging aqui
            return false;
        }
    }

    /// <summary>
    /// Consulta endereço pelo CEP via API ViaCEP
    /// </summary>
    public async Task<Result<ViaCEp>> GetAddressByCepAsync(
        string cep,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // 1. Validar CEP
            if (string.IsNullOrWhiteSpace(cep))
            {
                return Result<ViaCEp>.Failure("CEP é obrigatório");
            }

            // 2. Normalizar CEP (remover caracteres especiais)
            var normalizedCep = NormalizeCep(cep);

            // 3. Validar formato do CEP (deve ter 8 dígitos)
            if (normalizedCep.Length != 8 || !normalizedCep.All(char.IsDigit))
            {
                return Result<ViaCEp>.Failure("CEP inválido. Deve conter 8 dígitos");
            }

            // 4. Fazer requisição para API ViaCEP
            var response = await _httpClient.GetAsync(
                $"https://viacep.com.br/ws/{normalizedCep}/json/",
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return Result<ViaCEp>.Failure($"Erro ao consultar CEP: {response.StatusCode}");
            }

            // 5. Deserializar resposta
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var viaCepData = JsonSerializer.Deserialize<ViaCepResponse>(content, options);

            if (viaCepData == null)
            {
                return Result<ViaCEp>.Failure("Erro ao processar resposta da API ViaCEP");
            }

            // 6. Verificar se CEP foi encontrado
            if (viaCepData.Erro)
            {
                return Result<ViaCEp>.Failure("CEP não encontrado");
            }

            // 7. Mapear para DTO
            var result = new ViaCEp(
                Cep: viaCepData.Cep,
                Logradouro: viaCepData.Logradouro,
                Complemento: viaCepData.Complemento,
                unidade: viaCepData.Unidade,
                localidade: viaCepData.Localidade,
                uf: viaCepData.Uf,
                estado: viaCepData.Estado,
                regioao: viaCepData.Regiao,
                ibge: viaCepData.Ibge,
                gia: viaCepData.Gia,
                ddd: viaCepData.Ddd,
                siafi: viaCepData.Siafi
            );

            return Result<ViaCEp>.Success(result, "CEP consultado com sucesso");
        }
        catch (HttpRequestException ex)
        {
            return Result<ViaCEp>.Failure($"Erro ao conectar com API ViaCEP: {ex.Message}");
        }
        catch (JsonException ex)
        {
            return Result<ViaCEp>.Failure($"Erro ao processar resposta da API: {ex.Message}");
        }
        catch (Exception ex)
        {
            return Result<ViaCEp>.Failure($"Erro ao consultar CEP: {ex.Message}");
        }
    }

    /// <summary>
    /// Normaliza CEP removendo caracteres especiais
    /// </summary>
    private static string NormalizeCep(string cep)
    {
        return cep.Replace("-", "").Replace(".", "").Replace(" ", "").Trim();
    }

    /// <summary>
    /// Mapeia entidade User para UserDto
    /// Usa Role.RoleName do relacionamento carregado
    /// </summary>
    private static UserDto MapToDto(User user)
    {
        return new UserDto
        {
            Id = user.Id,
            CompanyId = user.CompanyId,
            Name = user.Name,
            Email = user.Email,
            Phone = user.Phone,
            CPF = user.CPF,
            BirthDate = user.BirthDate,
            Role = user.Role?.RoleName ?? "USER",
            IsEmailVerified = user.IsEmailVerified,
            IsFirstAccess = user.IsFirstAccess,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        };
    }

    /// <summary>
    /// Resolve contexto do usuário logado (actor) e valida permissões
    /// </summary>
    private async Task<Result<User?>> ResolveActorAsync(
        Guid? actorUserId,
        UserRole? actorRole,
        CancellationToken cancellationToken)
    {
        // Sem contexto de autenticação: mantém comportamento legado
        if (!actorRole.HasValue)
            return Result<User?>.Success(null);

        if (actorRole == UserRole.USER)
            return Result<User?>.Failure("Usuários padrão não possuem permissão para esta operação.");

        if (actorRole != UserRole.ADM)
            return Result<User?>.Success(null);

        // ADM precisa ter empresa vinculada para gerenciar equipe
        if (!actorUserId.HasValue)
            return Result<User?>.Failure("Não foi possível identificar o usuário ADM logado.");

        var actor = await _userRepository.GetByIdAsync(actorUserId.Value, cancellationToken);
        if (actor == null || !actor.IsActive)
            return Result<User?>.Failure("Usuário ADM logado inválido ou inativo.");

        // Validar se o actor carregou a Role corretamente
        if (actor.Role == null)
            return Result<User?>.Failure("Erro ao carregar permissões do usuário logado.");

        if (actor.CompanyId == null)
            return Result<User?>.Failure("ADM sem empresa vinculada não pode gerenciar equipe.");

        return Result<User?>.Success(actor);
    }

    /// <summary>
    /// Classe auxiliar para deserialização da resposta da API ViaCEP
    /// </summary>
    private class ViaCepResponse
    {
        public string Cep { get; set; } = string.Empty;
        public string Logradouro { get; set; } = string.Empty;
        public string Complemento { get; set; } = string.Empty;
        public string Unidade { get; set; } = string.Empty;
        public string Bairro { get; set; } = string.Empty;
        public string Localidade { get; set; } = string.Empty;
        public string Uf { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public string Regiao { get; set; } = string.Empty;
        public string Ibge { get; set; } = string.Empty;
        public string Gia { get; set; } = string.Empty;
        public string Ddd { get; set; } = string.Empty;
        public string Siafi { get; set; } = string.Empty;
        public bool Erro { get; set; }
    }
}
