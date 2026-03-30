using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

/// <summary>
/// Repositório de parâmetros do sistema
/// Tabela chave-valor simples: Nome (PK) e Valor
/// </summary>
public class ParameterRepository : IParameterRepository
{
    private readonly ApplicationDbContext _context;

    public ParameterRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Parameter?> GetByNomeAsync(string nome, CancellationToken cancellationToken = default)
    {
        return await _context.Parameters
            .FirstOrDefaultAsync(p => p.Nome == nome, cancellationToken);
    }

    public async Task UpdateAsync(Parameter parameter, CancellationToken cancellationToken = default)
    {
        _context.Parameters.Update(parameter);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
