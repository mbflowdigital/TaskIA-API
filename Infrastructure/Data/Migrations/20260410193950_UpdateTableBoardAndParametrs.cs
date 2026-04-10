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
                value: "Crie tarefas executáveis baseadas na análise abaixo.\r\n\r\nMitigue riscos e implemente recomendações fornecidas.\r\n\r\n═══════════════════════════════════════════════════════════════════\r\n📋 PROJETO\r\n═══════════════════════════════════════════════════════════════════\r\n\r\nNome: {ProjectName}\r\nPrazo: {StartDate} a {EndDate} ({TotalDays} dias úteis)\r\nDetalhe: {DetailLevel}\r\n\r\nEquipe:\r\n{TeamMembersSummary}\r\n\r\n═══════════════════════════════════════════════════════════════════\r\n🎯 ANÁLISE ANTERIOR\r\n═══════════════════════════════════════════════════════════════════\r\n\r\nVISÃO GERAL:\r\n{Overview}\r\n\r\nRISCOS CRÍTICOS:\r\n{CriticalRisks}\r\n\r\nRISCOS ALTOS:\r\n{HighRisks}\r\n\r\nRECOMENDAÇÕES PRINCIPAIS:\r\n{Top5Recommendations}\r\n\r\n═══════════════════════════════════════════════════════════════════\r\n📤 RESPOSTA\r\n═══════════════════════════════════════════════════════════════════\r\n\r\nRetorne JSON array (sem ```):\r\n\r\n[\r\n  {\r\n    \"name\": \"string (max 200)\",\r\n    \"description\": \"string (max 1000)\",\r\n    \"priority\": \"Crítica|Alta|Média|Baixa\",\r\n    \"suggestedResponsible\": \"Nome ou papel\",\r\n    \"deadlineInDays\": number,\r\n    \"order\": number,\r\n    \"parentTaskId\": null|\"guid\"\r\n  }\r\n]\r\n\r\n🎯 REGRAS:\r\n\r\nQUANTIDADE: {DetailLevel}\r\n  • Macro: 10-15 tarefas principais (sem subtarefas)\r\n  • Balanceado: 30-40 tarefas (20-25 principais + 10-15 subtarefas)\r\n  • Granular: 70-100 tarefas (40-50 principais + 30-50 subtarefas detalhadas)\r\n\r\nHIERARQUIA:\r\n  • Tarefas principais: parentTaskId = null\r\n  • Subtarefas: parentTaskId = \"guid da tarefa pai\"\r\n  • Use hierarquia para decompor tarefas complexas\r\n  • Exemplo:\r\n    - Tarefa pai: \"Implementar autenticação\" (parentTaskId: null)\r\n    - Subtarefa 1: \"Criar endpoints de login\" (parentTaskId: guid da tarefa pai)\r\n    - Subtarefa 2: \"Implementar JWT\" (parentTaskId: guid da tarefa pai)\r\n  • Máximo 2 níveis de hierarquia (pai → filho, sem netos)\r\n\r\nPRIORIDADE:\r\n  • Crítica: risco crítico ou bloqueante\r\n  • Alta: risco alto ou MVP\r\n  • Média: relevante\r\n  • Baixa: melhoria\r\n\r\nORDEM: 1000, 2000, 3000... (number)\r\n  • Tarefas pai primeiro, depois suas subtarefas\r\n  • Subtarefas devem ter order entre a tarefa pai e a próxima tarefa principal\r\n  • Exemplo:\r\n    - 1000: Tarefa principal A\r\n    - 1100: Subtarefa A.1\r\n    - 1200: Subtarefa A.2\r\n    - 2000: Tarefa principal B\r\n    - 2100: Subtarefa B.1\r\n\r\nPRAZO: Dias desde início ({TotalDays} max)\r\n  • Prazo da tarefa pai deve ser >= prazo da maior subtarefa\r\n  • Subtarefas devem ter prazos <= prazo da tarefa pai\r\n\r\nRESPONSÁVEL: Use só equipe informada\r\n\r\nDESCRIÇÃO: Cite risco OU recomendação que implementa\r\n\r\nPARENTTASKID:\r\n  • null para tarefas principais\r\n  • GUID válido para subtarefas (deve referenciar uma tarefa já criada no JSON)\r\n  • IMPORTANTE: Ao criar subtarefas, use o GUID gerado para a tarefa pai\r\n  • As subtarefas devem aparecer DEPOIS da tarefa pai no array JSON");
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
