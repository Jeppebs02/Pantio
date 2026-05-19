using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PantioRepository.EntityFramework.EFMigrations
{
    /// <inheritdoc />
    public partial class NormalizeQuantityUnit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Normalise existing free-text quantity_unit values to match QuantityUnit enum member names.
            migrationBuilder.Sql("""
                UPDATE inventory_items SET quantity_unit = 'L'  WHERE lower(quantity_unit) = 'l';
                UPDATE inventory_items SET quantity_unit = 'Ml' WHERE lower(quantity_unit) = 'ml';
                UPDATE inventory_items SET quantity_unit = 'Cl' WHERE lower(quantity_unit) = 'cl';
                UPDATE inventory_items SET quantity_unit = 'Dl' WHERE lower(quantity_unit) = 'dl';
                UPDATE inventory_items SET quantity_unit = 'Kg' WHERE lower(quantity_unit) = 'kg';
                UPDATE inventory_items SET quantity_unit = 'G'  WHERE lower(quantity_unit) = 'g';
                UPDATE inventory_items SET quantity_unit = 'Mg' WHERE lower(quantity_unit) = 'mg';
                UPDATE inventory_items
                SET quantity_unit = NULL
                WHERE quantity_unit IS NOT NULL
                  AND quantity_unit NOT IN ('L','Ml','Cl','Dl','Kg','G','Mg');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
