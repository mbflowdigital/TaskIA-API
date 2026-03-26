namespace Domain.Enums;

/// <summary>
/// Tipos de compliance que podem ser aplicados ao projeto
/// </summary>
public enum ComplianceType
{
    DadosPublicos = 0,
    LGPD = 1,
    PCI_DSS = 2,
    HIPAA = 3,
    ISO27001 = 4,
    SOX = 5
}
