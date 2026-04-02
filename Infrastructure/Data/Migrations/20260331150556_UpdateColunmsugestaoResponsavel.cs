using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateColunmsugestaoResponsavel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Remove a coluna antiga SugestaoResponsavel (string)
            migrationBuilder.DropColumn(
                name: "SugestaoResponsavel",
                table: "BoardTask");

            // Adiciona a nova coluna SugestaoResponsavelId (Guid?)
            migrationBuilder.AddColumn<Guid>(
                name: "SugestaoResponsavelId",
                table: "BoardTask",
                type: "uniqueidentifier",
                nullable: true);

            // Cria o índice para a foreign key
            migrationBuilder.CreateIndex(
                name: "IX_BoardTask_SugestaoResponsavelId",
                table: "BoardTask",
                column: "SugestaoResponsavelId");

            // Adiciona a foreign key com Restrict
            migrationBuilder.AddForeignKey(
                name: "FK_BoardTask_Users_SugestaoResponsavelId",
                table: "BoardTask",
                column: "SugestaoResponsavelId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove a foreign key
            migrationBuilder.DropForeignKey(
                name: "FK_BoardTask_Users_SugestaoResponsavelId",
                table: "BoardTask");

            // Remove o índice
            migrationBuilder.DropIndex(
                name: "IX_BoardTask_SugestaoResponsavelId",
                table: "BoardTask");

            // Remove a coluna nova
            migrationBuilder.DropColumn(
                name: "SugestaoResponsavelId",
                table: "BoardTask");

            // Recria a coluna antiga
            migrationBuilder.AddColumn<string>(
                name: "SugestaoResponsavel",
                table: "BoardTask",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);
        }
    }
}
