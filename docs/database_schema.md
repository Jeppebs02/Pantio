# Relational Database Schema — MVP

```mermaid
erDiagram

  users {
    uuid id PK
    string email
    string auth0_sub UK
    string phone_number "nullable"
    boolean onboarding_done
    timestamp created_at
    timestamp updated_at
  }

  user_profiles {
    uuid id PK
    uuid user_id FK
    string display_name
    integer household_size
    string locale
    jsonb notification_prefs
    timestamp updated_at
  }

  product_categories {
    integer id PK
    string off_tag UK
    string display_name
    integer default_shelf_life_days
  }

  product_cache {
    uuid id PK
    uuid user_id FK
    string ean UK
    integer category_id FK
    string product_name
    string quantity
    string quantity_unit
    timestamp cached_at
  }

  nutrition_facts {
    uuid id PK
    uuid product_cache_id FK "nullable"
    uuid inventory_item_id FK "nullable"
    float energy_kcal_100g
    float carbohydrates_100g
    float sugars_100g
    float fat_100g
    float saturated_fat_100g
    float proteins_100g
    float salt_100g
    string nutrition_data_per
    timestamp cached_at
  }

  store_connections {
    uuid id PK
    uuid user_id FK
    string chain
    string gigya_session_token
    string access_token
    string refresh_token
    string id_token
    timestamp token_expires_at
    timestamp last_polled_at
    timestamp connected_at
    timestamp disconnected_at
  }

  receipts {
    uuid id PK
    uuid store_connection_id FK
    uuid user_id FK
    string dsg_receipt_id UK
    string store_name
    string receipt_type
    float sales_total_dkk
    float member_discount_dkk
    float other_discount_dkk
    timestamp created_at
    timestamp imported_at
  }

  receipt_lines {
    uuid id PK
    uuid receipt_id FK
    string ean
    string article_description
    float sales_price_dkk
    float normal_price_dkk
    float discount_dkk
    jsonb discounts
    float qty_in_sales_unit
    float tax_amount_dkk
    string item_type
    boolean processed_to_inventory
  }

  inventories {
    uuid id PK
    uuid user_id FK
    string name
  }

  inventory_items {
    uuid id PK
    uuid inventory_id FK
    string ean
    uuid receipt_line_id FK "nullable"
    integer category_id FK "nullable"
    string product_name
    float quantity
    string quantity_unit
    string status
    string added_via
    string storage_location
    timestamp added_at
    timestamp updated_at
  }

  expiry_dates {
    uuid id PK
    uuid inventory_item_id FK
    date estimated_expiry
    boolean is_manual_override
    date override_date
    integer category_default_used_days
    timestamp notification_sent_at
  }

  expiry_notifications {
    uuid id PK
    uuid expiry_date_id FK
    uuid user_id FK
    integer days_before_expiry
    string channel
    timestamp sent_at
    boolean acknowledged
  }

  shopping_lists {
    uuid id PK
    uuid user_id FK
    string name
    timestamp created_at
    timestamp updated_at
  }

  shopping_list_items {
    uuid id PK
    uuid shopping_list_id FK
    string name
    float quantity
    string measuring_unit
    boolean is_checked
  }

  recipes {
    uuid id PK
    uuid user_id FK
    string name
    string description
    string instructions
    float portions
    timestamp completed_at
    timestamp created_at
  }

  recipe_entries {
    uuid id PK
    uuid recipe_id FK
    uuid inventory_item_id FK
    string product_name
    float quantity
    string measuring_unit
  }

  users ||--o| user_profiles : "has"
  users ||--o{ store_connections : "connects via"
  users ||--o{ inventories : "owns"
  users ||--o{ shopping_lists : "has"
  users ||--o{ receipts : "has"
  users ||--o{ expiry_notifications : "receives"
  users ||--o{ product_cache : "caches"
  users ||--o{ recipes : "has"

  product_categories ||--o{ product_cache : "classifies"
  product_categories ||--o{ inventory_items : "classifies"
  product_cache ||--o| nutrition_facts : "has"
  inventory_items ||--o| nutrition_facts : "has"

  store_connections ||--o{ receipts : "imports"
  receipts ||--o{ receipt_lines : "contains"
  receipt_lines ||--o| inventory_items : "creates"

  inventories ||--o{ inventory_items : "contains"
  inventory_items ||--|| expiry_dates : "has"
  expiry_dates ||--o{ expiry_notifications : "triggers"

  shopping_lists ||--o{ shopping_list_items : "contains"

  recipes ||--o{ recipe_entries : "contains"
  recipe_entries }o--o| inventory_items : "fulfilled by"

## Table notes

| Table | Key design decisions |
|---|---|
| `users` | `auth0_sub` is unique and stores the Auth0 subject used to map JWTs to internal users. `phone_number` is nullable. |
| `user_profiles` | 1:1 with `users`. Separated to keep the auth table lean. |
| `product_categories` | `off_tag` maps to an entry in `categories_tags[]` from the OFF response (e.g. `en:energy-drinks`). `default_shelf_life_days` drives expiry estimation. |
| `product_cache` | Per-user OFF cache. Unique on `(user_id, ean)`. `cached_at` enables TTL-based invalidation. |
| `nutrition_facts` | Shared table. Each row is owned by either a `product_cache` row or an `inventory_items` row — `product_cache_id` and `inventory_item_id` are both nullable, exactly one is set. Unique index on each. All values per-100g/100ml. `nutrition_data_per` stores the OFF field so the display unit is always known. |
| `store_connections` | One row per user per chain. `chain` enum: `netto`, `fotex`, `bilka`. `last_polled_at` drives the polling scheduler. Token refresh uses `client_id=customer-program`. |
| `receipts` | `dsg_receipt_id` unique constraint ensures idempotent import. `receipt_type`: `merged` or `full`. Monetary values in DKK. |
| `receipt_lines` | Sourced from `GET /api/cp/receipt/details?type=merged&receiptId={id}`. `item_type` `01` = product, `02` = deposit/pant — only `01` lines processed to inventory. `processed_to_inventory` prevents duplicates on re-poll. |
| `inventory_items` | Full product snapshot at add time: `product_name`, `quantity`, `quantity_unit`, `category_id`, and a linked `nutrition_facts` row all survive cache eviction and OFF outages. `ean` is a plain string, not a FK. `receipt_line_id` nullable for barcode/manual sources. `category_id` nullable until resolved by the OFF service. `added_via` enum: `receipt`, `barcode`, `manual`. `status` enum: `available`, `low`, `expired`, `consumed`. |
| `expiry_dates` | 1:1 with `inventory_items`. `estimated_expiry` = `added_at + category.default_shelf_life_days`. |
| `expiry_notifications` | `channel` enum: `push`, `in_app`. |
| `shopping_list_items` | Free-text in MVP. No FK to `recipe_entries` — missing ingredient resolution happens via case-insensitive name matching at the application layer. |
| `recipes` | Scoped to `user_id`. `completed_at` nullable — null = in progress, set = done. `instructions` is plain text in MVP. |
| `recipe_entries` | `inventory_item_id` nullable — null means the ingredient is missing from inventory. Missing is derived (`inventory_item_id IS NULL`), not stored as a flag. Resolution: when a new `InventoryItem` is created, the application runs a case-insensitive match against `recipe_entries.product_name` for the same user where `inventory_item_id IS NULL`. On match: `inventory_item_id` set, corresponding `ShoppingListItem` deleted. On completion: `inventory_items.quantity` decremented per entry via `inventory_item_id`. |

## Suggested indexes

```sql
-- Auth & polling
CREATE UNIQUE INDEX ON users (auth0_sub);
CREATE UNIQUE INDEX ON store_connections (user_id, chain);
CREATE INDEX ON store_connections (last_polled_at);
CREATE UNIQUE INDEX ON receipts (dsg_receipt_id);

-- Inventory queries
CREATE INDEX ON inventory_items (user_id, status);
CREATE INDEX ON inventory_items (ean);
CREATE INDEX ON inventory_items (category_id);
CREATE UNIQUE INDEX ON nutrition_facts (product_cache_id);
CREATE UNIQUE INDEX ON nutrition_facts (inventory_item_id);

-- Expiry dashboard
CREATE INDEX ON expiry_dates (estimated_expiry);
CREATE INDEX ON expiry_dates (inventory_item_id);

-- OFF cache lookup
CREATE UNIQUE INDEX ON product_cache (user_id, ean);

-- Shopping list
CREATE INDEX ON shopping_list_items (shopping_list_id, is_checked);

-- Recipe entry name matching (missing ingredient resolution)
CREATE INDEX ON recipe_entries (recipe_id);
CREATE INDEX ON recipe_entries (inventory_item_id) WHERE inventory_item_id IS NULL;
```
