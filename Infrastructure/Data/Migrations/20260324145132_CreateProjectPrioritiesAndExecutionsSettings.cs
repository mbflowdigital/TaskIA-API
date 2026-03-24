using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class CreateProjectPrioritiesAndExecutionsSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProjectExecutionSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MaiorRisco = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ExperienciaEquipe = table.Column<int>(type: "int", nullable: false),
                    NivelDetalhePlano = table.Column<int>(type: "int", nullable: false),
                    FrequenciaRevisao = table.Column<int>(type: "int", nullable: false),
                    Observacoes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectExecutionSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectExecutionSettings_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProjectPriorityRankings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PriorityType = table.Column<int>(type: "int", nullable: false),
                    Posicao = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectPriorityRankings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectPriorityRankings_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectExecutionSettings_ProjectId",
                table: "ProjectExecutionSettings",
                column: "ProjectId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectPriorityRankings_ProjectId_Posicao",
                table: "ProjectPriorityRankings",
                columns: new[] { "ProjectId", "Posicao" });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectPriorityRankings_ProjectId_PriorityType",
                table: "ProjectPriorityRankings",
                columns: new[] { "ProjectId", "PriorityType" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProjectExecutionSettings");

            migrationBuilder.DropTable(
                name: "ProjectPriorityRankings");
        }
    }
}
