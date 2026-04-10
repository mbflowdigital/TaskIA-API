using System.ComponentModel;

namespace Domain.Enums
{
    /// <summary>
    /// Funções/Posições de membros em projetos.
    /// Valores alinhados com o frontend (roleOptions).
    /// </summary>
    public enum UserPosition
    {
        GerenteDeProjeto = 1,
        Coordenador = 2,
        Supervisor = 3,
        Engenheiro = 4,
        Tecnico = 5,
        Especialista = 6,
        Analista = 7,
        Operador = 8,
        Assistente = 9,
        Administrador = 10,
        Consultor = 11
    }
}
