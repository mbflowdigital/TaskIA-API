using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateTablePositions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Positions",
                keyColumn: "Id",
                keyValue: 99);

            migrationBuilder.UpdateData(
                table: "Positions",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Description", "PositionName" },
                values: new object[] { "Gerente de projeto", "Gerente de Projeto" });

            migrationBuilder.UpdateData(
                table: "Positions",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "Description", "PositionName" },
                values: new object[] { "Coordenador", "Coordenador" });

            migrationBuilder.UpdateData(
                table: "Positions",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "Description", "PositionName" },
                values: new object[] { "Supervisor", "Supervisor" });

            migrationBuilder.UpdateData(
                table: "Positions",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "Description", "PositionName" },
                values: new object[] { "Engenheiro", "Engenheiro" });

            migrationBuilder.UpdateData(
                table: "Positions",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "Description", "PositionName" },
                values: new object[] { "Técnico", "Técnico" });

            migrationBuilder.UpdateData(
                table: "Positions",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "Description", "PositionName" },
                values: new object[] { "Especialista", "Especialista" });

            migrationBuilder.UpdateData(
                table: "Positions",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "Description", "PositionName" },
                values: new object[] { "Analista", "Analista" });

            migrationBuilder.UpdateData(
                table: "Positions",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "Description", "PositionName" },
                values: new object[] { "Operador", "Operador" });

            migrationBuilder.InsertData(
                table: "Positions",
                columns: new[] { "Id", "Description", "PositionName" },
                values: new object[,]
                {
                    { 9, "Assistente", "Assistente" },
                    { 10, "Administrador", "Administrador" },
                    { 11, "Consultor", "Consultor" },
                    { 12, "Analista de qualidade", "QA" },
                    { 13, "Scrum Master", "Scrum Master" },
                    { 14, "Desenvolvedor", "Desenvolvedor" },
                    { 15, "Product Owner", "Product Owner" },
                    { 16, "DevOps", "DevOps" },
                    { 99, "Outro", "Outro posição" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Positions",
                keyColumn: "Id",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "Positions",
                keyColumn: "Id",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "Positions",
                keyColumn: "Id",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "Positions",
                keyColumn: "Id",
                keyValue: 12);

            migrationBuilder.DeleteData(
                table: "Positions",
                keyColumn: "Id",
                keyValue: 13);

            migrationBuilder.DeleteData(
                table: "Positions",
                keyColumn: "Id",
                keyValue: 14);

            migrationBuilder.DeleteData(
                table: "Positions",
                keyColumn: "Id",
                keyValue: 15);

            migrationBuilder.DeleteData(
                table: "Positions",
                keyColumn: "Id",
                keyValue: 16);

            migrationBuilder.DeleteData(
                table: "Positions",
                keyColumn: "Id",
                keyValue: 17);

            migrationBuilder.UpdateData(
                table: "Positions",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Description", "PositionName" },
                values: new object[] { "Desenvolvedor de software", "Desenvolvedor" });

            migrationBuilder.UpdateData(
                table: "Positions",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "Description", "PositionName" },
                values: new object[] { "Designer UI/UX", "Designer" });

            migrationBuilder.UpdateData(
                table: "Positions",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "Description", "PositionName" },
                values: new object[] { "Gerente de projeto", "Gerente de Projeto" });

            migrationBuilder.UpdateData(
                table: "Positions",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "Description", "PositionName" },
                values: new object[] { "Dono do produto", "Product Owner" });

            migrationBuilder.UpdateData(
                table: "Positions",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "Description", "PositionName" },
                values: new object[] { "Facilitador Scrum", "Scrum Master" });

            migrationBuilder.UpdateData(
                table: "Positions",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "Description", "PositionName" },
                values: new object[] { "Analista de qualidade", "QA" });

            migrationBuilder.UpdateData(
                table: "Positions",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "Description", "PositionName" },
                values: new object[] { "Engenheiro DevOps", "DevOps" });

            migrationBuilder.UpdateData(
                table: "Positions",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "Description", "PositionName" },
                values: new object[] { "Analista de sistemas", "Analista" });

            migrationBuilder.InsertData(
                table: "Positions",
                columns: new[] { "Id", "Description", "PositionName" },
                values: new object[] { 99, "Outra posição", "Outro" });
        }
    }
}
