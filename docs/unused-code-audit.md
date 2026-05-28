# Unused & Redundant Code Audit

> Generated: 2026-05-22 — Read-only report, no changes made.

---

## 1. Unused Entity Properties & Fields

### `UserProfile` Entity — Full Dead Code
- **Files:**
  - `backend/PantioClassLibrary/Entities/UserProfile.cs` (entire file)
  - `backend/PantioRepository/EntityFramework/PantioDbContext.cs` (line 10 — DbSet registration)
- **Issue:** Entity with properties `DisplayName`, `HouseholdSize`, `Locale`, `NotificationPrefs` is fully defined and migrated to the database but is never loaded, read, or written anywhere in the API.
- **Severity:** High — creates a dead DB table with migration overhead.

### `GigyaSessionToken` on `StoreConnection`
- **File:** `backend/PantioClassLibrary/Entities/StoreConnection.cs` (line 21)
- **Issue:** Legacy token field from the old Gigya auth system, now replaced by Auth0. The field is explicitly set to `null` during disconnect (StoreConnectionService line 301) and is only referenced in the legacy `StoreConnectionTokenMigrationService`.
- **Severity:** High — leftover from a replaced system.

### `PhoneNumber` on `User`
- **File:** `backend/PantioClassLibrary/Entities/User.cs` (line 23)
- **Issue:** Property is defined but never read or written in any service, controller, or DTO.
- **Severity:** Medium — silent dead weight in the domain model.

---

## 2. Unused Imports

### `System.Security.Claims` in `UserController`
- **File:** `backend/PantioAPI/Controllers/UserController.cs` (line 1)
- **Issue:** Claims are accessed via `User.FindFirst()` without directly referencing `ClaimTypes`, making the `using` statement technically unused.
- **Severity:** Low.

### `Microsoft.AspNetCore.Authorization` in `HealthController`
- **File:** `backend/PantioAPI/Controllers/HealthController.cs` (line 1)
- **Issue:** `[AllowAnonymous]` resolves without needing this namespace explicitly imported.
- **Severity:** Low.

---

## 3. Duplicate / Redundant Logic

### Product Cache Lookup (3-tier pattern)
- **Files:**
  - `backend/PantioAPI/Controllers/ProductsController.cs` (lines 24–52)
  - `backend/PantioAPI/Services/InventoryItemService.cs` (lines 137–162)
- **Issue:** Both locations implement the same 3-tier cache lookup independently:
  1. Check Redis cache
  2. Check database cache
  3. Fall back to OpenFoodFacts API
- **Recommendation:** Extract to a shared `IProductDataService` to avoid divergence.
- **Severity:** Medium — maintenance burden if the lookup logic ever changes.

### Unit Compatibility Validation
- **Files:**
  - `backend/PantioAPI/Services/ShoppingListService.cs` (lines 55–73)
  - `backend/PantioClassLibrary/Utilities/QuantityUnitConverter.cs`
- **Issue:** Unit compatibility checking is duplicated inside `ShoppingListService` instead of delegating to the existing `QuantityUnitConverter` utility.
- **Severity:** Medium.

---

## 4. Unused DTO Fields

### `Id` on `NutritionFactsDto`
- **File:** `backend/PantioClassLibrary/DTO/NutritionFactsDto.cs` (line 4)
- **Issue:** The `Id` field is included in the DTO but is never surfaced to API clients — nutrition facts are always returned as nested objects within inventory item responses.
- **Severity:** Low.

---

## Summary

| # | Item | File | Severity |
|---|------|------|----------|
| 1 | `UserProfile` entity entirely unused | `Entities/UserProfile.cs`, `PantioDbContext.cs:10` | High |
| 2 | `GigyaSessionToken` (legacy Gigya) | `Entities/StoreConnection.cs:21` | High |
| 3 | `PhoneNumber` never used | `Entities/User.cs:23` | Medium |
| 4 | Duplicate product cache lookup | `ProductsController.cs:24–52`, `InventoryItemService.cs:137–162` | Medium |
| 5 | Duplicate unit validation logic | `ShoppingListService.cs:55–73` | Medium |
| 6 | Unused `Id` on `NutritionFactsDto` | `DTO/NutritionFactsDto.cs:4` | Low |
| 7 | Unused import `System.Security.Claims` | `Controllers/UserController.cs:1` | Low |
| 8 | Unused import `Authorization` | `Controllers/HealthController.cs:1` | Low |

### Recommended Cleanup Order
1. Remove `UserProfile` entity + DbSet + run a migration to drop the table.
2. Remove `GigyaSessionToken` field + any migration that references it.
3. Remove `PhoneNumber` from `User`.
4. Extract shared product cache lookup into a dedicated service.
5. Unify unit validation through `QuantityUnitConverter`.
6. Remove `Id` from `NutritionFactsDto` if confirmed unused by clients.
7. Clean up the two unused `using` directives.
