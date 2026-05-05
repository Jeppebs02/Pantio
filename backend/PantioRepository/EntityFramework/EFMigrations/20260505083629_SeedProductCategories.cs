using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace PantioRepository.EntityFramework.EFMigrations
{
    /// <inheritdoc />
    public partial class SeedProductCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "product_categories",
                columns: new[] { "id", "default_shelf_life_days", "display_name", "off_tag" },
                values: new object[,]
                {
                    { 1, 4, "Fersk kød", "en:fresh-meats" },
                    { 2, 2, "Fersk fisk", "en:fresh-fish" },
                    { 3, 7, "Mælk", "en:milks" },
                    { 4, 14, "Yoghurt", "en:yogurts" },
                    { 5, 21, "Ost", "en:cheeses" },
                    { 6, 28, "Æg", "en:eggs" },
                    { 7, 7, "Mejeriprodukter", "en:dairy" },
                    { 8, 5, "Friske grøntsager", "en:fresh-vegetables" },
                    { 9, 5, "Frisk frugt", "en:fresh-fruits" },
                    { 10, 3, "Frisk brød", "en:fresh-bread" },
                    { 11, 5, "Pålæg", "en:cooked-meats" },
                    { 12, 7, "Brød", "en:bread" },
                    { 13, 30, "Drikkevarer", "en:beverages" },
                    { 14, 7, "Juice", "en:juices" },
                    { 15, 180, "Sovse og dressinger", "en:sauces" },
                    { 16, 180, "Krydderier", "en:condiments" },
                    { 17, 90, "Kiks og kager", "en:biscuits-and-cakes" },
                    { 18, 180, "Chokolade", "en:chocolate" },
                    { 19, 180, "Frosne fødevarer", "en:frozen-foods" },
                    { 20, 730, "Konservesvarer", "en:canned-foods" },
                    { 21, 730, "Pasta", "en:pasta" },
                    { 22, 730, "Ris", "en:rice" },
                    { 23, 365, "Morgenmadsprodukter", "en:cereals" },
                    { 24, 365, "Olie", "en:oils" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "product_categories",
                keyColumn: "id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "product_categories",
                keyColumn: "id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "product_categories",
                keyColumn: "id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "product_categories",
                keyColumn: "id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "product_categories",
                keyColumn: "id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "product_categories",
                keyColumn: "id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "product_categories",
                keyColumn: "id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "product_categories",
                keyColumn: "id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "product_categories",
                keyColumn: "id",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "product_categories",
                keyColumn: "id",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "product_categories",
                keyColumn: "id",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "product_categories",
                keyColumn: "id",
                keyValue: 12);

            migrationBuilder.DeleteData(
                table: "product_categories",
                keyColumn: "id",
                keyValue: 13);

            migrationBuilder.DeleteData(
                table: "product_categories",
                keyColumn: "id",
                keyValue: 14);

            migrationBuilder.DeleteData(
                table: "product_categories",
                keyColumn: "id",
                keyValue: 15);

            migrationBuilder.DeleteData(
                table: "product_categories",
                keyColumn: "id",
                keyValue: 16);

            migrationBuilder.DeleteData(
                table: "product_categories",
                keyColumn: "id",
                keyValue: 17);

            migrationBuilder.DeleteData(
                table: "product_categories",
                keyColumn: "id",
                keyValue: 18);

            migrationBuilder.DeleteData(
                table: "product_categories",
                keyColumn: "id",
                keyValue: 19);

            migrationBuilder.DeleteData(
                table: "product_categories",
                keyColumn: "id",
                keyValue: 20);

            migrationBuilder.DeleteData(
                table: "product_categories",
                keyColumn: "id",
                keyValue: 21);

            migrationBuilder.DeleteData(
                table: "product_categories",
                keyColumn: "id",
                keyValue: 22);

            migrationBuilder.DeleteData(
                table: "product_categories",
                keyColumn: "id",
                keyValue: 23);

            migrationBuilder.DeleteData(
                table: "product_categories",
                keyColumn: "id",
                keyValue: 24);
        }
    }
}
