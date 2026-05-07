# Indkøbsliste Flow

Dette dokument beskriver det komplette flow for indkøbsliste-funktionen i Pantio — både manuel oprettelse og automatisk generering fra en opskrift.

---

## Overblik

Der er to flows:

1. **Fra opskrift**: Brugeren vælger en opskrift, systemet gennemgår det aktuelle lager og tilføjer kun de manglende ingredienser til en ny indkøbsliste (som rene tekststrenge — ikke linket til lageravarer).
2. **Manuel styring**: Brugeren kan oprette indkøbslister manuelt, tilføje og slette varer samt markere dem som købt.

---

## Entiteter og tabeller

| Entitet | Tabel | Fil |
|---------|-------|-----|
| `ShoppingList` | `shopping_lists` | `PantioClassLibrary/Entities/ShoppingList.cs` |
| `ShoppingListItem` | `shopping_list_items` | `PantioClassLibrary/Entities/ShoppingListItem.cs` |

Tabellerne blev oprettet i den initielle migration (`20260430101748_InitialCreate.cs`) og kræver ingen ny migration.

### `ShoppingList`
| Kolonne | Type | Beskrivelse |
|---------|------|-------------|
| `id` | uuid | Primærnøgle |
| `user_id` | uuid | FK til `users` |
| `name` | text | Navn på listen |
| `created_at` | timestamp | Oprettelsestidspunkt |
| `updated_at` | timestamp | Sidst opdateret |

### `ShoppingListItem`
| Kolonne | Type | Beskrivelse |
|---------|------|-------------|
| `id` | uuid | Primærnøgle |
| `shopping_list_id` | uuid | FK til `shopping_lists` (cascade delete) |
| `name` | text | Navn på varen (fritekst — ikke linket til lager) |
| `quantity` | real? | Mængde (valgfri) |
| `measuring_unit` | text? | Enhed (g, stk, L osv.) |
| `is_checked` | bool | Om varen er markeret som købt |

---

## Flow 1 — Fra opskrift

### Endpoint
```
POST /api/users/{userId}/shopping-lists/from-recipe
Body: { "recipeId": "guid", "inventoryId": "guid", "name": "string?" }
```

### Trin-for-trin

**Trin 1 — Hent opskrift**
`PantioAPI/Services/ShoppingListService.cs` (`CreateFromRecipeAsync`) ->
`PantioRepository/EntityFramework/Repositories/RecipeRepository.cs` (`GetByIdWithEntriesAsync`)

Opskriften hentes med alle dens ingredienser (`RecipeEntry`-entiteter). Returnerer `null` og `404 Not Found` hvis opskriften ikke eksisterer.

---

**Trin 2 — Hent aktuelle lageravarer**
`PantioAPI/Services/ShoppingListService.cs` ->
`PantioRepository/EntityFramework/Repositories/InventoryItemRepository.cs` (`GetByInventoryIdAsync`)

Alle lageravarer i det angivne lager hentes med et enkelt `WHERE inventory_id = ...` kald.

---

**Trin 3 — Find manglende ingredienser**
`PantioAPI/Services/ShoppingListService.cs` ->
`PantioAPI/Services/RecipeIngredientMatcher.cs` (`FindBestMatch`)

For hver `RecipeEntry` køres `RecipeIngredientMatcher.FindBestMatch` mod de aktuelle lageravarer:
1. Eksakt match (case-insensitivt, trimmet)
2. Substring-match i begge retninger

Ingredienser **uden match** = ikke på lager → tilføjes som `ShoppingListItem`. Ingredienser med match springes over.

---

**Trin 4 — Opret indkøbsliste med manglende varer**
`PantioAPI/Services/ShoppingListService.cs` ->
`PantioRepository/EntityFramework/Repositories/ShoppingListRepository.cs` (`CreateAsync`)

Bygger `ShoppingList` med:
- `Name = dto.Name ?? recipe.Name` (bruger opskriftens navn hvis intet navn angives)
- Én `ShoppingListItem` per manglende ingrediens med `Name`, `Quantity` og `MeasuringUnit` fra opskriften
- `IsChecked = false` på alle nye varer

Gemmes til `shopping_lists`- og `shopping_list_items`-tabellerne via EF Core.

---

**Trin 5 — Map til DTO og returner**

Returneres som `201 Created` med `ShoppingListDto` indeholdende alle varer.

---

## Flow 2 — Manuel styring

### Opret liste
```
POST /api/users/{userId}/shopping-lists
Body: { "name": "string" }
→ 201 Created
```

### Hent alle lister
```
GET /api/users/{userId}/shopping-lists
→ 200 OK  [ ShoppingListDto, ... ]
```

### Hent enkelt liste (med varer)
```
GET /api/users/{userId}/shopping-lists/{listId}
→ 200 OK  ShoppingListDto
```

### Slet liste (cascade sletter varer)
```
DELETE /api/users/{userId}/shopping-lists/{listId}
→ 204 No Content
```

### Tilføj vare manuelt
```
POST /api/users/{userId}/shopping-lists/{listId}/items
Body: { "name": "string", "quantity": float?, "measuringUnit": "string?" }
→ 201 Created
```

### Slet vare
```
DELETE /api/users/{userId}/shopping-lists/{listId}/items/{itemId}
→ 204 No Content
```

### Markér/fjern markering
```
PATCH /api/users/{userId}/shopping-lists/{listId}/items/{itemId}/toggle
→ 200 OK  ShoppingListItemDto  (isChecked vendt)
```

---

## Arkitektur

```
Controller  ->  Service  ->  Repository  ->  DbContext  ->  PostgreSQL
```

| Lag | Fil |
|-----|-----|
| Controller | `PantioAPI/Controllers/ShoppingListController.cs` |
| Service interface | `PantioClassLibrary/Interfaces/Services/IShoppingListService.cs` |
| Service impl. | `PantioAPI/Services/ShoppingListService.cs` |
| Repository interface | `PantioClassLibrary/Interfaces/Repository/IShoppingListRepository.cs` |
| Repository impl. | `PantioRepository/EntityFramework/Repositories/ShoppingListRepository.cs` |
| Matcher (delt) | `PantioAPI/Services/RecipeIngredientMatcher.cs` |

---

## DTOs

| DTO | Retning | Felter |
|-----|---------|--------|
| `CreateShoppingListDto` | Request | `name` |
| `AddShoppingListItemDto` | Request | `name`, `quantity?`, `measuringUnit?` |
| `AddFromRecipeDto` | Request | `recipeId`, `inventoryId`, `name?` |
| `ShoppingListDto` | Response | `id`, `userId`, `name`, `createdAt`, `items[]` |
| `ShoppingListItemDto` | Response | `id`, `name`, `quantity?`, `measuringUnit?`, `isChecked` |

---

## DI-registrering

`PantioAPI/Program.cs`

```csharp
builder.Services.AddScoped<IShoppingListRepository, ShoppingListRepository>();
builder.Services.AddScoped<IShoppingListService, ShoppingListService>();
```

---

## Vigtige designbeslutninger

**Indkøbsvarer er ikke linket til lageravarer.**
`ShoppingListItem.Name` er en ren tekststreng. Dette er bevidst — brugeren skal frit kunne redigere varernes navne og indkøbslisten behøver ikke kendes til lageret.

**`RecipeIngredientMatcher` bruges til at bestemme hvad der mangler.**
Den samme matcher der bruges til opskriftsforslag genbruges her. Matchet sker mod det *aktuelle* lager (ikke de gemte `InventoryItemId`-links på opskriften), så listen altid afspejler den nuværende beholdning.

**Cascade delete.**
Når en `ShoppingList` slettes, slettes alle dens `ShoppingListItem`-rækker automatisk via FK-cascade.
