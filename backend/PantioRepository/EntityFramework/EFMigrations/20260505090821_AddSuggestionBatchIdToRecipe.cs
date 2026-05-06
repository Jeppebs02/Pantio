using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PantioRepository.EntityFramework.EFMigrations
{
    /// <inheritdoc />
    public partial class AddSuggestionBatchIdToRecipe : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "suggestion_batch_id",
                table: "recipes",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "suggestion_batch_id",
                table: "recipes");
        }
    }
}
