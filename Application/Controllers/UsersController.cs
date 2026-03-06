using Application.Core.DTOs.Users;
using Application.Core.Interfaces.Services;
using Domain.Common;
using Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Application.Controllers;

/// <summary>
/// Controller de Usuários
/// Depende de IUserService (abstração), não de UserService (implementação)
/// Seguindo Dependency Inversion Principle (SOLID)
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    /// <summary>
    /// Cria um novo usuário
    /// </summary>
    /// <param name="request">Dados do usuário</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Usuário criado</returns>
    /// <response code="200">Usuário criado com sucesso</response>
    /// <response code="400">Dados inválidos</response>
    [HttpPost]
    [ProducesResponseType(typeof(Result<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        [FromBody] CreateUserRequest request,
        CancellationToken cancellationToken)
    {
        var (actorUserId, actorRole) = GetActorContext();

        var adminCheck = EnsureAdminRole(actorRole);
        if (adminCheck != null) return adminCheck;

        // Verificar permissão via Claims (ativo quando JWT estiver implementado)
        var roleClaim = actorRole?.ToString();
        if (!string.IsNullOrWhiteSpace(roleClaim) &&
            Enum.TryParse<UserRole>(roleClaim, ignoreCase: true, out var claimRole) &&
            claimRole == UserRole.USER)
        {
            return StatusCode(StatusCodes.Status403Forbidden,
                Result.Failure("Usuários padrão não podem criar outros usuários."));
        }

        var result = await _userService.CreateAsync(request, actorUserId, actorRole, cancellationToken);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Lista todos os usuários
    /// </summary>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Lista de usuários</returns>
    /// <response code="200">Lista retornada com sucesso</response>
    [HttpGet]
    [ProducesResponseType(typeof(Result<IEnumerable<UserDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var (actorUserId, actorRole) = GetActorContext();
        var adminCheck = EnsureAdminRole(actorRole);
        if (adminCheck != null) return adminCheck;

        var result = await _userService.GetAllAsync(actorUserId, actorRole, cancellationToken);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Busca usuário por ID
    /// </summary>
    /// <param name="id">ID do usuário</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Usuário encontrado</returns>
    /// <response code="200">Usuário encontrado</response>
    /// <response code="400">Usuário não encontrado</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(Result<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var (actorUserId, actorRole) = GetActorContext();
        var adminCheck = EnsureAdminRole(actorRole);
        if (adminCheck != null) return adminCheck;

        var result = await _userService.GetByIdAsync(id, actorUserId, actorRole, cancellationToken);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Atualiza um usuário
    /// </summary>
    /// <param name="id">ID do usuário</param>
    /// <param name="request">Dados para atualização</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Usuário atualizado</returns>
    /// <response code="200">Usuário atualizado com sucesso</response>
    /// <response code="400">Dados inválidos ou usuário não encontrado</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(Result<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateUserRequest request,
        CancellationToken cancellationToken)
    {
        if (id != request.Id)
        {
            return BadRequest(Result.Failure("ID da URL diferente do ID do corpo da requisição"));
        }

        var (actorUserId, actorRole) = GetActorContext();
        var adminCheck = EnsureAdminRole(actorRole);
        if (adminCheck != null) return adminCheck;

        var result = await _userService.UpdateAsync(request, actorUserId, actorRole, cancellationToken);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Desativa um usuário (soft delete)
    /// </summary>
    /// <param name="id">ID do usuário</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Resultado da operação</returns>
    /// <response code="200">Usuário desativado com sucesso</response>
    /// <response code="400">Usuário não encontrado</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(Result), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        var (actorUserId, actorRole) = GetActorContext();
        var adminCheck = EnsureAdminRole(actorRole);
        if (adminCheck != null) return adminCheck;

        var result = await _userService.DeleteAsync(id, actorUserId, actorRole, cancellationToken);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Busca usuários por email
    /// </summary>
    /// <param name="email">Email ou parte do email</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Lista de usuários encontrados</returns>
    [HttpGet("search")]
    [ProducesResponseType(typeof(Result<IEnumerable<UserDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchByEmail(
        [FromQuery] string email,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return BadRequest(Result.Failure("Email é obrigatório"));
        }

        var result = await _userService.FindByEmailAsync(email, cancellationToken);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    private (Guid? ActorUserId, UserRole? ActorRole) GetActorContext()
    {
        var userIdRaw =
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
            User.FindFirst("sub")?.Value ??
            Request.Headers["X-User-Id"].FirstOrDefault();

        var roleRaw =
            User.FindFirst(ClaimTypes.Role)?.Value ??
            Request.Headers["X-User-Role"].FirstOrDefault();

        Guid? actorUserId = null;
        if (!string.IsNullOrWhiteSpace(userIdRaw) && Guid.TryParse(userIdRaw, out var parsedId))
            actorUserId = parsedId;

        UserRole? actorRole = null;
        if (!string.IsNullOrWhiteSpace(roleRaw) && Enum.TryParse<UserRole>(roleRaw, true, out var parsedRole))
            actorRole = parsedRole;

        return (actorUserId, actorRole);
    }

    private IActionResult? EnsureAdminRole(UserRole? actorRole)
    {
        if (!actorRole.HasValue)
        {
            return StatusCode(StatusCodes.Status403Forbidden,
                Result.Failure("Perfil do usuário não identificado para esta operação."));
        }

        var isAdmin = actorRole == UserRole.ADM || actorRole == UserRole.ADM_MASTER;
        if (!isAdmin)
        {
            return StatusCode(StatusCodes.Status403Forbidden,
                Result.Failure("Apenas administradores podem acessar este recurso."));
        }

        return null;
    }

    /// <summary>
    /// Verifica se email já está cadastrado
    /// </summary>
    /// <param name="email">Email para verificar</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>True se email existe, False se não</returns>
    [HttpGet("check-email")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> CheckEmail(
        [FromQuery] string email,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return BadRequest(new { exists = false, message = "Email inválido" });
        }

        var exists = await _userService.EmailExistsAsync(email, cancellationToken);
        return Ok(new { exists, message = exists ? "Email já cadastrado" : "Email disponível" });
    }
}
