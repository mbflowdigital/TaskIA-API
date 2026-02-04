using Domain.Common;

namespace Application.Core.Interfaces;

/// <summary>
/// Interface base para casos de uso (Use Cases)
/// Implementa Command pattern e Single Responsibility Principle
/// </summary>
/// <typeparam name="TRequest">Tipo do objeto de requisição</typeparam>
/// <typeparam name="TResponse">Tipo do objeto de resposta</typeparam>
public interface IUseCase<in TRequest, TResponse>
{
    Task<Result<TResponse>> ExecuteAsync(TRequest request, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface para casos de uso sem retorno de dados
/// </summary>
public interface IUseCase<in TRequest>
{
    Task<Result> ExecuteAsync(TRequest request, CancellationToken cancellationToken = default);
}
