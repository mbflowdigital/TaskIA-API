using Application.Core.DTOs.Auth;
using Application.Core.DTOs.Users;
using Application.Core.Interfaces.Services;
using Domain.Common;
using Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Application.Controllers;

/// <summary>
/// Controller de Autentica��o
/// Gerencia opera��es de login e autentica��o de usu�rios
/// Senha padr�o: Data de nascimento no formato ddMMyyyy (ex: 25111998)
/// Preparado para evolu��o futura (JWT, refresh token, etc)
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// Realiza login do usu�rio com CPF e senha
    /// Senha padr�o: Data de nascimento (ddMMyyyy - ex: 25111998)
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(Result<LoginResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _authService.LoginAsync(request, cancellationToken);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Troca senha de primeiro acesso
    /// </summary>
    [HttpPost("change-password-first-access")]
    [ProducesResponseType(typeof(Result<LoginResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ChangePasswordFirstAccess(
        [FromBody] ChangePasswordFirstAccessRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _authService.ChangePasswordFirstAccessAsync(request, cancellationToken);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Verifica se CPF j� est� cadastrado
    /// </summary>
    [HttpGet("check-cpf")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> CheckCPF(
        [FromQuery] string cpf,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(cpf))
        {
            return BadRequest(new { exists = false, message = "CPF inv�lido" });
        }

        var exists = await _authService.CPFExistsAsync(cpf, cancellationToken);
        return Ok(new { exists, message = exists ? "CPF j� cadastrado" : "CPF dispon�vel" });
    }

    /// <summary>
    /// Registra novo usuário (cria conta com senha padrão)
    /// Se o JWT estiver presente, valida permissões do criador.
    /// ADM_MASTER → pode criar ADM e USER | ADM → pode criar USER | USER → proibido
    /// </summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(Result<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken)
    {
        // Ler role das Claims (disponível quando JWT estiver implementado)
        UserRole? createdByRole = null;
        var roleClaim = User.FindFirst(ClaimTypes.Role)?.Value;
        if (!string.IsNullOrWhiteSpace(roleClaim) &&
            Enum.TryParse<UserRole>(roleClaim, ignoreCase: true, out var claimRole))
        {
            createdByRole = claimRole;
            if (claimRole == UserRole.USER)
                return StatusCode(StatusCodes.Status403Forbidden,
                    Result.Failure("Usuários padrão não podem criar outros usuários."));
        }

        var result = await _authService.RegisterAsync(request, createdByRole, cancellationToken);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Conclui o onboarding do ADM: cria a empresa e vincula ao usuário.
    /// Deve ser chamado após o primeiro login de um ADM sem empresa.
    /// </summary>
    [HttpPost("onboarding")]
    [ProducesResponseType(typeof(Result<LoginResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Onboarding(
        [FromBody] OnboardingRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _authService.OnboardingAsync(request, cancellationToken);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    // TODO: Endpoints futuros
    // POST /api/auth/refresh-token
    // POST /api/auth/logout
    // POST /api/auth/change-password (usu�rio logado)
    // POST /api/auth/forgot-password
    // POST /api/auth/reset-password
}
