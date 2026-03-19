using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUserRoleToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Role",
                table: "Users",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "USER");

            // Bootstrap: promove o primeiro usuário existente para ADM_MASTER
            // (para bases já populadas antes da introdução de perfis)
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM [Users])
BEGIN
    UPDATE [Users]
    SET [Role] = 'ADM_MASTER'
    WHERE [Id] = (
        SELECT TOP(1) [Id]
        FROM [Users]
        ORDER BY [CreatedAt] ASC
    );
END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Role",
                table: "Users");
        }
    }
}
