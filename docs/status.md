# Pantio — Project Status

> Opdateret: 2026-05-22 | Branch: `dev`

---

## Hvad er Pantio?

Pantio er en dansk madlager- og husholdningsapp, der hjælper brugerne med at reducere madspild. Kerneidéen er at forbinde brugerens loyalitetskort hos supermarkeder (Netto, Føtex, Bilka), importere kvitteringer automatisk og bruge den information til at holde styr på:

- Hvad der er på lager og hvornår det udløber
- Indkøbslister
- AI-genererede madopskrifter baseret på hvad der er i køleskabet

---

## Tech Stack

| Lag | Teknologi |
|---|---|
| Backend runtime | .NET 10, ASP.NET Core Web API |
| Database | PostgreSQL (EF Core 10 + Npgsql) |
| Cache | Redis (30-dages TTL på produktdata) |
| Frontend | Vue 3 + Vite (mobil-first PWA / Capacitor) |
| Auth | Auth0 (JWT), OAuth2 PKCE mod DSG Club API |
| AI | Google Gemini 2.5 Flash (opskriftforslag, JSON-tvunget output) |
| Produktdata | OpenFoodFacts API (produktopslag via EAN) |
| Butiksintegration | Netto via DSG Club API |
| Containerisering | Docker (multi-stage build) |
| Tests | NUnit 4 + Moq, EF Core InMemory |

---

## Backend-arkitektur

Backenden er opdelt i fire projekter med et strengt lagdelt ansvar:

```
PantioAPI        →  Controllers + Services + Baggrundstjenester + DI
PantioRepository →  EF DbContext, Repositories, Migrations, Mappers
PantioClassLibrary → Entities, DTOs (records), Interfaces, Enums
PantioTest       →  Controller-, service- og repositorytests
```

Dataflowet følger altid:

```
HTTP-request → Controller → Service → Repository → PostgreSQL
```

- Controllers håndterer udelukkende HTTP-bekymringer (routing, statuskoder, model binding).
- Services indeholder forretningslogik og orkestrering.
- Repositories er den eneste lagl der taler direkte med EF Core.
- Interfaces og DTOs i `PantioClassLibrary` holder lagene løst koblede.

### Autentificering

Alle endpoints kræver Auth0 JWT som standard. En global action filter (`Auth0OwnershipFilter`) validerer at JWT'ens `sub`-claim matcher `userId`-ruteparameteren — det forhindrer brugere i at tilgå hinandens data.

Registrering er beskyttet af en `X-Registration-Secret` header (server-side hemmelighed).

---

## Implementerede funktioner

### 1. Brugerstyring

**Backend:** `UserController`, `UserService`

- `POST /api/auth/register` — opretter bruger med registreringshemmelighed (bruges ikke af den normale app-flow).
- `POST /api/users/ensure` — idempotent oprettelse/opslag af bruger baseret på Auth0 `sub`. Frontenden kalder dette efter login.
- `DELETE /api/users/{userId}` — sletter bruger og alle tilhørende data.
- `PATCH /api/users/{userId}/fcm-token` — opdaterer Firebase Cloud Messaging-token til push-notifikationer.

**Frontend:** Login-flow via Auth0 redirect (`LoginView.vue`), onboarding (`OnboardingView.vue`), brugerens profil via `useAuthStore`.

---

### 2. Lagerstyring (Inventory)

**Backend:** `InventoryController`, `InventoryItemController`, `InventoryService`, `InventoryItemService`

Et lager (`Inventory`) er en navngivet beholder for madvarer. En bruger kan have flere lagre (fx "Køleskab", "Fryser", "Skab").

Endpoints:

```
POST   /api/users/{userId}/inventories               Opret lager
GET    /api/users/{userId}/inventories               Hent alle lagre
GET    /api/users/{userId}/inventories/{id}          Hent enkelt lager
PUT    /api/users/{userId}/inventories/{id}          Opdater lager
DELETE /api/users/{userId}/inventories/{id}          Slet lager

POST   /api/inventories/{inventoryId}/items          Tilføj vare
GET    /api/inventories/{inventoryId}/items          Hent alle varer
PUT    /api/inventories/{inventoryId}/items/{id}     Opdater vare
DELETE /api/inventories/{inventoryId}/items/{id}     Slet vare
PATCH  /api/inventories/{inventoryId}/items/{id}/expiry  Sæt manuel udløbsdato
```

Optimistisk concurrency er implementeret med row version-kolonner på både `Inventory` og `InventoryItem` — konflikter returneres som HTTP 409.

**Frontend:** `InventoryListView.vue`, `InventoryView.vue`, `ItemDetailView.vue`.

---

### 3. Produktopslag (EAN / Stregkode)

**Backend:** `ProductsController`, `ProductCacheService`, `OpenFoodFactsService`

Produktdata hentes via en tre-trins lookup-kæde:

```
1. Redis-cache (30 dages TTL)
   ↓ miss
2. PostgreSQL (ProductCache tabel, per-bruger)
   ↓ miss
3. OpenFoodFacts API (ekstern)
   → gem i Postgres og Redis
```

Produktkategorier (seeded i migrering) bruges til at slå standardholdbarhed op (fx "Mejeri: 7 dage"). EAN-opslaget returnerer produktnavn, mærke, mængde, enhed, næringsstoffer og kategori.

**Contribute-endpoints** (brugerbidrag sendt til OpenFoodFacts):

```
POST /api/products/{ean}/contribute/quantity          Bidrag med mængde/enhed
POST /api/products/{ean}/contribute/new-product       Opret nyt produkt
POST /api/products/{ean}/contribute/nutrition-image   Upload næringstabel-billede
```

**Frontend:** `ManualEntryView.vue` håndterer hele tilføjelsesflowet — EAN-opslag, manuel indskrivning, bidrag til manglende data, og gemning til lager.

---

### 4. Stregkodescanning

**Frontend:** `BarcodeScanner.vue`, `useBarcode` composable, Capacitor-plugin.

På native (iOS/Android) bruges Capacitor til kamerascanning. På web/dev simuleres scanning via `window.prompt`. Scanning kan startes fra hjemmeskærmen ("Hurtig scan") eller direkte i `ManualEntryView`. Hvis brugeren har flere lagre, vises en bottom sheet til valg af lager.

---

### 5. Indkøbslister

**Backend:** `ShoppingListController`, `ShoppingListService`

```
GET    /api/users/{userId}/shopping-lists                      Hent alle lister
POST   /api/users/{userId}/shopping-lists                      Opret liste
GET    /api/users/{userId}/shopping-lists/{listId}             Hent liste med varer
DELETE /api/users/{userId}/shopping-lists/{listId}             Slet liste
POST   /api/users/{userId}/shopping-lists/from-recipe          Opret liste fra opskrift
POST   /api/users/{userId}/shopping-lists/{listId}/items       Tilføj vare
DELETE /api/users/{userId}/shopping-lists/{listId}/items/{id}  Fjern vare
PATCH  /api/users/{userId}/shopping-lists/{listId}/items/{id}/toggle  Afkryds/fjern afkrydsning
```

**Frontend:** `ShoppingListView.vue`, `ShoppingDetailView.vue`, `ShoppingAddItemView.vue`.

---

### 6. AI-opskriftforslag (Gemini)

**Backend:** `RecipeSuggestionController`, `RecipeSuggestionService`, `RecipeIngredientMatcher`

Brugeren vælger varer fra sit lager og sender en forespørgsel. Backend'en kalder Google Gemini 2.5 Flash med en struktureret prompt, der tvinger JSON-output. Gemini returnerer opskriftforslag med ingrediensliste, fremgangsmåde og estimeret tid.

```
POST /api/users/{userId}/recipe-suggestions
Body: { inventoryItemIds: [...] }
```

`RecipeIngredientMatcher` forsøger at matche opskriftingredienser mod eksisterende lagervarer (til forbrug-ved-cook-flow).

Gemte opskrifter:

```
GET  /api/users/{userId}/recipes              Liste med søgning/filtrering
GET  /api/users/{userId}/recipes/{recipeId}   Hent detaljer
POST /api/users/{userId}/recipes/{recipeId}/save  Toggle gem/fjern fra gemt
POST /api/recipes/{recipeId}/complete             Marker opskrift som lavet (forbruger ingredienser)
POST /api/recipes/{recipeId}/link                 Kobl opskrift til lager(e)
```

**Frontend:** `RecipeMainView.vue`, `RecipeSuggestionsView.vue`, `RecipeDetailView.vue`, `RecipeGeneratingLoader.vue`.

---

### 7. Netto-integration (DSG Club API)

**Backend:** `StoreConnectionController`, `StoreConnectionService`, `NettoReceiptService`

Netto bruger DSG's OAuth2 Authorization Code + PKCE-flow:

```
1. Frontend åbner PKCE-redirect mod https://p-idp.dsgapps.dk/apps
2. DSG redirecter tilbage til frontenden med authorization code
3. Frontend sender code + PKCE-verifier til backend
4. Backend veksler code til tokens via https://idp.dsgapps.dk/token
5. Backend gemmer access_token, refresh_token, id_token, token_expires_at
```

Kvitteringshentning:

```
GET  /api/users/{userId}/store-connections                          Hent forbindelser
POST /api/users/{userId}/store-connections/{chain}                  Kobl butiksintegration
GET  /api/users/{userId}/store-connections/{id}/pending-receipts    Hent kvitteringer der ikke er importeret
POST /api/users/{userId}/store-connections/{id}/import              Importer valgte kvitteringer
POST /api/users/{userId}/store-connections/{id}/sync                Manuel synkronisering
GET  /api/users/{userId}/store-connections/{id}/sync-history        Vis synkroniseringslog
PATCH /api/users/{userId}/store-connections/{id}/auto-sync          Slå auto-sync til/fra
DELETE /api/users/{userId}/store-connections/{id}                   Afkobl integration
```

Kvitteringer importeres idempotent (keyed på DSG receipt-id). Kvitteringslinjer gemmes med produktbeskrivelse, EAN, pris, rabat, mængde og varetype.

**Frontend:** `ConnectStoreView.vue`, `NettoDetailView.vue`.

---

### 8. Udløbsdato-overvågning

**Backend:** `ExpiryCheckBackgroundService`, `ExpiryCheckService`, `ExpiryDateService`

En baggrundstjeneste kører hvert 4,8. time og opretter `ExpiryNotification`-records for varer der udløber inden for 3 dage. Manuel override af udløbsdato er mulig via `PATCH .../expiry`.

---

### 9. Inaktiv-bruger håndtering

**Backend:** `InactiveUserBackgroundService`, `InactiveUserService`

Baggrundstjeneste der sporer inaktive brugere og kan igangsætte oprydning. Felter for sidst-aktiv tidsstempel holdes på `User`-entiteten.

---

## Generelt systemflow

Herunder er det typiske flow for en bruger der tilføjer en vare til sit lager via stregkodescanning:

```
[Bruger] Trykker "Hurtig scan" på hjemmeskærmen
    ↓
[Frontend] Åbner kamera via Capacitor / simuleret input i dev
    ↓
[Frontend] EAN sendt til GET /api/products/{ean}
    ↓
[Backend] ProductsController
    → Tjekker Redis-cache
    → Tjekker PostgreSQL ProductCache
    → Kalder OpenFoodFacts API (ved cache miss)
    → Matcher mod produktkategori (standardholdbarhed)
    → Returnerer produktdata til frontend
    ↓
[Frontend] ManualEntryView viser produktnavn, mængde, enhed
    → Bruger kan rette mængde/enhed og bidrage til OFF
    → Bruger sætter antal og eventuel manuel udløbsdato
    ↓
[Frontend] POST /api/inventories/{inventoryId}/items
    ↓
[Backend] InventoryItemController → InventoryItemService → Repository → PostgreSQL
    ↓
[Baggrund] ExpiryCheckBackgroundService kører hvert 4,8h
    → Opretter ExpiryNotification for varer der udløber inden for 3 dage
```

Flow for Netto-kvitteringsimport:

```
[Bruger] Trykker "Netto+" → "Kobl Netto"
    ↓
[Frontend] Starter PKCE-flow → åbner DSG login-side
    ↓
[DSG] Bruger logger ind, DSG redirecter med code
    ↓
[Frontend] POST /api/users/{userId}/store-connections/Netto { code, codeVerifier }
    ↓
[Backend] Veksler code til tokens, gemmer forbindelsen
    ↓
[Bruger] Trykker "Synkroniser" eller aktiverer auto-sync
    ↓
[Backend] POST .../sync → henter kvitteringer fra DSG Club API
    → Gemmer Receipt + ReceiptLines idempotent i Postgres
    ↓
[Bruger] Ser ventende kvitteringer, vælger hvilke der skal importeres
    ↓
[Backend] POST .../import → markerer kvitteringer som importeret
```

---

## Hvad er ikke implementeret endnu

| Funktion | Status |
|---|---|
| Føtex / Bilka-integration | Ikke startet — vises som "Kommer snart" i UI |
| Auto-sync baggrundspoll | Placeholder; strukturen er der, men polling er ikke aktiveret |
| Push-notifikationer (FCM) | Token gemmes, men afsendelse ikke implementeret |
| Færdig kvitterings-til-lager-konvertering | Kvitteringer importeres, men auto-oprettelse af lagervarer mangler |
| Tests på rigtig PostgreSQL | Tests kører mod EF InMemory (ikke real Postgres) |
