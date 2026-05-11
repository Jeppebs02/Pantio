using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PantioRepository.EntityFramework.EFMigrations
{
    /// <inheritdoc />
    public partial class FixRecipeEntryInventoryItemCascade : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_recipe_entries_inventory_items_inventory_item_id",
                table: "recipe_entries");

            migrationBuilder.AddForeignKey(
                name: "FK_recipe_entries_inventory_items_inventory_item_id",
                table: "recipe_entries",
                column: "inventory_item_id",
                principalTable: "inventory_items",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_recipe_entries_inventory_items_inventory_item_id",
                table: "recipe_entries");

            migrationBuilder.AddForeignKey(
                name: "FK_recipe_entries_inventory_items_inventory_item_id",
                table: "recipe_entries",
                column: "inventory_item_id",
                principalTable: "inventory_items",
                principalColumn: "id");
        }
    }
}
