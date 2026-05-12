using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PantioRepository.EntityFramework.EFMigrations
{
    /// <inheritdoc />
    public partial class ReceiptLineInventoryItemsOneToMany : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_inventory_items_receipt_line_id",
                table: "inventory_items");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_items_receipt_line_id",
                table: "inventory_items",
                column: "receipt_line_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_inventory_items_receipt_line_id",
                table: "inventory_items");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_items_receipt_line_id",
                table: "inventory_items",
                column: "receipt_line_id",
                unique: true);
        }
    }
}
