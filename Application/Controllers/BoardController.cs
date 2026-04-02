using Application.Core.DTOs.Board.Requests;
using Application.Core.DTOs.Board.Responses;
using Application.Core.Interfaces.Services;
using Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Application.Controllers;

/// <summary>
/// Controller para gerenciamento de Board (Tarefas)
/// </summary>
[ApiController]
[Authorize]
[Route("api/[controller]")]
[Produces("application/json")]
public class BoardController : ControllerBase
{
    private readonly IBoardService _boardService;

    public BoardController(IBoardService boardService)
    {
        _boardService = boardService;
    }

    /// <summary>
    /// Cria uma nova tarefa
    /// </summary>
    /// <param name="request">Dados da tarefa</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    [HttpPost]
    [ProducesResponseType(typeof(Result<BoardDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        [FromBody] CreateBoardRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _boardService.CreateAsync(request, cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(result);

        return CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result);
    }

    /// <summary>
    /// Atualiza informações de uma tarefa
    /// </summary>
    /// <param name="id">ID da tarefa</param>
    /// <param name="request">Dados atualizados</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(Result<BoardDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        [FromRoute] Guid id,
        [FromBody] UpdateBoardRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _boardService.UpdateAsync(id, request, cancellationToken);

        if (!result.IsSuccess)
            return result.Message.Contains("não encontrada") ? NotFound(result) : BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// Atribui ou altera o responsável de uma tarefa
    /// </summary>
    /// <param name="id">ID da tarefa</param>
    /// <param name="request">ID do responsável</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    [HttpPut("{id:guid}/assign-responsavel")]
    [ProducesResponseType(typeof(Result<BoardDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AssignResponsavel(
        [FromRoute] Guid id,
        [FromBody] AssignResponsavelRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _boardService.AssignResponsavelAsync(id, request, cancellationToken);

        if (!result.IsSuccess)
            return result.Message.Contains("não encontrad") ? NotFound(result) : BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// Atribui ou altera a sugestão de responsável de uma tarefa
    /// </summary>
    /// <param name="id">ID da tarefa</param>
    /// <param name="request">ID do usuário sugerido</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    [HttpPut("{id:guid}/assign-sugestao")]
    [ProducesResponseType(typeof(Result<BoardDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AssignSugestaoResponsavel(
        [FromRoute] Guid id,
        [FromBody] AssignSugestaoResponsavelRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _boardService.AssignSugestaoResponsavelAsync(id, request, cancellationToken);

        if (!result.IsSuccess)
            return result.Message.Contains("não encontrad") ? NotFound(result) : BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// Altera o status de uma tarefa
    /// </summary>
    /// <param name="id">ID da tarefa</param>
    /// <param name="request">Novo status</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    [HttpPut("{id:guid}/status")]
    [ProducesResponseType(typeof(Result<BoardDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStatus(
        [FromRoute] Guid id,
        [FromBody] UpdateBoardStatusRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _boardService.UpdateStatusAsync(id, request, cancellationToken);

        if (!result.IsSuccess)
            return result.Message.Contains("não encontrada") ? NotFound(result) : BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// Altera a ordem da tarefa no board
    /// </summary>
    /// <param name="id">ID da tarefa</param>
    /// <param name="request">Nova ordem</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    [HttpPut("{id:guid}/ordem")]
    [ProducesResponseType(typeof(Result<BoardDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateOrdem(
        [FromRoute] Guid id,
        [FromBody] UpdateBoardOrdemRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _boardService.UpdateOrdemAsync(id, request, cancellationToken);

        if (!result.IsSuccess)
            return NotFound(result);

        return Ok(result);
    }

    /// <summary>
    /// Altera o prazo estimado de uma tarefa
    /// </summary>
    /// <param name="id">ID da tarefa</param>
    /// <param name="request">Novo prazo em dias</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    [HttpPut("{id:guid}/prazo")]
    [ProducesResponseType(typeof(Result<BoardDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdatePrazo(
        [FromRoute] Guid id,
        [FromBody] UpdateBoardPrazoRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _boardService.UpdatePrazoAsync(id, request, cancellationToken);

        if (!result.IsSuccess)
            return result.Message.Contains("não encontrada") ? NotFound(result) : BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// Obtém todas as tarefas de um projeto
    /// </summary>
    /// <param name="projectId">ID do projeto</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    [HttpGet("project/{projectId:guid}")]
    [ProducesResponseType(typeof(Result<IEnumerable<BoardDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByProject(
        [FromRoute] Guid projectId,
        CancellationToken cancellationToken)
    {
        var result = await _boardService.GetByProjectIdAsync(projectId, cancellationToken);

        if (!result.IsSuccess)
            return NotFound(result);

        return Ok(result);
    }

    /// <summary>
    /// Obtém tarefas de um projeto por status (fase)
    /// </summary>
    /// <param name="projectId">ID do projeto</param>
    /// <param name="status">Status da tarefa (A Fazer, Em Andamento, Concluído)</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    [HttpGet("project/{projectId:guid}/status/{status}")]
    [ProducesResponseType(typeof(Result<IEnumerable<BoardDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByProjectAndStatus(
        [FromRoute] Guid projectId,
        [FromRoute] string status,
        CancellationToken cancellationToken)
    {
        var result = await _boardService.GetByProjectIdAndStatusAsync(projectId, status, cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// Obtém tarefas de um projeto por prioridade
    /// </summary>
    /// <param name="projectId">ID do projeto</param>
    /// <param name="priority">Prioridade (Baixa, Média, Alta, Crítica)</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    [HttpGet("project/{projectId:guid}/priority/{priority}")]
    [ProducesResponseType(typeof(Result<IEnumerable<BoardDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByProjectAndPriority(
        [FromRoute] Guid projectId,
        [FromRoute] string priority,
        CancellationToken cancellationToken)
    {
        var result = await _boardService.GetByProjectIdAndPriorityAsync(projectId, priority, cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// Obtém uma tarefa por ID
    /// </summary>
    /// <param name="id">ID da tarefa</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(Result<BoardDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _boardService.GetByIdAsync(id, cancellationToken);

        if (!result.IsSuccess)
            return NotFound(result);

        return Ok(result);
    }


    /// <summary>
    /// Obtém estatísticas de tarefas de um projeto
    /// </summary>
    /// <param name="projectId">ID do projeto</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    [HttpGet("project/{projectId:guid}/statistics")]
    [ProducesResponseType(typeof(Result<BoardStatisticsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStatistics(
        [FromRoute] Guid projectId,
        CancellationToken cancellationToken)
    {
        var result = await _boardService.GetStatisticsByProjectIdAsync(projectId, cancellationToken);

        if (!result.IsSuccess)
            return NotFound(result);

        return Ok(result);
    }


    /// <summary>
    /// Remove uma tarefa (soft delete)
    /// </summary>
    /// <param name="id">ID da tarefa</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(Result), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _boardService.DeleteAsync(id, cancellationToken);

        if (!result.IsSuccess)
            return NotFound(result);

        return Ok(result);
    }

    
}
