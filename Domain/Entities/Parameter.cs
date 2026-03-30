using System.ComponentModel.DataAnnotations;

namespace Domain.Entities;

/// <summary>
/// Entidade de parâmetros do sistema (chave-valor)
/// Tabela com apenas 2 colunas: Nome (PK) e Valor
/// </summary>
public class Parameter
{
    [Key]
    [Required]
    [MaxLength(100)]
    public string Nome { get; set; } = string.Empty;

    [Required]
    public string Valor { get; set; } = string.Empty;

    public void UpdateValor(string valor)
    {
        Valor = valor;
    }
}
