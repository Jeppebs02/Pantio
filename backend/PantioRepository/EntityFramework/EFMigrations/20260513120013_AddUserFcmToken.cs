using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PantioRepository.EntityFramework.EFMigrations
{
    /// <inheritdoc />
    public partial class AddUserFcmToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "fcm_token",
                table: "users",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "fcm_token",
                table: "users");
        }
    }
}
