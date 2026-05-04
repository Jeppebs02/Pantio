using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PantioRepository.EntityFramework.EFMigrations
{
    /// <inheritdoc />
    public partial class InventoryItemProductData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_nutrition_facts_product_cache_product_cache_id",
                table: "nutrition_facts");

            migrationBuilder.AlterColumn<Guid>(
                name: "product_cache_id",
                table: "nutrition_facts",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "inventory_item_id",
                table: "nutrition_facts",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "category_id",
                table: "inventory_items",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_nutrition_facts_inventory_item_id",
                table: "nutrition_facts",
                column: "inventory_item_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_inventory_items_category_id",
                table: "inventory_items",
                column: "category_id");

            migrationBuilder.AddForeignKey(
                name: "FK_inventory_items_product_categories_category_id",
                table: "inventory_items",
                column: "category_id",
                principalTable: "product_categories",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_nutrition_facts_inventory_items_inventory_item_id",
                table: "nutrition_facts",
                column: "inventory_item_id",
                principalTable: "inventory_items",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_nutrition_facts_product_cache_product_cache_id",
                table: "nutrition_facts",
                column: "product_cache_id",
                principalTable: "product_cache",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_inventory_items_product_categories_category_id",
                table: "inventory_items");

            migrationBuilder.DropForeignKey(
                name: "FK_nutrition_facts_inventory_items_inventory_item_id",
                table: "nutrition_facts");

            migrationBuilder.DropForeignKey(
                name: "FK_nutrition_facts_product_cache_product_cache_id",
                table: "nutrition_facts");

            migrationBuilder.DropIndex(
                name: "IX_nutrition_facts_inventory_item_id",
                table: "nutrition_facts");

            migrationBuilder.DropIndex(
                name: "IX_inventory_items_category_id",
                table: "inventory_items");

            migrationBuilder.DropColumn(
                name: "inventory_item_id",
                table: "nutrition_facts");

            migrationBuilder.DropColumn(
                name: "category_id",
                table: "inventory_items");

            migrationBuilder.AlterColumn<Guid>(
                name: "product_cache_id",
                table: "nutrition_facts",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_nutrition_facts_product_cache_product_cache_id",
                table: "nutrition_facts",
                column: "product_cache_id",
                principalTable: "product_cache",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
