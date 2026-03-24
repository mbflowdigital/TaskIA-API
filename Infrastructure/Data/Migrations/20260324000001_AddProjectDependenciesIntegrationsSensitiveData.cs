using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectDependenciesIntegrationsSensitiveData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ====================================================================
            // ALTER TABLE: Adicionar colunas condicionais em ProjectDetails
            // ====================================================================

            migrationBuilder.AddColumn<decimal>(
                name: "ValorOrcamento",
                table: "ProjectDetails",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HorasDowntime",
                table: "ProjectDetails",
                type: "int",
                nullable: true);

            // ====================================================================
            // CREATE TABLE: ProjectDependencies
            // Registros de dependências externas do projeto
            // ====================================================================

            migrationBuilder.CreateTable(
                name: "ProjectDependencies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Descricao = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Prazo = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Criticidade = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectDependencies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectDependencies_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // ====================================================================
            // CREATE TABLE: ProjectIntegrations
            // Registros de integrações externas do projeto
            // ====================================================================

            migrationBuilder.CreateTable(
                name: "ProjectIntegrations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NomeSistema = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Criticidade = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectIntegrations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectIntegrations_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // ====================================================================
            // CREATE TABLE: ProjectSensitiveData
            // Tipos de dados sensíveis tratados pelo projeto
            // ====================================================================

            migrationBuilder.CreateTable(
                name: "ProjectSensitiveData",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TipoDadoSensivel = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectSensitiveData", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectSensitiveData_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // ====================================================================
            // INDEXES
            // ====================================================================

            migrationBuilder.CreateIndex(
                name: "IX_ProjectDependencies_ProjectId",
                table: "ProjectDependencies",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectIntegrations_ProjectId",
                table: "ProjectIntegrations",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectSensitiveData_ProjectId_TipoDadoSensivel",
                table: "ProjectSensitiveData",
                columns: new[] { "ProjectId", "TipoDadoSensivel" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // ====================================================================
            // Remover tabelas novas (ordem inversa das FKs)
            // ====================================================================

            migrationBuilder.DropTable(
                name: "ProjectSensitiveData");

            migrationBuilder.DropTable(
                name: "ProjectIntegrations");

            migrationBuilder.DropTable(
                name: "ProjectDependencies");

            // ====================================================================
            // Remover colunas adicionadas em ProjectDetails
            // ====================================================================

            migrationBuilder.DropColumn(
                name: "HorasDowntime",
                table: "ProjectDetails");

            migrationBuilder.DropColumn(
                name: "ValorOrcamento",
                table: "ProjectDetails");
        }
    }
}
