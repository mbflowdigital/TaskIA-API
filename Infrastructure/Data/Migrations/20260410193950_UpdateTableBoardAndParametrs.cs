using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateTableBoardAndParametrs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Verificar e adicionar coluna ParentTaskId se não existir
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.columns 
                               WHERE object_id = OBJECT_ID(N'[dbo].[BoardTask]') 
                               AND name = 'ParentTaskId')
                BEGIN
                    ALTER TABLE [BoardTask] ADD [ParentTaskId] uniqueidentifier NULL;
                END
            ");

            // Criar índice se não existir
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.indexes 
                               WHERE object_id = OBJECT_ID(N'[dbo].[BoardTask]') 
                               AND name = 'IX_BoardTask_ParentTaskId')
                BEGIN
                    CREATE INDEX [IX_BoardTask_ParentTaskId] ON [BoardTask] ([ParentTaskId]);
                END
            ");

            // Adicionar foreign key se não existir
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.foreign_keys 
                               WHERE object_id = OBJECT_ID(N'[dbo].[FK_BoardTask_BoardTask_ParentTaskId]') 
                               AND parent_object_id = OBJECT_ID(N'[dbo].[BoardTask]'))
                BEGIN
                    ALTER TABLE [BoardTask] 
                    ADD CONSTRAINT [FK_BoardTask_BoardTask_ParentTaskId] 
                    FOREIGN KEY ([ParentTaskId]) 
                    REFERENCES [BoardTask] ([Id]) 
                    ON DELETE NO ACTION;
                END
            ");

            // Atualizar Prompt_Task com instruções de hierarquia
            migrationBuilder.UpdateData(
                table: "Parameters",
                keyColumn: "Nome",
                keyValue: "Prompt_Task",
                column: "Valor",
                value: "Crie 5-10 MACROS ESTRATÉGICAS baseadas na análise abaixo. Foque em ENTREGAS CONCRETAS que geram VALOR REAL.\r\n\r\n═══════════════════════════════════════════════════════════════════\r\n📋 PROJETO\r\n═══════════════════════════════════════════════════════════════════\r\n\r\nNome: {ProjectName}\r\nPrazo: {StartDate} a {EndDate} ({TotalDays} dias úteis)\r\n\r\nEquipe:\r\n{TeamMembersSummary}\r\n\r\n═══════════════════════════════════════════════════════════════════\r\n🎯 ANÁLISE ESTRATÉGICA\r\n═══════════════════════════════════════════════════════════════════\r\n\r\nVISÃO GERAL:\r\n{Overview}\r\n\r\nRISCOS CRÍTICOS:\r\n{CriticalRisks}\r\n\r\nRISCOS ALTOS:\r\n{HighRisks}\r\n\r\nRECOMENDAÇÕES PRINCIPAIS:\r\n{Top5Recommendations}\r\n\r\n═══════════════════════════════════════════════════════════════════\r\n🎯 MACROS ESTRATÉGICAS\r\n═══════════════════════════════════════════════════════════════════\r\n\r\nGere 5-10 MACROS que representem ENTREGAS CONCRETAS, não análises teóricas:\r\n\r\n✅ BOAS MACROS (exemplos):\r\n• \"Implementar sistema de autenticação OAuth2 com recuperação de senha\"\r\n• \"Criar dashboard executivo com métricas de performance\"\r\n• \"Desenvolver API RESTful para integração com sistemas externos\"\r\n• \"Configurar infraestrutura de CI/CD com testes automatizados\"\r\n• \"Implementar sistema de notificações em tempo real\"\r\n\r\n❌ RUINS MACROS (evite):\r\n• \"Analisar riscos do projeto\" (é análise, não entrega)\r\n• \"Implementar recomendações da IA\" (genérico demais)\r\n• \"Mitigar riscos críticos\" (ação sem resultado concreto)\r\n• \"Seguir melhores práticas\" (muito vago)\r\n\r\n═══════════════════════════════════════════════════════════════════\r\n📤 RESPOSTA JSON\r\n═══════════════════════════════════════════════════════════════════\r\n\r\nRetorne array JSON (sem ```):\r\n\r\n[\r\n  {\r\n    \"name\": \"string (max 200) - Entrega concreta com resultado mensurável\",\r\n    \"description\": \"string (max 1000) - Como implementar + valor que gera\",\r\n    \"priority\": \"Crítica|Alta|Média|Baixa\",\r\n    \"suggestedResponsible\": \"Nome da pessoa da equipe\",\r\n    \"deadlineInDays\": number,\r\n    \"order\": number\r\n  }\r\n]\r\n\r\n🎯 REGRAS ESTRATÉGICAS:\r\n\r\nQUANTIDADE: Sempre 5-10 macros estratégicas (independente do DetailLevel)\r\n\r\nFOCO EM VALOR:\r\n  • Cada macro deve gerar um resultado concreto e mensurável\r\n  • Priorize entregas que mitigam riscos críticos\r\n  • Implemente as recomendações mais importantes\r\n  • Considere dependências técnicas e de negócio\r\n\r\nPRIORIDADE:\r\n  • Crítica: Bloqueia o projeto ou mitiga risco crítico\r\n  • Alta: Essencial para MVP ou mitiga risco alto\r\n  • Média: Importante mas não bloqueante\r\n  • Baixa: Melhoria ou otimização\r\n\r\nPRAZO: Dias realistas baseado na complexidade\r\n  • Macros críticas: 3-7 dias\r\n  • Macros altas: 5-14 dias\r\n  • Macros médias: 7-21 dias\r\n  • Macros baixas: 10-30 dias\r\n\r\nRESPONSÁVEL: Atribua baseado nas habilidades da equipe\r\n\r\nORDEM: 1000, 2000, 3000... (ordem de execução ideal)\r\n\r\nDESCRIÇÃO: Explique COMO fazer + QUAL valor gera");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remover a foreign key se existir
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.foreign_keys 
                           WHERE object_id = OBJECT_ID(N'[dbo].[FK_BoardTask_BoardTask_ParentTaskId]') 
                           AND parent_object_id = OBJECT_ID(N'[dbo].[BoardTask]'))
                BEGIN
                    ALTER TABLE [BoardTask] DROP CONSTRAINT [FK_BoardTask_BoardTask_ParentTaskId];
                END
            ");

            // Remover o índice se existir
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.indexes 
                           WHERE object_id = OBJECT_ID(N'[dbo].[BoardTask]') 
                           AND name = 'IX_BoardTask_ParentTaskId')
                BEGIN
                    DROP INDEX [IX_BoardTask_ParentTaskId] ON [BoardTask];
                END
            ");

            // Remover a coluna se existir
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.columns 
                           WHERE object_id = OBJECT_ID(N'[dbo].[BoardTask]') 
                           AND name = 'ParentTaskId')
                BEGIN
                    ALTER TABLE [BoardTask] DROP COLUMN [ParentTaskId];
                END
            ");

            // Nota: Não revertemos o Prompt_Task pois é apenas atualização de texto
        }
    }
}
