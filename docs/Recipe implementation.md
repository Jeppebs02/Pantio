# Recipe Feature Implementation

This document explains how the recipe feature works in Pantio — from database schema through AI generation to inventory deduction — and walks through a full end-to-end flow example.

---

## Table of Contents

1. [Overview](#overview)
2. [Database Schema](#database-schema)
3. [Backend Architecture](#backend-architecture)
   - [Entities](#entities)
   - [DTOs](#dtos)
   - [Repository](#repository)
   - [Services](#services)
   - [Controllers](#controllers)
4. [Frontend Architecture](#frontend-architecture)
   - [Types](#types)
   - [API Service](#api-service)
   - [Pinia Store](#pinia-store)
   - [Views](#views)
5. [Key Design Decisions](#key-design-decisions)
6. [Full Flow Example](#full-flow-example)

---

## Overview

The recipe feature lets users generate AI-powered recipe suggestions based on items in their inventory. The flow is:

1. User selects inventory items in the **Generate** tab
2. Backend calls **Google Gemini 2.5 Flash** with a Danish-language prompt
3. Gemini returns 3 structured recipe suggestions, persisted to the database
4. User browses suggestions, opens a detail view, and can **complete** a recipe
5. On completion, ingredient quantities are **deducted from inventory**; the other two suggestions remain so the user can browse them later

---

## Database Schema

Two tables power the feature.

```sql
recipes (
  id                UUID        PRIMARY KEY,
  user_id           UUID        NOT NULL REFERENCES users(id) ON DELETE CASCADE,
  name              TEXT        NOT NULL,
  description       TEXT,
  instructions      TEXT,
  portions          FLOAT       NOT NULL,
  completed_at      TIMESTAMPTZ,
  created_at        TIMESTAMPTZ NOT NULL,
  suggestion_batch_id UUID,        -- groups the 3 recipes generated in one call
  is_saved          BOOLEAN     NOT NULL DEFAULT false
)

recipe_entries (
  id                UUID        PRIMARY KEY,
  recipe_id         UUID        NOT NULL REFERENCES recipes(id) ON DELETE CASCADE,
  inventory_item_id UUID        REFERENCES inventory_items(id) ON DELETE SET NULL,
  product_name      TEXT        NOT NULL,
  quantity          DECIMAL     NOT NULL,
  measuring_unit    TEXT
)
```

`suggestion_batch_id` is the most important field: all 3 suggestions from a single Gemini call share the same batch ID. All three persist after completion so the user can browse the full batch.

`inventory_item_id` in `recipe_entries` is nullable. It is set by `RecipeIngredientMatcher` at creation time and can be refreshed later via the `/link` endpoint.

---

## Backend Architecture

### Entities

**`PantioClassLibrary/Entities/Recipe.cs`**

```csharp
[Table("recipes")]
public class Recipe
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string? Instructions { get; set; }
    public float Portions { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid? SuggestionBatchId { get; set; }
    public bool IsSaved { get; set; }

    public ICollection<RecipeEntry> Entries { get; set; } = [];
}
```

**`PantioClassLibrary/Entities/RecipeEntry.cs`** — represents a single ingredient line in a recipe:

- `InventoryItemId` is nullable; when populated, the completion flow can deduct quantities directly
- On recipe deletion: cascade. On inventory item deletion: SET NULL (so recipes survive item removal)

---

### DTOs

**`RecipeSuggestionDto`** — returned to the frontend for both suggestions and saved recipes:

```csharp
public record RecipeSuggestionIngredientDto(
    string ProductName,
    decimal Quantity,
    string? MeasuringUnit,
    Guid? InventoryItemId,
    bool InInventory          // computed: InventoryItemId != null
);

public record RecipeSuggestionDto(
    Guid Id,
    string Name,
    string Description,
    string Instructions,
    float Portions,
    IEnumerable<RecipeSuggestionIngredientDto> Ingredients,
    bool IsSaved
);
```

**`RecipeListItemDto`** — lightweight version for list views:

```csharp
public record RecipeListItemDto(
    Guid Id,
    string Name,
    string? Description,
    float Portions,
    int IngredientCount,
    IEnumerable<string> IngredientNames,
    bool IsSaved
);
```

**`RecipeSuggestionRequestDto`** — request body for generating suggestions:

```csharp
public record RecipeSuggestionRequestDto(IEnumerable<Guid> InventoryItemIds);
```

---

### Repository

**`PantioRepository/EntityFramework/Repositories/RecipeRepository.cs`**

Key methods:

| Method | Purpose |
|--------|---------|
| `CreateAsync(recipe)` | Persists a new recipe with all entries |
| `GetByIdWithEntriesAsync(id)` | Loads recipe + all entries (eager load) |
| `GetBySuggestionBatchAsync(batchId)` | Loads all suggestions sharing a batch ID |
| `GetByUserFilteredAsync(userId, search, ingredientNames)` | List view with title search and ingredient filter |
| `SetCompletedAsync(id)` | Stamps `CompletedAt = UtcNow` |
| `DeleteAsync(id)` | Hard delete (cascades to entries) |
| `ClearInventoryLinksAsync(recipeId)` | Sets all `InventoryItemId` to null (makes recipe reusable) |
| `UpdateEntryLinksAsync(recipeId, links)` | Batch-updates ingredient → inventory item mappings |
| `ToggleSavedAsync(recipeId)` | Flips `IsSaved` and returns new value |

---

### Services

#### `IUnitOfWork` / `EFUnitOfWork`

**`PantioClassLibrary/Interfaces/IUnitOfWork.cs`** and **`PantioRepository/EFUnitOfWork.cs`**

Wraps EF Core's transaction handling so that `RecipeService` can execute a set of operations atomically. The interface uses a callback pattern rather than explicit Begin/Commit/Rollback because Npgsql's `EnableRetryOnFailure` execution strategy requires the entire transactional unit — including the `BeginTransaction` call — to be wrapped in `CreateExecutionStrategy().ExecuteAsync(...)`.

```csharp
public interface IUnitOfWork
{
    Task ExecuteInTransactionAsync(Func<Task> operation, CancellationToken ct = default);
}
```

```csharp
public class EFUnitOfWork(PantioDbContext db) : IUnitOfWork
{
    public Task ExecuteInTransactionAsync(Func<Task> operation, CancellationToken ct = default)
    {
        var strategy = db.Database.CreateExecutionStrategy();
        return strategy.ExecuteAsync(async () =>
        {
            await using var tx = await db.Database.BeginTransactionAsync(ct);
            try
            {
                await operation();
                await tx.CommitAsync(ct);
            }
            catch
            {
                await tx.RollbackAsync(ct);
                throw;
            }
        });
    }
}
```

Registered as `AddScoped<IUnitOfWork, EFUnitOfWork>()` in `Program.cs`.

---

#### `RecipeIngredientMatcher`

**`PantioAPI/Services/RecipeIngredientMatcher.cs`**

Simple static utility used in two places: during suggestion creation and during re-linking.

```csharp
internal static class RecipeIngredientMatcher
{
    public static InventoryItem? FindBestMatch(string name, List<InventoryItem> items)
    {
        var needle = Normalize(name);
        var needleWords = Words(needle);

        return items.FirstOrDefault(i => Normalize(i.ProductName) == needle)
            ?? items.FirstOrDefault(i =>
            {
                var hayWords = Words(Normalize(i.ProductName));
                // needleWords ⊆ hayWords only when the haystack has at most one extra word,
                // so "salt" does not match "salt and vinegar chips" but "oksekød" still
                // matches "hakket oksekød".
                if (needleWords.IsSubsetOf(hayWords) && hayWords.Count <= needleWords.Count + 1)
                    return true;
                return hayWords.IsSubsetOf(needleWords);
            });
    }

    private static HashSet<string> Words(string s) =>
        s.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet();

    private static string Normalize(string s)
    {
        var lower = s.Trim().ToLowerInvariant().Replace("-", " ").Replace("_", " ");
        var decomposed = lower.Normalize(NormalizationForm.FormD);
        var stripped = new string(decomposed
            .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            .ToArray());
        return stripped.Normalize(NormalizationForm.FormC);
    }
}
```

Priority: exact match → word-token subset match. Case-insensitive, diacritics stripped (e.g. `jalapeño` → `jalapeno`), dashes and underscores treated as spaces.

The `needleWords ⊆ hayWords` direction (ingredient is more general than the product) is capped at haystack size ≤ needle size + 1. This allows `{"oksekød"}` to match `{"hakket", "oksekød"}` (one qualifier added) while blocking `{"salt"}` from matching `{"salt", "and", "vinegar", "chips"}` (a multi-word branded product). The reverse direction `hayWords ⊆ needleWords` has no size cap because a short inventory item name (e.g. `{"oksekød"}`) should always match a more descriptive Gemini ingredient (e.g. `{"hakket", "oksekød"}`).

Single-word names like `løg` do **not** match compound words like `hvidløg` because word-splitting is on whitespace, not substrings.

Note: ø and æ do not decompose under NFD and are preserved as-is through normalization. Only å (which decomposes to a + combining ring above) is folded to `a`.

---

#### `RecipeSuggestionService`

**`PantioAPI/Services/RecipeSuggestionService.cs`**

Responsible for calling Gemini and persisting the results.

**Step 1 — Build the prompt**

The prompt uses Unix line endings (`\n` only — not `\r\n`) throughout. Windows-style `\r\n` caused Gemini to emit carriage-return characters (U+000D) as substitutes for Danish letters in its JSON output. The first rule explicitly instructs Gemini to always write ø, æ, å correctly.

```csharp
private static string BuildPrompt(List<InventoryItem> items)
{
    var sb = new StringBuilder();
    sb.Append("Du er en hjælpsom opskriftsassistent. Foreslå præcis 3 opskrifter på dansk. Følg regler nøje\n");
    sb.Append('\n');
    sb.Append("Regler:\n");
    sb.Append("- VIGTIGT: Skriv ALTID korrekte danske bogstaver: ø, æ, å. Eksempler: løg, oksekød, mælk, hvidløg, grønne bønner, rødkål, æg, smør, grød. Disse bogstaver er gyldige JSON-tegn og MÅ ALDRIG erstattes med mellemrum eller andre tegn.\n");
    // ... regler: diverse opskrifter, enheder (kg/g/mg/l/ml/dl/cl/stk),
    //     nummererede trin på separate linjer, heltal til portioner ...
    sb.Append("Tilgængelige ingredienser:\n");
    foreach (var item in items)
    {
        var unit = item.QuantityUnit?.ToString() ?? "stk";
        sb.Append($"- {item.ProductName}: {item.Quantity} {unit}\n");
    }
    sb.Append('\n');
    sb.Append("Svar med KUN gyldig JSON — ingen markdown, ingen forklaring.\n");
    return sb.ToString();
}
```

**Step 2 — Call Gemini with a response schema**

The schema is passed via `responseSchema` in `generationConfig`, forcing Gemini to emit valid JSON directly. `JavaScriptEncoder.UnsafeRelaxedJsonEscaping` is used when serializing the request so Danish characters in the prompt are sent as literal UTF-8, not escaped. `thinkingBudget = 0` disables extended thinking to reduce latency.

```csharp
var responseSchema = new {
    type = "object",
    properties = new {
        recipes = new {
            type = "array",
            items = new {
                type = "object",
                properties = new {
                    name         = new { type = "string" },
                    description  = new { type = "string" },
                    instructions = new { type = "string" },
                    portions     = new { type = "number" },
                    ingredients  = new {
                        type = "array",
                        items = new {
                            type = "object",
                            properties = new {
                                productName = new { type = "string" },
                                quantity    = new { type = "number" },
                                unit        = new { type = "string",
                                              @enum = new[] { "kg","g","mg","l","ml","dl","cl","stk" } }
                            }
                        }
                    }
                }
            }
        }
    }
};
```

**Step 3 — Persist with a shared batch ID**

```csharp
var batchId = Guid.NewGuid();

foreach (var geminiRecipe in geminiBody.Recipes)
{
    var recipe = new Recipe
    {
        Id = Guid.NewGuid(),
        UserId = userId,
        SuggestionBatchId = batchId,         // all 3 share this
        Entries = geminiRecipe.Ingredients.Select(ing => new RecipeEntry
        {
            ProductName = ing.ProductName,
            Quantity = ing.Quantity ?? 0,
            MeasuringUnit = ing.Unit,
            InventoryItemId = RecipeIngredientMatcher.FindBestMatch(ing.ProductName, items)?.Id
        }).ToList()
    };
    await recipeRepository.CreateAsync(recipe, ct);
}
```

---

#### `RecipeService`

**`PantioAPI/Services/RecipeService.cs`**

**`CompleteAsync`** — called when the user marks a recipe as done. The DB mutations run inside `ExecuteInTransactionAsync` so they are atomic and retryable. Cache invalidation runs after the transaction commits — it is intentionally outside the transaction scope so a cache failure cannot roll back a successful DB completion.

```csharp
public async Task<bool> CompleteAsync(Guid recipeId, CancellationToken ct = default)
{
    var recipe = await recipeRepository.GetByIdWithEntriesAsync(recipeId, ct);
    if (recipe is null) return false;

    var linkedEntries = recipe.Entries
        .Where(e => e.InventoryItemId.HasValue)
        .Select(e => (e.InventoryItemId!.Value, e.Quantity, e.MeasuringUnit))
        .ToList();

    var affectedInventoryIds = new HashSet<Guid>();

    await unitOfWork.ExecuteInTransactionAsync(async () =>
    {
        // Clear links so the recipe becomes a reusable template
        await recipeRepository.ClearInventoryLinksAsync(recipeId, ct);

        foreach (var (itemId, qty, entryUnit) in linkedEntries)
        {
            var item = await inventoryItemRepository.GetByIdAsync(itemId, ct);
            if (item is null) continue;

            affectedInventoryIds.Add(item.InventoryId);

            var effectiveQty = qty;
            if (item.QuantityUnit.HasValue && entryUnit is not null
                && Enum.TryParse<QuantityUnit>(entryUnit, ignoreCase: true, out var parsedUnit)
                && QuantityUnitConverter.AreSameCategory(item.QuantityUnit.Value, parsedUnit))
            {
                effectiveQty = QuantityUnitConverter.Convert(qty, parsedUnit, item.QuantityUnit.Value);
            }

            var newQty = item.Quantity - effectiveQty;
            if (newQty <= 0)
                await inventoryItemRepository.DeleteAsync(item.Id, ct);   // fully consumed
            else
                await inventoryItemRepository.UpdateAsync(item.Id, /* newQty */, ct);
        }

        await recipeRepository.SetCompletedAsync(recipeId, ct);
    }, ct);

    // Outside the transaction: failure here does not roll back the completion
    foreach (var inventoryId in affectedInventoryIds)
        await inventoryItemCacheService.InvalidateAsync(inventoryId, ct);

    return true;
}
```

**`LinkToInventoryAsync`** — re-matches ingredient names against a given set of inventories:

```csharp
public async Task<RecipeSuggestionDto?> LinkToInventoryAsync(
    Guid recipeId, IEnumerable<Guid> inventoryIds, CancellationToken ct = default)
{
    var allItems = new List<InventoryItem>();
    foreach (var inventoryId in inventoryIds)
        allItems.AddRange(await inventoryItemRepository.GetByInventoryIdAsync(inventoryId, ct));

    var links = recipe.Entries.ToDictionary(
        e => e.Id,
        e => RecipeIngredientMatcher.FindBestMatch(e.ProductName, allItems)?.Id
    );

    await recipeRepository.UpdateEntryLinksAsync(recipeId, links, ct);
    return RecipeSuggestionMapper.ToDto(await recipeRepository.GetByIdWithEntriesAsync(recipeId, ct));
}
```

---

### Controllers

**`PantioAPI/Controllers/RecipeController.cs`** and **`RecipeSuggestionController.cs`**

| Method | Route | Purpose |
|--------|-------|---------|
| `POST` | `/api/users/{userId}/recipe-suggestions` | Generate 3 AI suggestions |
| `GET` | `/api/users/{userId}/recipes` | List recipes (search + ingredient filter) |
| `GET` | `/api/users/{userId}/recipes/{recipeId}` | Get single recipe with entries |
| `POST` | `/api/users/{userId}/recipes/{recipeId}/save` | Toggle `IsSaved` |
| `POST` | `/api/recipes/{recipeId}/complete` | Deduct inventory and mark recipe complete |
| `POST` | `/api/recipes/{recipeId}/link` | Re-link ingredients to inventory items |

```csharp
[HttpPost("api/recipes/{recipeId:guid}/complete")]
public async Task<IActionResult> Complete(Guid recipeId, CancellationToken ct)
{
    var success = await service.CompleteAsync(recipeId, ct);
    if (!success) return NotFound();
    return NoContent();
}

[HttpPost("api/recipes/{recipeId:guid}/link")]
public async Task<IActionResult> Link(Guid recipeId, RecipeLinkRequestDto request, CancellationToken ct)
{
    var result = await service.LinkToInventoryAsync(recipeId, request.InventoryIds, ct);
    if (result is null) return NotFound();
    return Ok(result);
}
```

**Exception handling** — `Program.cs` registers a global exception handler immediately after `app.UseCors()`. This guarantees that `Access-Control-Allow-Origin` is present even on 500 responses, so the browser never misreports a server error as a CORS failure:

```csharp
app.UseCors();
app.UseExceptionHandler(errorApp =>
    errorApp.Run(async context =>
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsync("{\"status\":500,\"title\":\"Internal Server Error\"}");
    }));
app.UseAuthentication();
```

---

## Frontend Architecture

### Types

**`frontend/PantioApp/src/services/types.ts`**

```typescript
export type RecipeIngredientDto = {
  productName: string
  quantity: number
  measuringUnit: string | null
  inventoryItemId: string | null
  inInventory: boolean
}

export type RecipeDto = {
  id: string
  name: string
  description: string
  instructions: string
  portions: number
  ingredients: RecipeIngredientDto[]
  isSaved: boolean
}

export type RecipeListItemDto = {
  id: string
  name: string
  description: string | null
  portions: number
  ingredientCount: number
  ingredientNames: string[]
  isSaved: boolean
}
```

---

### API Service

**`frontend/PantioApp/src/services/recipes.ts`**

Thin wrappers around `apiFetch`:

```typescript
export function getRecipeSuggestions(
  userId: string,
  inventoryItemIds: string[],
): Promise<RecipeSuggestionsDto> {
  return apiFetch(`/api/users/${userId}/recipe-suggestions`, {
    method: 'POST',
    body: JSON.stringify({ inventoryItemIds }),
  })
}

export function completeRecipe(recipeId: string): Promise<void> {
  return apiFetch(`/api/recipes/${recipeId}/complete`, { method: 'POST' })
}

export function linkRecipe(recipeId: string, inventoryIds: string[]): Promise<RecipeDto> {
  return apiFetch(`/api/recipes/${recipeId}/link`, {
    method: 'POST',
    body: JSON.stringify({ inventoryIds }),
  })
}

export function listRecipes(
  userId: string,
  search?: string,
  ingredients?: string[],
): Promise<RecipeListItemDto[]> {
  const params = new URLSearchParams()
  if (search) params.set('search', search)
  ingredients?.forEach((i) => params.append('ingredient', i))
  const qs = params.toString()
  return apiFetch(`/api/users/${userId}/recipes${qs ? `?${qs}` : ''}`)
}

export function toggleSave(userId: string, recipeId: string): Promise<{ isSaved: boolean }> {
  return apiFetch(`/api/users/${userId}/recipes/${recipeId}/save`, { method: 'POST' })
}
```

---

### Pinia Store

**`frontend/PantioApp/src/stores/recipes.ts`**

```typescript
export const useRecipesStore = defineStore('recipes', () => {
  const suggestions = ref<RecipeDto[]>([])       // current AI batch
  const currentRecipe = ref<RecipeDto | null>(null)
  const isLoading = ref(false)
  const recipeList = ref<RecipeListItemDto[]>([]) // browse/saved tabs
  const isLoadingList = ref(false)

  async function getSuggestions(inventoryItemIds: string[]) {
    isLoading.value = true
    suggestions.value = []
    const result = await recipesService.getRecipeSuggestions(userId, inventoryItemIds)
    suggestions.value = result.suggestions
    isLoading.value = false
  }

  async function completeRecipe(recipeId: string) {
    await recipesService.completeRecipe(recipeId)
    suggestions.value = suggestions.value.filter((r) => r.id !== recipeId)
    if (currentRecipe.value?.id === recipeId) currentRecipe.value = null
  }

  async function toggleSave(recipeId: string) {
    const { isSaved } = await recipesService.toggleSave(userId, recipeId)
    // Update all three locations: recipeList, currentRecipe, suggestions
    const listItem = recipeList.value.find((r) => r.id === recipeId)
    if (listItem) listItem.isSaved = isSaved
    if (currentRecipe.value?.id === recipeId) currentRecipe.value.isSaved = isSaved
    const suggestion = suggestions.value.find((r) => r.id === recipeId)
    if (suggestion) suggestion.isSaved = isSaved
    return isSaved
  }

  // ... linkRecipe, fetchRecipeById, fetchRecipeList
})
```

The store holds suggestions in memory between the generate tab and the detail view. When navigating to a recipe detail, the view first checks `suggestions` before making a network request.

**`frontend/PantioApp/src/stores/inventory.ts`** — relevant recipe-facing behavior:

`deleteInventory` removes the deleted inventory's entry from `itemsByInventory` immediately so the Generate tab no longer shows its items without requiring a page reload:

```typescript
async function deleteInventory(id: string) {
  await inventoryService.deleteInventory(userId(), id)
  inventories.value = inventories.value.filter((i) => i.id !== id)
  delete itemsByInventory.value[id]
}
```

`fetchAllItemSummaries` replaces the entire `itemsByInventory` map rather than merging into it, so stale keys from previously deleted inventories never survive a refresh:

```typescript
async function fetchAllItemSummaries() {
  const fresh: Record<string, InventoryItemDto[]> = {}
  await Promise.all(
    inventories.value.map(async (inv) => {
      const result = await inventoryService.getInventoryItems(inv.id)
      fresh[inv.id] = result
    }),
  )
  itemsByInventory.value = fresh
}
```

---

### Views

#### `RecipeMainView.vue` — Three-tab layout

| Tab | Content |
|-----|---------|
| **Saved** | Recipes where `isSaved = true` |
| **Browse** | All user recipes; debounced title search + ingredient tag filter |
| **Generate** | Inventory item picker → Gemini call → suggestion cards |

The Generate tab derives its item list from `itemsByInventory` across all current inventories:

```typescript
const inventoryItems = computed<InventoryItemDto[]>(() => {
  const all = Object.values(invStore.itemsByInventory).flat()
  return [...all].sort((a, b) => {
    const dA = a.expiryDate ? new Date(a.expiryDate.estimatedExpiry).getTime() : Infinity
    const dB = b.expiryDate ? new Date(b.expiryDate.estimatedExpiry).getTime() : Infinity
    return dA - dB
  })
})
```

Items are sorted by expiry date (soonest first) with expiry badges. `onMounted` calls `fetchAllItemSummaries()` every time the view loads, which rebuilds `itemsByInventory` from scratch and removes any stale entries from deleted inventories.

#### `RecipeDetailView.vue` — Recipe detail

**Ingredient separation** — at render time the view determines which ingredients are in inventory by scanning `invStore.itemsByInventory` directly:

```typescript
function isInInventory(productName: string): boolean {
  const needle = normalizeName(productName)
  const allItems = Object.values(invStore.itemsByInventory).flat()
  return allItems.some((item) => {
    const hay = normalizeName(item.productName)
    return hay === needle || hay.includes(needle) || needle.includes(hay)
  })
}

const haveIngredients = computed(
  () => recipe.value?.ingredients.filter((i) => isInInventory(i.productName)) ?? [],
)
const needIngredients = computed(
  () => recipe.value?.ingredients.filter((i) => !isInInventory(i.productName)) ?? [],
)
```

**Portion scaling** — quantities scale with the user's portion stepper without any server round-trip:

```typescript
function scaleQty(qty: number): string {
  const scaled = qty * (selectedPortions.value / (recipe.value?.portions ?? 1))
  return parseFloat(scaled.toFixed(2)).toString()
}
```

**Completing a recipe** — links first, then completes:

```typescript
async function completeRecipe() {
  if (allInventoryIds.value.length > 0)
    await recipeStore.linkRecipe(recipe.value.id, allInventoryIds.value)  // re-link to current inventory state
  await recipeStore.completeRecipe(recipe.value.id)
  router.replace({ name: 'inventory', params: { id: primaryInventoryId.value } })
}
```

Calling `linkRecipe` immediately before `completeRecipe` ensures the ingredient-to-inventory-item mapping reflects the current inventory state, not stale links from when the recipe was originally generated.

---

## Key Design Decisions

**Suggestion batching** — Gemini always returns 3 recipes grouped by `SuggestionBatchId`. All three are kept after completion so the user can browse the full set of suggestions.

**Lazy linking** — Ingredient-to-inventory-item links are set optimistically at creation time using string matching, then refreshed just before completion. This avoids stale references if inventory changes between suggestion generation and actual cooking.

**Recipe as reusable template** — on completion, `ClearInventoryLinksAsync` removes all inventory links before deducting quantities. The recipe record itself is kept (with `CompletedAt` set), so it remains in the user's history as an unlinked template they can cook again.

**Atomic completion via callback transaction** — the entire `CompleteAsync` flow (clear links → deduct inventory → mark complete) runs inside `IUnitOfWork.ExecuteInTransactionAsync`, which wraps everything in `CreateExecutionStrategy().ExecuteAsync(...)`. This is required because Npgsql's `EnableRetryOnFailure` execution strategy does not allow user-initiated transactions outside its own retry scope. Cache invalidation is placed after the callback so that a cache failure cannot roll back a successful DB completion.

**Unit conversion on completion** — the backend converts between compatible units (e.g. recipe says 500 ml, inventory item stored in liters) using `QuantityUnitConverter`. Incompatible category pairs (e.g. grams vs. liters) are skipped and the raw quantity is used as-is.

**Client-side ingredient check** — `RecipeDetailView` independently determines `haveIngredients` / `needIngredients` by scanning the inventory store directly rather than relying on `InInventory` from the DTO. This ensures the split always reflects the current in-memory inventory state.

**Unix prompt line endings** — `BuildPrompt` uses `\n`-only line endings (not `\r\n`). Windows-style `\r\n` caused Gemini to emit carriage-return characters (U+000D) in place of Danish letters such as ø, æ, and å in its JSON output.

**CORS on errors** — the global exception handler is registered after `app.UseCors()` so that CORS headers are present even on 500 responses. Without this, the browser misreports server exceptions as CORS failures.

**Compound-product false-positive guard** — the `needleWords ⊆ hayWords` match direction is only allowed when the haystack contains at most one more word than the needle. This prevents a generic recipe ingredient like "salt" from being incorrectly linked to a branded product such as "Salt and Vinegar Chips", while still allowing "oksekød" to link to "Hakket oksekød".

---

## Full Flow Example

**Scenario**: A user has the following inventory items:
- Pasta, 500 g
- Tomater, 4 stk
- Løg, 2 stk
- Oksekød, 300 g

### Step 1 — User selects items and requests suggestions

The user opens **Opskrifter → Generér** and checks all four items. They tap "Find opskrifter".

**Frontend** calls:
```
POST /api/users/abc-123/recipe-suggestions
{ "inventoryItemIds": ["id-pasta", "id-tomater", "id-løg", "id-oksekød"] }
```

### Step 2 — Backend builds the Gemini prompt

`RecipeSuggestionService.BuildPrompt` produces (Unix line endings throughout):

```
Du er en hjælpsom opskriftsassistent. Foreslå præcis 3 opskrifter på dansk. Følg regler nøje

Regler:
- VIGTIGT: Skriv ALTID korrekte danske bogstaver: ø, æ, å. Eksempler: løg, oksekød, mælk ...
- De 3 opskrifter skal være tydeligt forskellige fra hinanden: ...
- Mængder SKAL angives i én af disse enheder: kg, g, mg, l, ml, dl, cl, stk.
...

Tilgængelige ingredienser:
- Pasta: 500 g
- Tomater: 4 stk
- Løg: 2 stk
- Oksekød: 300 g

Svar med KUN gyldig JSON — ingen markdown, ingen forklaring.
```

### Step 3 — Gemini responds

Gemini 2.5 Flash returns a JSON object (conforming to the enforced response schema):

```json
{
  "recipes": [
    {
      "name": "Spaghetti bolognese",
      "description": "Klassisk italiensk kødsauce med pasta.",
      "instructions": "1. Hak løget fint.\n2. Brun oksekødet i en gryde.\n3. Tilsæt løg og tomater.\n4. Lad saucen simre i 20 minutter.\n5. Kog pasta og server.",
      "portions": 2,
      "ingredients": [
        { "productName": "Pasta", "quantity": 250, "unit": "g" },
        { "productName": "Oksekød", "quantity": 300, "unit": "g" },
        { "productName": "Tomater", "quantity": 3, "unit": "stk" },
        { "productName": "Løg", "quantity": 1, "unit": "stk" },
        { "productName": "Hvidløg", "quantity": 2, "unit": "stk" },
        { "productName": "Olivenolie", "quantity": 30, "unit": "ml" }
      ]
    },
    { /* Vegetarisk tomatsauce */ },
    { /* Pastasalat */ }
  ]
}
```

### Step 4 — Backend persists the suggestions

A new `batchId` is generated (e.g., `batch-999`). For each recipe, a `Recipe` row is created with `SuggestionBatchId = batch-999`. For each ingredient, `RecipeIngredientMatcher.FindBestMatch` runs:

```
"Pasta"     → normalizes to "pasta" → exact match → InventoryItemId = id-pasta
"Oksekød"   → normalizes to "oksekød" → exact match → InventoryItemId = id-oksekød
"Tomater"   → normalizes to "tomater" → exact match → InventoryItemId = id-tomater
"Løg"       → normalizes to "løg" → exact match → InventoryItemId = id-løg
"Hvidløg"   → normalizes to "hvidløg" → words: {"hvidløg"} — no subset match with any inventory item → InventoryItemId = null
"Olivenolie"→ no match → InventoryItemId = null
```

Three `Recipe` rows and their `RecipeEntry` children are saved.

### Step 5 — Frontend displays the suggestions

The store populates `suggestions` with the 3 `RecipeDto` objects. The Generate tab renders three recipe cards. The user taps **Spaghetti bolognese**.

### Step 6 — User views the recipe detail

Navigation goes to `/recipes/{id-bolognese}`. `RecipeDetailView` checks `recipeStore.suggestions` first and finds the recipe already in memory — no network request needed.

The view separates ingredients into two groups by scanning `invStore.itemsByInventory`:

- **Du har** (green): Pasta, Oksekød, Tomater, Løg (all exact word-token matches)
- **Du mangler** (neutral): Hvidløg, Olivenolie

`Hvidløg` is **not** matched to `Løg`: word-tokenisation splits on whitespace, so `{"hvidløg"}` and `{"løg"}` are two distinct tokens — neither is a subset of the other.

The user adjusts portions from 2 to 4. `scaleQty` recalculates: Pasta 250 g → 500 g, Oksekød 300 g → 600 g, etc.

### Step 7 — User completes the recipe

The user taps **"Jeg lavede dette — opdater lager"**.

**Step 7a — Re-link** (`linkRecipe`):
```
POST /api/recipes/id-bolognese/link
{ "inventoryIds": ["inv-123"] }
```
`LinkToInventoryAsync` fetches all items from inventory `inv-123` and runs `FindBestMatch` again, ensuring fresh links.

**Step 7b — Complete** (`completeRecipe`):
```
POST /api/recipes/id-bolognese/complete
```

`CompleteAsync` runs inside `ExecuteInTransactionAsync`:

1. **Snapshot links** — captures `[(id-pasta, 250, "g"), (id-oksekød, 300, "g"), (id-tomater, 3, "stk"), (id-løg, 1, "stk")]`
2. **Clear links** — sets all `InventoryItemId = null` on the bolognese entries
3. **Deduct inventory**:
   - Pasta: 500 g − 250 g = 250 g → `UpdateAsync`
   - Oksekød: 300 g − 300 g = 0 g → `DeleteAsync` (fully consumed)
   - Tomater: 4 stk − 3 stk = 1 stk → `UpdateAsync`
   - Løg: 2 stk − 1 stk = 1 stk → `UpdateAsync`
4. **Set `CompletedAt`** — transaction commits
5. **Invalidate cache** for inventory `inv-123` (after commit, outside transaction)

### Step 8 — Redirect

The frontend removes the recipe from `suggestions`, clears `currentRecipe`, and redirects to the inventory view where the updated quantities are visible.

---

The result: the user cooked bolognese, their pasta and beef are gone from inventory, leftover tomatoes and onions are correctly reduced — all in a single tap. The other two suggestions from the batch remain available to browse.
