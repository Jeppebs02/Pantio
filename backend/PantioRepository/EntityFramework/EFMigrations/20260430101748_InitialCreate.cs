using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PantioRepository.EntityFramework.EFMigrations;

/// <inheritdoc />
public partial class InitialCreate : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "product_categories",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                off_tag = table.Column<string>(type: "text", nullable: false),
                display_name = table.Column<string>(type: "text", nullable: false),
                default_shelf_life_days = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_product_categories", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "users",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                email = table.Column<string>(type: "text", nullable: false),
                phone_number = table.Column<string>(type: "text", nullable: false),
                onboarding_done = table.Column<bool>(type: "boolean", nullable: false),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_users", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "inventories",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                user_id = table.Column<Guid>(type: "uuid", nullable: false),
                name = table.Column<string>(type: "text", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_inventories", x => x.id);
                table.ForeignKey(
                    name: "FK_inventories_users_user_id",
                    column: x => x.user_id,
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "product_cache",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                user_id = table.Column<Guid>(type: "uuid", nullable: false),
                ean = table.Column<string>(type: "text", nullable: false),
                category_id = table.Column<int>(type: "integer", nullable: true),
                product_name = table.Column<string>(type: "text", nullable: false),
                quantity = table.Column<string>(type: "text", nullable: true),
                quantity_unit = table.Column<string>(type: "text", nullable: true),
                cached_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_product_cache", x => x.id);
                table.ForeignKey(
                    name: "FK_product_cache_product_categories_category_id",
                    column: x => x.category_id,
                    principalTable: "product_categories",
                    principalColumn: "id");
                table.ForeignKey(
                    name: "FK_product_cache_users_user_id",
                    column: x => x.user_id,
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "recipes",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                user_id = table.Column<Guid>(type: "uuid", nullable: false),
                name = table.Column<string>(type: "text", nullable: false),
                description = table.Column<string>(type: "text", nullable: true),
                instructions = table.Column<string>(type: "text", nullable: true),
                portions = table.Column<float>(type: "real", nullable: false),
                completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_recipes", x => x.id);
                table.ForeignKey(
                    name: "FK_recipes_users_user_id",
                    column: x => x.user_id,
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "shopping_lists",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                user_id = table.Column<Guid>(type: "uuid", nullable: false),
                name = table.Column<string>(type: "text", nullable: false),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_shopping_lists", x => x.id);
                table.ForeignKey(
                    name: "FK_shopping_lists_users_user_id",
                    column: x => x.user_id,
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "store_connections",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                user_id = table.Column<Guid>(type: "uuid", nullable: false),
                chain = table.Column<string>(type: "text", nullable: false),
                gigya_session_token = table.Column<string>(type: "text", nullable: true),
                access_token = table.Column<string>(type: "text", nullable: true),
                refresh_token = table.Column<string>(type: "text", nullable: true),
                id_token = table.Column<string>(type: "text", nullable: true),
                token_expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                last_polled_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                connected_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                disconnected_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_store_connections", x => x.id);
                table.ForeignKey(
                    name: "FK_store_connections_users_user_id",
                    column: x => x.user_id,
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "user_profiles",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                user_id = table.Column<Guid>(type: "uuid", nullable: false),
                display_name = table.Column<string>(type: "text", nullable: true),
                household_size = table.Column<int>(type: "integer", nullable: true),
                locale = table.Column<string>(type: "text", nullable: true),
                notification_prefs = table.Column<string>(type: "jsonb", nullable: true),
                updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_user_profiles", x => x.id);
                table.ForeignKey(
                    name: "FK_user_profiles_users_user_id",
                    column: x => x.user_id,
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "nutrition_facts",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                product_cache_id = table.Column<Guid>(type: "uuid", nullable: false),
                energy_kcal_100g = table.Column<float>(type: "real", nullable: true),
                carbohydrates_100g = table.Column<float>(type: "real", nullable: true),
                sugars_100g = table.Column<float>(type: "real", nullable: true),
                fat_100g = table.Column<float>(type: "real", nullable: true),
                saturated_fat_100g = table.Column<float>(type: "real", nullable: true),
                proteins_100g = table.Column<float>(type: "real", nullable: true),
                salt_100g = table.Column<float>(type: "real", nullable: true),
                nutrition_data_per = table.Column<string>(type: "text", nullable: true),
                cached_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_nutrition_facts", x => x.id);
                table.ForeignKey(
                    name: "FK_nutrition_facts_product_cache_product_cache_id",
                    column: x => x.product_cache_id,
                    principalTable: "product_cache",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "shopping_list_items",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                shopping_list_id = table.Column<Guid>(type: "uuid", nullable: false),
                name = table.Column<string>(type: "text", nullable: false),
                quantity = table.Column<float>(type: "real", nullable: true),
                measuring_unit = table.Column<string>(type: "text", nullable: true),
                is_checked = table.Column<bool>(type: "boolean", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_shopping_list_items", x => x.id);
                table.ForeignKey(
                    name: "FK_shopping_list_items_shopping_lists_shopping_list_id",
                    column: x => x.shopping_list_id,
                    principalTable: "shopping_lists",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "receipts",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                store_connection_id = table.Column<Guid>(type: "uuid", nullable: false),
                user_id = table.Column<Guid>(type: "uuid", nullable: false),
                dsg_receipt_id = table.Column<string>(type: "text", nullable: false),
                store_name = table.Column<string>(type: "text", nullable: true),
                receipt_type = table.Column<string>(type: "text", nullable: true),
                sales_total_dkk = table.Column<float>(type: "real", nullable: false),
                member_discount_dkk = table.Column<float>(type: "real", nullable: false),
                other_discount_dkk = table.Column<float>(type: "real", nullable: false),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                imported_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_receipts", x => x.id);
                table.ForeignKey(
                    name: "FK_receipts_store_connections_store_connection_id",
                    column: x => x.store_connection_id,
                    principalTable: "store_connections",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_receipts_users_user_id",
                    column: x => x.user_id,
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "receipt_lines",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                receipt_id = table.Column<Guid>(type: "uuid", nullable: false),
                ean = table.Column<string>(type: "text", nullable: true),
                article_description = table.Column<string>(type: "text", nullable: true),
                sales_price_dkk = table.Column<float>(type: "real", nullable: false),
                normal_price_dkk = table.Column<float>(type: "real", nullable: false),
                discount_dkk = table.Column<float>(type: "real", nullable: false),
                discounts = table.Column<string>(type: "jsonb", nullable: true),
                qty_in_sales_unit = table.Column<float>(type: "real", nullable: false),
                tax_amount_dkk = table.Column<float>(type: "real", nullable: false),
                item_type = table.Column<string>(type: "text", nullable: true),
                processed_to_inventory = table.Column<bool>(type: "boolean", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_receipt_lines", x => x.id);
                table.ForeignKey(
                    name: "FK_receipt_lines_receipts_receipt_id",
                    column: x => x.receipt_id,
                    principalTable: "receipts",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "inventory_items",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                inventory_id = table.Column<Guid>(type: "uuid", nullable: false),
                ean = table.Column<string>(type: "text", nullable: true),
                receipt_line_id = table.Column<Guid>(type: "uuid", nullable: true),
                product_name = table.Column<string>(type: "text", nullable: false),
                quantity = table.Column<float>(type: "real", nullable: false),
                quantity_unit = table.Column<string>(type: "text", nullable: true),
                status = table.Column<string>(type: "text", nullable: false),
                added_via = table.Column<string>(type: "text", nullable: false),
                storage_location = table.Column<string>(type: "text", nullable: true),
                added_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UserId = table.Column<Guid>(type: "uuid", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_inventory_items", x => x.id);
                table.ForeignKey(
                    name: "FK_inventory_items_inventories_inventory_id",
                    column: x => x.inventory_id,
                    principalTable: "inventories",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_inventory_items_receipt_lines_receipt_line_id",
                    column: x => x.receipt_line_id,
                    principalTable: "receipt_lines",
                    principalColumn: "id");
                table.ForeignKey(
                    name: "FK_inventory_items_users_UserId",
                    column: x => x.UserId,
                    principalTable: "users",
                    principalColumn: "id");
            });

        migrationBuilder.CreateTable(
            name: "expiry_dates",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                inventory_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                estimated_expiry = table.Column<DateOnly>(type: "date", nullable: false),
                is_manual_override = table.Column<bool>(type: "boolean", nullable: false),
                override_date = table.Column<DateOnly>(type: "date", nullable: true),
                category_default_used_days = table.Column<int>(type: "integer", nullable: true),
                notification_sent_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_expiry_dates", x => x.id);
                table.ForeignKey(
                    name: "FK_expiry_dates_inventory_items_inventory_item_id",
                    column: x => x.inventory_item_id,
                    principalTable: "inventory_items",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "recipe_entries",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                recipe_id = table.Column<Guid>(type: "uuid", nullable: false),
                inventory_item_id = table.Column<Guid>(type: "uuid", nullable: true),
                product_name = table.Column<string>(type: "text", nullable: false),
                quantity = table.Column<float>(type: "real", nullable: false),
                measuring_unit = table.Column<string>(type: "text", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_recipe_entries", x => x.id);
                table.ForeignKey(
                    name: "FK_recipe_entries_inventory_items_inventory_item_id",
                    column: x => x.inventory_item_id,
                    principalTable: "inventory_items",
                    principalColumn: "id");
                table.ForeignKey(
                    name: "FK_recipe_entries_recipes_recipe_id",
                    column: x => x.recipe_id,
                    principalTable: "recipes",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "expiry_notifications",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                expiry_date_id = table.Column<Guid>(type: "uuid", nullable: false),
                user_id = table.Column<Guid>(type: "uuid", nullable: false),
                days_before_expiry = table.Column<int>(type: "integer", nullable: false),
                channel = table.Column<string>(type: "text", nullable: false),
                sent_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                acknowledged = table.Column<bool>(type: "boolean", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_expiry_notifications", x => x.id);
                table.ForeignKey(
                    name: "FK_expiry_notifications_expiry_dates_expiry_date_id",
                    column: x => x.expiry_date_id,
                    principalTable: "expiry_dates",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_expiry_notifications_users_user_id",
                    column: x => x.user_id,
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_expiry_dates_estimated_expiry",
            table: "expiry_dates",
            column: "estimated_expiry");

        migrationBuilder.CreateIndex(
            name: "IX_expiry_dates_inventory_item_id",
            table: "expiry_dates",
            column: "inventory_item_id",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_expiry_notifications_expiry_date_id",
            table: "expiry_notifications",
            column: "expiry_date_id");

        migrationBuilder.CreateIndex(
            name: "IX_expiry_notifications_user_id",
            table: "expiry_notifications",
            column: "user_id");

        migrationBuilder.CreateIndex(
            name: "IX_inventories_user_id",
            table: "inventories",
            column: "user_id");

        migrationBuilder.CreateIndex(
            name: "IX_inventory_items_ean",
            table: "inventory_items",
            column: "ean");

        migrationBuilder.CreateIndex(
            name: "IX_inventory_items_inventory_id_status",
            table: "inventory_items",
            columns: new[] { "inventory_id", "status" });

        migrationBuilder.CreateIndex(
            name: "IX_inventory_items_receipt_line_id",
            table: "inventory_items",
            column: "receipt_line_id",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_inventory_items_UserId",
            table: "inventory_items",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_nutrition_facts_product_cache_id",
            table: "nutrition_facts",
            column: "product_cache_id",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_product_cache_category_id",
            table: "product_cache",
            column: "category_id");

        migrationBuilder.CreateIndex(
            name: "IX_product_cache_user_id_ean",
            table: "product_cache",
            columns: new[] { "user_id", "ean" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_receipt_lines_receipt_id",
            table: "receipt_lines",
            column: "receipt_id");

        migrationBuilder.CreateIndex(
            name: "IX_receipts_dsg_receipt_id",
            table: "receipts",
            column: "dsg_receipt_id",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_receipts_store_connection_id",
            table: "receipts",
            column: "store_connection_id");

        migrationBuilder.CreateIndex(
            name: "IX_receipts_user_id",
            table: "receipts",
            column: "user_id");

        migrationBuilder.CreateIndex(
            name: "IX_recipe_entries_inventory_item_id",
            table: "recipe_entries",
            column: "inventory_item_id",
            filter: "inventory_item_id IS NULL");

        migrationBuilder.CreateIndex(
            name: "IX_recipe_entries_recipe_id",
            table: "recipe_entries",
            column: "recipe_id");

        migrationBuilder.CreateIndex(
            name: "IX_recipes_user_id",
            table: "recipes",
            column: "user_id");

        migrationBuilder.CreateIndex(
            name: "IX_shopping_list_items_shopping_list_id_is_checked",
            table: "shopping_list_items",
            columns: new[] { "shopping_list_id", "is_checked" });

        migrationBuilder.CreateIndex(
            name: "IX_shopping_lists_user_id",
            table: "shopping_lists",
            column: "user_id");

        migrationBuilder.CreateIndex(
            name: "IX_store_connections_last_polled_at",
            table: "store_connections",
            column: "last_polled_at");

        migrationBuilder.CreateIndex(
            name: "IX_store_connections_user_id_chain",
            table: "store_connections",
            columns: new[] { "user_id", "chain" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_user_profiles_user_id",
            table: "user_profiles",
            column: "user_id",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "expiry_notifications");

        migrationBuilder.DropTable(
            name: "nutrition_facts");

        migrationBuilder.DropTable(
            name: "recipe_entries");

        migrationBuilder.DropTable(
            name: "shopping_list_items");

        migrationBuilder.DropTable(
            name: "user_profiles");

        migrationBuilder.DropTable(
            name: "expiry_dates");

        migrationBuilder.DropTable(
            name: "product_cache");

        migrationBuilder.DropTable(
            name: "recipes");

        migrationBuilder.DropTable(
            name: "shopping_lists");

        migrationBuilder.DropTable(
            name: "inventory_items");

        migrationBuilder.DropTable(
            name: "product_categories");

        migrationBuilder.DropTable(
            name: "inventories");

        migrationBuilder.DropTable(
            name: "receipt_lines");

        migrationBuilder.DropTable(
            name: "receipts");

        migrationBuilder.DropTable(
            name: "store_connections");

        migrationBuilder.DropTable(
            name: "users");
    }
}
