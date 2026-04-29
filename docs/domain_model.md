# Domain Model — MVP

```mermaid
classDiagram
direction TB

  %% ── USER CONTEXT ──
  class User {
    +UUID id PK
    +String email UK
    +String phone_number UK
    +Boolean onboarding_done
    +Timestamp created_at
    +Timestamp updated_at
  }

  class UserProfile {
    +UUID id PK
    +UUID user_id FK
    +String display_name
    +Integer household_size
    +String locale
    +JSONB notification_prefs
    +Timestamp updated_at
  }

  %% ── PRODUCT CATALOGUE CONTEXT ──
  %% No Product mirror. Data fetched from OFF at lookup time and cached per user.
  %% GET https://world.openfoodfacts.org/api/v2/product/{ean}
  class ProductCategory {
    +Integer id PK
    +String off_tag UK
    +String display_name
    +Integer default_shelf_life_days
  }

  class ProductCache {
    +UUID id PK
    +UUID user_id FK
    +String ean UK
    +Integer category_id FK
    +String product_name
    +String quantity
    +String quantity_unit
    +Timestamp cached_at
  }

  class NutritionFacts {
    +UUID id PK
    +UUID product_cache_id FK
    +Float energy_kcal_100g
    +Float carbohydrates_100g
    +Float sugars_100g
    +Float fat_100g
    +Float saturated_fat_100g
    +Float proteins_100g
    +Float salt_100g
    +String nutrition_data_per
    +Timestamp cached_at
  }

  %% ── INVENTORY CONTEXT ──
  class InventoryItem {
    +UUID id PK
    +UUID user_id FK
    +String ean
    +UUID receipt_line_id FK
    +String product_name
    +Float quantity
    +String quantity_unit
    +String status
    +String added_via
    +String storage_location
    +Timestamp added_at
    +Timestamp updated_at
  }

  class ExpiryDate {
    +UUID id PK
    +UUID inventory_item_id FK
    +Date estimated_expiry
    +Boolean is_manual_override
    +Date override_date
    +Integer category_default_used_days
    +Timestamp notification_sent_at
  }

  class ExpiryNotification {
    +UUID id PK
    +UUID expiry_date_id FK
    +UUID user_id FK
    +Integer days_before_expiry
    +String channel
    +Timestamp sent_at
    +Boolean acknowledged
  }

  %% ── SUPERMARKET INTEGRATION CONTEXT ──
  %% Receipt list:   GET https://p-club.dsgapps.dk/api/cp/receipt
  %% Receipt detail: GET https://p-club.dsgapps.dk/api/cp/receipt/details?type=merged&receiptId={id}
  class StoreConnection {
    +UUID id PK
    +UUID user_id FK
    +String chain
    +String gigya_session_token
    +String access_token
    +String refresh_token
    +String id_token
    +Timestamp token_expires_at
    +Timestamp last_polled_at
    +Timestamp connected_at
    +Timestamp disconnected_at
  }

  class Receipt {
    +UUID id PK
    +UUID store_connection_id FK
    +UUID user_id FK
    +String dsg_receipt_id UK
    +String store_name
    +String receipt_type
    +Float sales_total_dkk
    +Float member_discount_dkk
    +Float other_discount_dkk
    +Timestamp created_at
    +Timestamp imported_at
  }

  class ReceiptLine {
    +UUID id PK
    +UUID receipt_id FK
    +String ean
    +String article_description
    +Float sales_price_dkk
    +Float normal_price_dkk
    +Float discount_dkk
    +JSONB discounts
    +Float qty_in_sales_unit
    +Float tax_amount_dkk
    +String item_type
    +Boolean processed_to_inventory
  }

  %% ── SHOPPING LIST CONTEXT ──
  class ShoppingList {
    +UUID id PK
    +UUID user_id FK
    +String name
    +Timestamp created_at
    +Timestamp updated_at
  }

  class ShoppingListItem {
    +UUID id PK
    +UUID shopping_list_id FK
    +String name
    +Float quantity
    +String measuring_unit
    +Boolean is_checked
  }

  %% ── RECIPE CONTEXT ──
  class Recipe {
    +UUID id PK
    +UUID user_id FK
    +String name
    +String description
    +String instructions
    +Float portions
    +Timestamp completed_at
    +Timestamp created_at
  }

  class RecipeEntry {
    +UUID id PK
    +UUID recipe_id FK
    +UUID inventory_item_id FK
    +String product_name
    +Float quantity
    +String measuring_unit
  }

  %% ── RELATIONSHIPS ──
  User "1" --> "0..1" UserProfile : has
  User "1" --> "0..*" StoreConnection : connects via
  User "1" --> "0..*" InventoryItem : owns
  User "1" --> "0..*" ShoppingList : has
  User "1" --> "0..*" Receipt : has
  User "1" --> "0..*" ExpiryNotification : receives
  User "1" --> "0..*" ProductCache : caches
  User "1" --> "0..*" Recipe : has

  ProductCategory "1" --> "0..*" ProductCache : classifies
  ProductCache "1" --> "0..1" NutritionFacts : has

  StoreConnection "1" --> "0..*" Receipt : imports via polling
  Receipt "1" --> "1..*" ReceiptLine : contains
  ReceiptLine "1" --> "0..1" InventoryItem : creates

  InventoryItem "1" --> "1" ExpiryDate : has
  ExpiryDate "1" --> "0..*" ExpiryNotification : triggers

  ShoppingList "1" --> "0..*" ShoppingListItem : contains

  Recipe "1" --> "1..*" RecipeEntry : contains
  RecipeEntry "0..*" --> "0..1" InventoryItem : fulfilled by
```

## Context boundaries

| Context | Aggregates | Notes |
|---|---|---|
| User | `User`, `UserProfile` | Auth identity, preferences, onboarding state |
| Product catalogue | `ProductCache`, `NutritionFacts`, `ProductCategory` | No Product mirror. Thin per-user OFF cache. `InventoryItem` snapshots name and unit at add time. |
| Inventory | `InventoryItem`, `ExpiryDate`, `ExpiryNotification` | Core domain. Expiry estimated from `ProductCategory.default_shelf_life_days`. |
| Supermarket integration | `StoreConnection`, `Receipt`, `ReceiptLine` | DSG Club API (`p-club.dsgapps.dk`). OAuth via Gigya + PKCE, `customer-program` client. |
| Shopping list | `ShoppingList`, `ShoppingListItem` | Free-text entries in MVP. No direct link to `RecipeEntry` — fulfilled via name matching. |
| Recipe | `Recipe`, `RecipeEntry` | AI-suggested. Lifecycle driven by `RecipeEntry.inventory_item_id` nullability. |

## Key domain rules

- No `Product` table. OFF is the source of truth. `ProductCache` is a thin per-user cache — evictable at any time.
- `InventoryItem` snapshots `product_name`, `quantity`, and `quantity_unit` at add time. These survive cache eviction and OFF outages.
- `ProductCategory.off_tag` maps to an entry in `categories_tags[]` from the OFF response. Resolved once at cache time.
- `ExpiryDate.estimated_expiry` = `InventoryItem.added_at + ProductCategory.default_shelf_life_days`.
- `NutritionFacts` is 1:0..1 with `ProductCache`. All values are per-100g/100ml as returned by OFF.
- Polling: `GET /api/cp/receipt` → diff against known `dsg_receipt_id` values → call `GET /api/cp/receipt/details?type=merged&receiptId={id}` for new IDs.
- `ReceiptLine.item_type` `01` = product, `02` = deposit/pant. Only `01` lines are processed to inventory.
- `ReceiptLine.processed_to_inventory` prevents duplicate `InventoryItem` creation on re-poll.
- All monetary values from the Club API are in **DKK**. Stored as `FLOAT` matching the API.
- `StoreConnection` token refresh uses `client_id=customer-program` — never `scan-and-go-native`.
- `InventoryItem.added_via` enum: `receipt`, `barcode`, `manual`.
- `InventoryItem.status` enum: `available`, `low`, `expired`, `consumed`.

## Recipe lifecycle

1. AI suggests a recipe → `Recipe` + `RecipeEntry` rows created. `inventory_item_id` set where inventory items exist, `NULL` where ingredients are missing.
2. Missing entries (`inventory_item_id IS NULL`) → a `ShoppingListItem` is created per missing `RecipeEntry` using `RecipeEntry.product_name` as the item name.
3. User purchases item → `InventoryItem` created → matcher runs case-insensitive comparison of `InventoryItem.product_name` against all `RecipeEntry.product_name` where `inventory_item_id IS NULL` for that user. On match: `RecipeEntry.inventory_item_id` set, corresponding `ShoppingListItem` deleted.
4. All `RecipeEntry` rows for a recipe have `inventory_item_id` set → recipe is actionable.
5. User marks recipe complete → `Recipe.completed_at` set, `InventoryItem.quantity` decremented per `RecipeEntry` via `inventory_item_id`.
