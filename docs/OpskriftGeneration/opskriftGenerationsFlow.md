# Opskriftgenererings Flow

Dette dokument beskriver det komplette flow for opskriftforslag-funktionen i Pantio — fra frontend-anmodning til gemt opskrift og afsluttet madlavning.

---

## Overblik

Brugeren vælger ingredienser fra sit lager, sender dem til backend'en, og modtager 3 AI-genererede opskriftsforslag. Hver opskrift viser hvilke ingredienser brugeren allerede har, og hvilke der mangler. Når brugeren vælger en opskrift og markerer den som afsluttet, trækkes brugte mængder fra lageret automatisk.

---

## 1. Hent opskriftsforslag

### Endpoint
```
POST /api/users/{userId}/recipe-suggestions
Body: { "inventoryItemIds": ["guid1", "guid2", ...] }
```

### Trin-for-trin

**Trin 1 — Validering**
`PantioAPI/Controllers/RecipeSuggestionController.cs`

Controlleren tjekker at `inventoryItemIds` ikke er tom. Returnerer `400 Bad Request` hvis listen er tom.

---

**Trin 2 — Hent lagervarer fra databasen**
`PantioAPI/Services/RecipeSuggestionService.cs` ->
`PantioRepository/EntityFramework/Repositories/InventoryItemRepository.cs` (`GetByIdsAsync`)

De angivne ID'er slås op i `inventory_items`-tabellen med et enkelt `WHERE id = ANY(...)` kald. Returnerer `InventoryItem`-entiteter (`PantioClassLibrary/Entities/InventoryItem.cs`) med navn, mængde og enhed.

---

**Trin 3 — Byg Gemini-prompt**
`PantioAPI/Services/RecipeSuggestionService.cs` (`BuildPrompt`)

Bygger en dansk tekstprompt med:
- Instruktioner om at foreslå præcis 3 tydeligt forskellige opskrifter
- Listen af tilgængelige ingredienser med mængder
- Det præcise JSON-skema som Gemini skal svare i

---

**Trin 4 — Kald Gemini AI**
`PantioAPI/Services/RecipeSuggestionService.cs` (`CallGeminiAsync`)
`PantioAPI/GeminiOptions.cs`

Sender POST til Gemini REST API:
```
https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent
```
API-nøglen hentes fra `GeminiOptions` som bindes til miljøvariablen `Gemini__ApiKey`. `responseMimeType: "application/json"` tvinger Gemini til at svare i JSON. Navigerer `candidates[0].content.parts[0].text` for at udtrække JSON-strengen.

---

**Trin 5 — Deserialisér Geminies svar**
`PantioAPI/Services/RecipeSuggestionService.cs`

JSON-strengen deserialiseres til private record-typer:
- `GeminiResponseBody` -> `GeminiRecipe` -> `GeminiIngredient`

Disse typer eksisterer kun internt i servicen og bruges udelukkende til deserialisering.

---

**Trin 6 — Match ingredienser til lagervarer**
`PantioAPI/Services/RecipeIngredientMatcher.cs` (`FindBestMatch`)

For hver ingrediens Gemini returnerede, forsøges et match mod de hentede lageravarer via normaliseret substringmatch:
1. Eksakt match (case-insensitivt, trimmet)
2. Substring-match i begge retninger

Hvis match findes sættes `InventoryItemId` på `RecipeEntry`. Hvis ikke (f.eks. "salt", "olie") forbliver den `null`.

---

**Trin 7 — Gem 3 opskrifter i databasen**
`PantioAPI/Services/RecipeSuggestionService.cs` ->
`PantioRepository/EntityFramework/Repositories/RecipeRepository.cs` (`CreateAsync`)

Bygger `Recipe`- og `RecipeEntry`-entiteter (`PantioClassLibrary/Entities/Recipe.cs`, `RecipeEntry.cs`). Alle 3 opskrifter tildeles samme `SuggestionBatchId` (ny `Guid`) så systemet ved de hører sammen. Gemmes til `recipes`- og `recipe_entries`-tabellerne via EF Core.

---

**Trin 8 — Map til DTO og returner**
`PantioRepository/Mapper/RecipeSuggestionMapper.cs` (`ToDto`)
`PantioClassLibrary/DTO/RecipeSuggestionDto.cs`

Hver `Recipe` mappes til `RecipeSuggestionDto`. Hver ingrediens (`RecipeSuggestionIngredientDto`) indeholder:

| Felt | Beskrivelse |
|------|-------------|
| `productName` | Ingrediensens navn |
| `quantity` | Mængde |
| `measuringUnit` | Enhed (g, stk, dl, osv.) |
| `inventoryItemId` | ID på matchet lageravare, eller `null` |
| `inInventory` | `true` hvis brugeren har varen, `false` hvis den mangler |

Returneres som `200 OK` pakket i `RecipeSuggestionListDto`.

---

## 2. Genlinkér opskrift til aktuelt lager (ved genbrug)

### Endpoint
```
POST /api/recipes/{recipeId}/link
Body: { "inventoryId": "guid" }
```

`PantioAPI/Controllers/RecipeController.cs` ->
`PantioAPI/Services/RecipeService.cs` (`LinkToInventoryAsync`) ->
`PantioRepository/EntityFramework/Repositories/RecipeRepository.cs` (`UpdateEntryLinksAsync`)

Hentes opskriften med dens ingredienser, derefter hentes alle lageravarer i det angivne lager. `RecipeIngredientMatcher.FindBestMatch` køres for hver ingrediens mod det nuværende lager og de opdaterede links gemmes. Bruges når brugeren vil lave en opskrift igen og lageret har ændret sig siden forslaget.

---

## 3. Afslut opskrift

### Endpoint
```
POST /api/recipes/{recipeId}/complete
```

`PantioAPI/Controllers/RecipeController.cs` ->
`PantioAPI/Services/RecipeService.cs` (`CompleteAsync`)

### Rækkefølge (orden er vigtig pga. fremmednøgle-begrænsninger)

**Trin 1 — Slet søsteopskrifter**
`PantioRepository/EntityFramework/Repositories/RecipeRepository.cs` (`DeleteAsync`)

De 2 andre opskrifter fra samme `SuggestionBatchId` slettes. Deres `recipe_entries` cascade-slettes med dem, så ingen poster peger på lageravarer der skal slettes.

**Trin 2 — Gem snapshot af links og ryd dem**
`PantioRepository/EntityFramework/Repositories/RecipeRepository.cs` (`ClearInventoryLinksAsync`)

`InventoryItemId` på alle ingredienser i den valgte opskrift sættes til `null`. Opskriften bliver hermed en genanvendelig skabelon uden binding til specifikke lageravarer.

**Trin 3 — Dekrement eller slet lageravarer**
`PantioRepository/EntityFramework/Repositories/InventoryItemRepository.cs` (`UpdateAsync` / `DeleteAsync`)

For hvert link fra snapshot:
- Hvis ny mængde > 0: opdater mængden (`UpdateAsync`)
- Hvis ny mængde <= 0: slet varen fra lageret (`DeleteAsync`)

**Trin 4 — Markér opskrift som afsluttet**
`PantioRepository/EntityFramework/Repositories/RecipeRepository.cs` (`SetCompletedAsync`)

`completed_at` sættes til `DateTime.UtcNow` i `recipes`-tabellen.

---

## Entiteter og tabeller

| Entitet | Tabel | Fil |
|---------|-------|-----|
| `Recipe` | `recipes` | `PantioClassLibrary/Entities/Recipe.cs` |
| `RecipeEntry` | `recipe_entries` | `PantioClassLibrary/Entities/RecipeEntry.cs` |
| `InventoryItem` | `inventory_items` | `PantioClassLibrary/Entities/InventoryItem.cs` |

Migrationen der tilføjede `suggestion_batch_id` til `recipes`:
`PantioRepository/EntityFramework/EFMigrations/20260505090821_AddSuggestionBatchIdToRecipe.cs`

---

## DI-registrering

`PantioAPI/Program.cs`

```csharp
builder.Services.Configure<GeminiOptions>(builder.Configuration.GetSection("Gemini"));
builder.Services.AddHttpClient("Gemini");
builder.Services.AddScoped<IRecipeRepository, RecipeRepository>();
builder.Services.AddScoped<IRecipeService, RecipeService>();
builder.Services.AddScoped<IRecipeSuggestionService, RecipeSuggestionService>();
```

---

## Konfiguration

| Indstilling | Fil | Bemærkning |
|-------------|-----|------------|
| Gemini API-nøgle | Miljøvariabel `Gemini__ApiKey` | Sættes ved opstart, aldrig i kildekode |
| Gemini model | `RecipeSuggestionService.cs` | `gemini-2.5-flash` via v1beta |
| Databaseforbindelse | `appsettings.Development.json` | Lokalt Docker-miljø |
