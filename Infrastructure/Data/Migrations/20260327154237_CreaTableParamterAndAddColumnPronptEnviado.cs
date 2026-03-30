using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class CreaTableParamterAndAddColumnPronptEnviado : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Prompt_enviado",
                table: "Projects",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Parameters",
                columns: table => new
                {
                    Nome = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Valor = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Parameters", x => x.Nome);
                });

            migrationBuilder.InsertData(
                table: "Parameters",
                columns: new[] { "Nome", "Valor" },
                values: new object[] { "Prompt_Base", "Você é um consultor sênior especialista em gerenciamento de projetos, análise de riscos e planejamento estratégico. \r\nSua missão é analisar profundamente o projeto descrito abaixo e fornecer insights valiosos, identificando:\r\n- Viabilidade real considerando recursos, prazos e complexidade\r\n- Riscos técnicos, humanos, de negócio e externos\r\n- Recomendações práticas e acionáveis para maximizar o sucesso do projeto\r\n\r\nConsidere o perfil da equipe, suas experiências passadas, restrições operacionais e o contexto completo do projeto.\r\nSeja específico, objetivo e estratégico nas suas análises.\r\n\r\n═══════════════════════════════════════════════════════════════════\r\n📋 INFORMAÇÕES GERAIS DO PROJETO\r\n═══════════════════════════════════════════════════════════════════\r\n\r\nNome do Projeto: {ProjectName}\r\nObjetivo: {Objective}\r\nDescrição Detalhada: {Description}\r\n\r\n📅 Cronograma:\r\n  • Data de Início: {StartDate}\r\n  • Data de Término: {EndDate}\r\n\r\n🏢 Contexto Organizacional:\r\n  • Empresa: {Company}\r\n  • Departamento: {Department}\r\n  • Tipo de Projeto: {ProjectType}\r\n\r\n═══════════════════════════════════════════════════════════════════\r\n👥 COMPOSIÇÃO DA EQUIPE E RESPONSABILIDADES\r\n═══════════════════════════════════════════════════════════════════\r\n\r\n{TeamMembers}\r\n\r\n💡 ANÁLISE REQUERIDA: Avalie se a equipe possui:\r\n  - Seniority adequada para o escopo\r\n  - Dedicação suficiente considerando o prazo\r\n  - Papéis bem distribuídos (evitando sobrecarga)\r\n  - Aprovadores estrategicamente posicionados\r\n\r\n═══════════════════════════════════════════════════════════════════\r\n⚙️ CONTEXTO OPERACIONAL E RESTRIÇÕES\r\n═══════════════════════════════════════════════════════════════════\r\n\r\n💰 Orçamento:\r\n  {Budget}\r\n\r\n⏰ Regime de Trabalho:\r\n  {WorkSchedule}\r\n\r\n🚨 Política de Downtime:\r\n  {DowntimePolicy}\r\n\r\n🔗 Dependências Externas:\r\n{ExternalDependencies}\r\n\r\n🔌 Integrações Necessárias:\r\n{Integrations}\r\n\r\n📜 Conformidade e Regulamentações:\r\n  • Requisitos: {Compliance}\r\n  • Aprovadores de Compliance: {ComplianceApprovers}\r\n\r\n🚫 Períodos de Indisponibilidade da Equipe:\r\n{UnavailablePeriods}\r\n\r\n💡 ANÁLISE REQUERIDA: Identifique:\r\n  - Dependências críticas que podem bloquear o projeto\r\n  - Conflitos entre prazos de dependências e cronograma\r\n  - Riscos de disponibilidade da equipe em fases críticas\r\n  - Impacto das políticas de downtime na estratégia de deploy\r\n\r\n═══════════════════════════════════════════════════════════════════\r\n🎯 PRIORIDADES E CONTEXTO ESTRATÉGICO\r\n═══════════════════════════════════════════════════════════════════\r\n\r\nRanking de Prioridades: {PriorityRanking}\r\n\r\n⚠️ Maior Risco Percebido pela Equipe:\r\n{BiggestRisk}\r\n\r\n📚 Experiência Prévia da Equipe:\r\n  • Nível de experiência: {PreviousExperience}\r\n  • O que funcionou bem em projetos anteriores: {WhatWentWell}\r\n  • O que não funcionou em projetos anteriores: {WhatWentWrong}\r\n\r\n📊 Expectativas de Gestão:\r\n  • Nível de Detalhe no Planejamento: {DetailLevel}\r\n  • Frequência de Revisão: {ReviewFrequency}\r\n\r\n📝 Observações Finais do Solicitante:\r\n{FinalObservations}\r\n\r\n💡 ANÁLISE REQUERIDA:\r\n  - Compare as prioridades declaradas com os riscos identificados\r\n  - Identifique se a experiência prévia da equipe é compatível com os desafios\r\n  - Sugira ajustes no nível de detalhe ou frequência de revisão se necessário\r\n  - Considere as lições aprendidas para evitar erros recorrentes\r\n\r\n═══════════════════════════════════════════════════════════════════\r\n📤 FORMATO DE RESPOSTA OBRIGATÓRIO\r\n═══════════════════════════════════════════════════════════════════\r\n\r\nResponda SOMENTE no formato JSON abaixo (sem markdown, sem explicações adicionais):\r\n\r\n{\r\n  \"overview\": \"Análise geral do projeto incluindo: viabilidade técnica e de negócio, pontos fortes da proposta, principais desafios identificados, adequação da equipe ao escopo, e uma avaliação crítica do cronograma proposto. Seja objetivo e direto (3-4 frases).\",\r\n  \"risks\": \"Riscos no formato: CRITICO: <lista separada por vírgula> | ALTO: <lista separada por vírgula> | MEDIO: <lista separada por vírgula> | BAIXO: <lista separada por vírgula>. Classifique cada risco (técnico, de equipe, de negócio, de cronograma) no nível adequado. Use cada nível apenas se houver riscos reais nele; omita os que não se aplicam. Seja específico e cite exemplos do contexto fornecido.\",\r\n  \"recommendations\": \"Forneça recomendações práticas e acionáveis separadas por ponto e vírgula. Inclua sugestões para: mitigação de riscos identificados, otimização da alocação da equipe, estratégias de gestão de dependências, melhorias no processo de aprovação, pontos de atenção no cronograma, e boas práticas baseadas nas experiências anteriores relatadas. Seja estratégico e prático.\"\r\n}" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Parameters");

            migrationBuilder.DropColumn(
                name: "Prompt_enviado",
                table: "Projects");
        }
    }
}
