using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PantioRepository.EntityFramework.EFMigrations
{
    /// <inheritdoc />
    public partial class AddInactiveUserTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "deletion_warning_sent_at",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "last_activity_at",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "deletion_warning_sent_at",
                table: "users");

            migrationBuilder.DropColumn(
                name: "last_activity_at",
                table: "users");
        }
    }
}
