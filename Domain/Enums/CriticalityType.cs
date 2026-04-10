using System.ComponentModel;

namespace Domain.Enums;

/// <summary>
/// Níveis de criticidade para dependências e integrações do projeto.
/// Valores alinhados com o frontend (projects-create step 3).
/// </summary>
public enum CriticalityType
{
    Bloqueante = 1,

    Importante = 2,

    Desejavel = 3
}
