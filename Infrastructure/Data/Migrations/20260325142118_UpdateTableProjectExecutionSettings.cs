using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateTableProjectExecutionSettings : Migration
    {
      
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OQueDeuCerto",
                table: "ProjectExecutionSettings",
                type: "varchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OQueDeuErrado",
                table: "ProjectExecutionSettings",
                type: "varchar(1000)",
                maxLength: 1000,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OQueDeuCerto",
                table: "ProjectExecutionSettings");

            migrationBuilder.DropColumn(
                name: "OQueDeuErrado",
                table: "ProjectExecutionSettings");
        }
    }
}
