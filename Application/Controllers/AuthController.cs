using Application.Core.DTOs.Auth;
using Application.Core.Interfaces.Services;
using Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
    /// Realiza login do usuário com CPF e senha
    /// Senha padrão: Data de nascimento (ddMMyyyy - ex: 25111998)
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
    /// Verifica se CPF já está cadastrado
    /// </summary>
    [HttpGet("check-cpf")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> CheckCPF(
        [FromQuery] string cpf,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(cpf))
        {
            return BadRequest(new { exists = false, message = "CPF inválido" });
        }

        var exists = await _authService.CPFExistsAsync(cpf, cancellationToken);
        return Ok(new { exists, message = exists ? "CPF já cadastrado" : "CPF disponível" });
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

    // TODO: Endpoints futuros
    // POST /api/auth/refresh-token
    // POST /api/auth/change-password (usuário logado)
    // POST /api/auth/forgot-password
    // POST /api/auth/reset-password
}
