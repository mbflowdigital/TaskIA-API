using Application.Core.DTOs.Auth;
using Application.Core.Interfaces.Services;
using Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace Application.Controllers;

/// <summary>
/// Controller de Autenticação
/// Gerencia operações de login e autenticação de usuários
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

    // TODO: Endpoints futuros
    // POST /api/auth/refresh-token
    // POST /api/auth/logout
    // POST /api/auth/change-password (usuário logado)
    // POST /api/auth/forgot-password
    // POST /api/auth/reset-password
}
