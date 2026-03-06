using Application.Core.DTOs.Auth;
using Application.Core.DTOs.Users;
using Application.Core.Interfaces.Services;
using Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Application.Controllers;

/// <summary>
/// Controller de Autenticação
/// Gerencia operações de login, logout e autenticação de usuários
/// Senha padrão: Data de nascimento no formato ddMMyyyy (ex: 25111998)
/// Preparado para evolução futura (JWT, refresh token, etc)
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
    /// Renova o token JWT usando refresh token
    /// Permite obter novo access token sem fazer login novamente
    /// </summary>
    /// <param name="request">Token expirado e refresh token</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <response code="200">Token renovado com sucesso</response>
    /// <response code="400">Token inválido ou refresh token inválido</response>
    [HttpPost("refresh-token")]
    [ProducesResponseType(typeof(Result<LoginResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RefreshToken(
        [FromBody] RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _authService.RefreshTokenAsync(request, cancellationToken);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Troca senha do usuário autenticado
    /// Requer autenticação com Bearer token
    /// Usuário deve fornecer senha atual e nova senha
    /// </summary>
    /// <param name="request">Dados para troca de senha (exceto UserId que vem do token)</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <response code="200">Senha alterada com sucesso</response>
    /// <response code="400">Dados inválidos ou senha atual incorreta</response>
    /// <response code="401">Token não fornecido ou inválido</response>
    [HttpPost("change-password")]
    [Authorize]
    [ProducesResponseType(typeof(Result), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordRequest request,
        CancellationToken cancellationToken)
    {

        var result = await _authService.ChangePasswordAsync(request, cancellationToken);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Esqueceu a senha
    /// </summary>
    /// <response code="200">Solicitação de redefinição de senha enviada com sucesso</response>
    /// <response code="400">CPF inválido ou erro ao processar solicitação</response>
    [HttpPost("forgot-password")]
    [ProducesResponseType(typeof(Result), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ForgotPassword(
        [FromBody] ForgotPasswordRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _authService.ForgotPasswordAsync(request, cancellationToken);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Verifica se CPF já está cadastrado
    /// </summary>
    /// <response code="200">Verificação realizada com sucesso</response>
    /// <response code="400">CPF inválido</response>
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

    /// <summary>
    /// Realiza logout do usuário (revoga o token JWT)
    /// Requer autenticação com Bearer token
    /// Token revogado é adicionado na blacklist e não pode mais ser usado
    /// </summary>
    /// <response code="200">Logout realizado com sucesso</response>
    /// <response code="400">Token inválido ou erro ao realizar logout</response>
    /// <response code="401">Token não fornecido ou inválido</response>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(typeof(Result), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        // Extrair token do header Authorization: "Bearer {token}"
        var authHeader = Request.Headers.Authorization.FirstOrDefault();
        
        if (string.IsNullOrWhiteSpace(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return Unauthorized(Result.Failure("Token não fornecido no header Authorization"));
        }

        var token = authHeader["Bearer ".Length..].Trim();

        var result = await _authService.LogoutAsync(token, cancellationToken);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

 
}
