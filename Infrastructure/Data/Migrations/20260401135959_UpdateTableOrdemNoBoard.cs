using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateTableOrdemNoBoard : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Primeiro, remover o índice que depende da coluna OrdemNoBoard
            migrationBuilder.DropIndex(
                name: "IX_ProjectTasks_ProjectId_OrdemNoBoard",
                table: "BoardTask");

            // Alterar o tipo da coluna OrdemNoBoard
            migrationBuilder.AlterColumn<decimal>(
                name: "OrdemNoBoard",
                table: "BoardTask",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 1000m,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            // Recriar o índice com o novo tipo de coluna
            migrationBuilder.CreateIndex(
                name: "IX_BoardTask_ProjectId_OrdemNoBoard",
                table: "BoardTask",
                columns: new[] { "ProjectId", "OrdemNoBoard" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remover o índice recriado
            migrationBuilder.DropIndex(
                name: "IX_BoardTask_ProjectId_OrdemNoBoard",
                table: "BoardTask");

            // Reverter a coluna para string
            migrationBuilder.AlterColumn<string>(
                name: "OrdemNoBoard",
                table: "BoardTask",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            // Recriar o índice original
            migrationBuilder.CreateIndex(
                name: "IX_ProjectTasks_ProjectId_OrdemNoBoard",
                table: "BoardTask",
                columns: new[] { "ProjectId", "OrdemNoBoard" });
        }
    }
}
