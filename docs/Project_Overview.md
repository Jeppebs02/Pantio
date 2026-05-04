# Pantio — Project Overview

Pantio is a household inventory management app. Users connect their supermarket loyalty accounts to automatically import receipts, which are processed into a personal food inventory with expiry tracking, shopping lists, and AI-suggested recipes.

---

## Tech Stack

| Layer | Technology |
|---|---|
| Runtime | .NET 10 |
| Web framework | ASP.NET Core Web API (minimal hosting model) |
| Database | PostgreSQL 16+ |
| ORM | Entity Framework Core 10 (Npgsql provider) |
| Containerisation | Docker (multi-stage Linux build) |
| Testing | NUnit 4, Moq 4, EF Core InMemory |
| API docs | Built-in OpenAPI (`/openapi/v1.json` in Development) |

---

## Solution Structure

```
backend/
  PantioClassLibrary/       # Shared kernel — entities, DTOs, interfaces, enums
  PantioRepository/         # Data access — EF DbContext, repositories, mappers, migrations
  PantioAPI/                # ASP.NET Core host — controllers, services, DI wiring
  PantioTest/               # Test project — unit & integration tests
docs/                       # Project documentation
```

### PantioClassLibrary
Contains everything that crosses layer boundaries. No dependencies on other projects.

```
Entities/         # EF entity classes (table-mapped POCOs)
DTO/              # Request/response data transfer objects (C# records)
Interfaces/
  Repository/     # IInventoryRepository, IInventoryItemRepository, …
  Services/       # IInventoryService, IInventoryItemService, …
Enums/            # InventoryStatus, AddedVia, StoreChain, NotificationChannel
```

### PantioRepository
Depends on `PantioClassLibrary`. Implements the repository interfaces and owns the EF setup.

```
EntityFramework/
  PantioDbContext.cs          # DbContext — DbSets, OnModelCreating (enums → string, indexes)
  Repositories/               # InventoryRepository, InventoryItemRepository, …
  EFMigrations/               # EF Core migration files
Mapper/                       # Static mapper classes (entity ↔ DTO)
```

### PantioAPI
Depends on both libraries. Entry point is `Program.cs` (minimal hosting, no `Startup.cs`).

```
Controllers/      # MVC controllers (attribute-routed)
Services/         # Service implementations — orchestrate repos, call mappers
Program.cs        # DI registration, middleware pipeline, EF configuration
```

### PantioTest
```
ControllerTests/    # Unit tests — mock IService, assert HTTP result types
ServiceTests/       # Unit tests — mock IRepository, assert mapped output
RepositoryTests/    # Integration tests — EF InMemory, real DbContext per test
```

---

## Architecture & Patterns

**Clean layered architecture with repository pattern:**

```
Controller  →  IService  →  IRepository  →  DbContext
```

- **Controllers** receive HTTP requests, delegate entirely to a service, return `IActionResult`.
- **Services** contain business logic, call repository interfaces, use mappers to convert between entities and DTOs.
- **Repositories** are the only layer that touches `DbContext`. Each method is `async` and accepts a `CancellationToken`.
- **Mappers** are static classes per entity (`InventoryMapper`, `InventoryItemMapper`). `ToDto` and `ToEntity` are the two methods.
- **Interfaces** live in `PantioClassLibrary` so both `PantioAPI` and `PantioTest` can reference them without pulling in the concrete implementations.

---

## Domain Model

### Core entities

| Entity | Table | Key relationships |
|---|---|---|
| `User` | `users` | Root aggregate — owns inventories, receipts, shopping lists, recipes |
| `UserProfile` | `user_profiles` | 1:1 with User, holds display name, locale, notification prefs (JSONB) |
| `Inventory` | `inventories` | User owns 1..* inventories (e.g. "Fridge", "Pantry") |
| `InventoryItem` | `inventory_items` | Belongs to an Inventory; optionally linked to a ReceiptLine |
| `ExpiryDate` | `expiry_dates` | 1:1 with InventoryItem; estimated from category shelf-life |
| `ExpiryNotification` | `expiry_notifications` | Triggered by ExpiryDate; scoped to User |
| `StoreConnection` | `store_connections` | OAuth token store per user per chain (Netto, Føtex, Bilka) |
| `Receipt` | `receipts` | Imported via DSG Club API polling |
| `ReceiptLine` | `receipt_lines` | Line items on a receipt; `processed_to_inventory` prevents re-import |
| `ProductCache` | `product_cache` | Per-user OFF (Open Food Facts) cache, keyed on `(user_id, ean)` |
| `NutritionFacts` | `nutrition_facts` | 1:1 with ProductCache; all values per 100g/100ml |
| `ShoppingList` | `shopping_lists` | User-owned list |
| `ShoppingListItem` | `shopping_list_items` | Free-text in MVP; matched to recipe entries by name |
| `Recipe` | `recipes` | AI-suggested; `completed_at` null = in progress |
| `RecipeEntry` | `recipe_entries` | Ingredient line; `inventory_item_id` null = missing from inventory |

### Key enums

| Enum | Values |
|---|---|
| `InventoryStatus` | `Available`, `Low`, `Expired`, `Consumed` |
| `AddedVia` | `Receipt`, `Barcode`, `Manual` |
| `StoreChain` | `Netto`, `Fotex`, `Bilka` |
| `NotificationChannel` | `Push`, `InApp` |

All enums are stored as strings in the database (configured via `HasConversion<string>()` in `OnModelCreating`).

---

## API Conventions

- Routes are attribute-routed and follow the pattern `api/{parent-resource}/{parentId:guid}/{child-resource}` for nested resources (e.g. items under an inventory, inventories under a user).
- All IDs in routes use the `:guid` constraint.
- Controllers return `IActionResult` — typically `Ok`, `CreatedAtAction`, `NoContent`, or `NotFound`. No raw status codes.
- `POST` returns `201 Created` with the created resource in the body and a `Location` header pointing to the collection.
- `DELETE` returns `204 No Content` on success, `404 Not Found` if the resource does not exist.
- All controller action methods accept a `CancellationToken` parameter bound automatically from the request lifetime.
- For the full list of available endpoints, run the API in Development and browse `/openapi/v1.json`.

---

## Testing Approach

All tests follow the **Arrange / Act / Assert** structure using `#region` blocks.

| Test type | Location | Dependencies mocked |
|---|---|---|
| Controller unit tests | `ControllerTests/` | `IService` mocked with Moq |
| Service unit tests | `ServiceTests/` | `IRepository` mocked with Moq |
| Repository integration tests | `RepositoryTests/` | Real `PantioDbContext` with EF InMemory; fresh DB per test via `Guid.NewGuid()` database name |

Run all tests:
```bash
dotnet test backend/PantioTest/PantioTest.csproj
```

---

## Running Locally

```bash
# From backend/PantioAPI/
dotnet run

# OpenAPI docs (Development only)
GET http://localhost:5082/openapi/v1.json

# Docker
docker build -t pantio-api .
docker run -p 8080:8080 pantio-api
```

Connection string is read from `appsettings.json` → `ConnectionStrings:DefaultConnection` (PostgreSQL).

---

## Logging

Logging uses the built-in `Microsoft.Extensions.Logging` (`ILogger<T>`) injected into services via the primary constructor. No extra packages required — ASP.NET Core registers the provider automatically.

### Where logging lives

Logging is done in **services only**. Controllers are too thin to have meaningful context; repositories are too low-level and would produce noise without business meaning.

### Log levels

| Level | When to use |
|---|---|
| `Debug` | High-frequency read operations (fetching collections) — off by default in Production |
| `Information` | Significant state changes: resource created, deleted |
| `Warning` | Expected-but-notable conditions: resource not found on delete, lookup miss |
| `Error` | Unhandled exceptions (not yet wired — use a middleware or `try/catch` in the service) |

### PII policy — what must never be logged

The following are considered personal data under GDPR and must not appear in any log message:

- Email addresses
- Phone numbers
- Display names or any other user-provided text
- Any `string` field sourced from a DTO or entity

**Safe to log:** GUIDs (opaque internal IDs), counts, enum values, timestamps.

### Example

```csharp
// ✅ Safe — opaque IDs only
logger.LogInformation("Inventory {InventoryId} created for user {UserId}", created.Id, userId);

// ❌ Never — contains user-provided text
logger.LogInformation("Inventory '{Name}' created for {Email}", dto.Name, user.Email);
```

---

## Key Domain Rules

- No `Product` table. Open Food Facts (OFF) is the source of truth. `ProductCache` is a thin, evictable per-user cache.
- `InventoryItem` snapshots `product_name`, `quantity`, and `quantity_unit` at add time — survives cache eviction and OFF outages.
- `ExpiryDate.estimated_expiry` = `added_at + ProductCategory.default_shelf_life_days`.
- `ReceiptLine.item_type` `01` = product, `02` = deposit/pant — only `01` lines are processed to inventory.
- `ReceiptLine.processed_to_inventory` prevents duplicate `InventoryItem` creation on re-poll.
- Missing recipe ingredients (`RecipeEntry.inventory_item_id IS NULL`) are resolved via case-insensitive name matching when a new `InventoryItem` is created.
- All monetary values are in **DKK**, stored as `FLOAT`.
