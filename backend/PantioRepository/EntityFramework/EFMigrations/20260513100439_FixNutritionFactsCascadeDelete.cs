using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PantioRepository.EntityFramework.EFMigrations
{
    /// <inheritdoc />
    public partial class FixNutritionFactsCascadeDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_nutrition_facts_product_cache_product_cache_id",
                table: "nutrition_facts");

            migrationBuilder.AddForeignKey(
                name: "FK_nutrition_facts_product_cache_product_cache_id",
                table: "nutrition_facts",
                column: "product_cache_id",
                principalTable: "product_cache",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.DropForeignKey(
                name: "FK_nutrition_facts_inventory_items_inventory_item_id",
                table: "nutrition_facts");

            migrationBuilder.AddForeignKey(
                name: "FK_nutrition_facts_inventory_items_inventory_item_id",
                table: "nutrition_facts",
                column: "inventory_item_id",
                principalTable: "inventory_items",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_nutrition_facts_inventory_items_inventory_item_id",
                table: "nutrition_facts");

            migrationBuilder.AddForeignKey(
                name: "FK_nutrition_facts_inventory_items_inventory_item_id",
                table: "nutrition_facts",
                column: "inventory_item_id",
                principalTable: "inventory_items",
                principalColumn: "id");

            migrationBuilder.DropForeignKey(
                name: "FK_nutrition_facts_product_cache_product_cache_id",
                table: "nutrition_facts");

            migrationBuilder.AddForeignKey(
                name: "FK_nutrition_facts_product_cache_product_cache_id",
                table: "nutrition_facts",
                column: "product_cache_id",
                principalTable: "product_cache",
                principalColumn: "id");
        }
    }
}
