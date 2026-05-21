using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PantioRepository.EntityFramework.EFMigrations
{
    /// <inheritdoc />
    public partial class LowercaseQuantityUnit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE inventory_items    SET quantity_unit   = lower(quantity_unit)   WHERE quantity_unit   IS NOT NULL;
                UPDATE shopping_list_items SET measuring_unit = lower(measuring_unit)  WHERE measuring_unit  IS NOT NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
