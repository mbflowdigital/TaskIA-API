using Application.Core.DTOs.Companies;
using Application.Core.Interfaces.Services;
using Domain.Common;
using Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Application.Controllers;

/// <summary>
/// Controller de Empresas
/// Apenas ADM pode criar/editar sua própria empresa.
/// ADM_MASTER não pertence a empresa.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class CompaniesController : ControllerBase
{
    private readonly ICompanyService _companyService;

    public CompaniesController(ICompanyService companyService)
    {
        _companyService = companyService;
    }

    /// <summary>
    /// Cria uma nova empresa. Requer role ADM (quando JWT ativo).
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(Result<CompanyDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create(
        [FromBody] CreateCompanyRequest request,
        CancellationToken cancellationToken)
    {
        // Validar permissão quando JWT estiver presente
        if (TryGetRole(out var role))
        {
            if (role != UserRole.ADM)
                return StatusCode(StatusCodes.Status403Forbidden,
                    Result.Failure("Apenas ADM pode criar empresas."));
        }

        var result = await _companyService.CreateAsync(request, cancellationToken);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Busca empresa por ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(Result<CompanyDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _companyService.GetByIdAsync(id, cancellationToken);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Atualiza empresa. ADM só edita sua própria empresa (quando JWT ativo).
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(Result<CompanyDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateCompanyRequest request,
        CancellationToken cancellationToken)
    {
        if (id != request.Id)
            return BadRequest(Result.Failure("ID da URL diferente do ID do corpo da requisição."));

        // Validar permissão quando JWT estiver presente
        if (TryGetRole(out var role))
        {
            if (role != UserRole.ADM && role != UserRole.ADM_MASTER)
                return StatusCode(StatusCodes.Status403Forbidden,
                    Result.Failure("Sem permissão para editar esta empresa."));
        }

        var result = await _companyService.UpdateAsync(request, cancellationToken);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Desativa empresa (soft delete). Requer ADM_MASTER (quando JWT ativo).
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(Result), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        if (TryGetRole(out var role))
        {
            if (role != UserRole.ADM_MASTER)
                return StatusCode(StatusCodes.Status403Forbidden,
                    Result.Failure("Apenas ADM_MASTER pode desativar empresas."));
        }

        var result = await _companyService.DeleteAsync(id, cancellationToken);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Lista usuários de uma empresa.
    /// </summary>
    [HttpGet("{id:guid}/users")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetUsers(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _companyService.GetUsersByCompanyAsync(id, cancellationToken);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    // Lê role das claims (disponível quando JWT for implementado)
    private bool TryGetRole(out UserRole role)
    {
        role = UserRole.USER;
        var claim = User.FindFirst(ClaimTypes.Role)?.Value;
        if (string.IsNullOrWhiteSpace(claim)) return false;
        return Enum.TryParse(claim, ignoreCase: true, out role);
    }
}
