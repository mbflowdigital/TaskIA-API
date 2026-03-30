using Domain.Entities;

namespace Domain.Interfaces;

/// <summary>
/// Interface do repositório de parâmetros do sistema
/// Tabela chave-valor simples: Nome (PK) e Valor
/// </summary>
public interface IParameterRepository
{
    Task<Parameter?> GetByNomeAsync(string nome, CancellationToken cancellationToken = default);
    Task UpdateAsync(Parameter parameter, CancellationToken cancellationToken = default);
}
