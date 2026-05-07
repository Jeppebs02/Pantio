# Pantio - Project Overview

Pantio is a household inventory management app. Users connect supermarket loyalty accounts, import receipts, and use that purchase history as input for personal inventory, expiry tracking, shopping lists, and recipe workflows.

## Tech Stack

| Layer | Technology |
|---|---|
| Runtime | .NET 10 |
| Web framework | ASP.NET Core Web API |
| Database | PostgreSQL |
| ORM | Entity Framework Core 10 with Npgsql |
| Frontend | Vue 3 + Vite |
| Authentication | Auth0 |
| Containerisation | Docker / docker compose |
| Testing | NUnit, Moq, EF Core InMemory |

## Solution Structure

```text
backend/
  PantioClassLibrary/       Shared DTOs, entities, interfaces, enums
  PantioRepository/         EF DbContext, repositories, migrations
  PantioAPI/                API host, controllers, services, DI wiring
  PantioTest/               Controller, service, and repository tests
frontend/
  PantioApp/                Vue frontend
docs/                       Project documentation
```

## Architecture

Pantio uses a layered backend structure:

```text
Controller -> Service -> Repository -> DbContext
```

- Controllers handle HTTP concerns only.
- Services contain orchestration and business logic.
- Repositories are the only layer that talks to EF Core directly.
- Shared interfaces and DTOs live in `PantioClassLibrary`.

## Core Domain Model

Key entities:

- `User`
- `UserProfile`
- `Inventory`
- `InventoryItem`
- `ExpiryDate`
- `ExpiryNotification`
- `StoreConnection`
- `Receipt`
- `ReceiptLine`
- `ProductCache`
- `NutritionFacts`
- `ShoppingList`
- `ShoppingListItem`
- `Recipe`
- `RecipeEntry`

Important current relationships:

- A `User` owns inventories, store connections, receipts, shopping lists, and recipes.
- A `StoreConnection` represents one external supermarket account for one user and chain.
- A `Receipt` belongs to a `StoreConnection` and a `User`.
- A `ReceiptLine` belongs to a `Receipt`.

## Current Integration State

Pantio now has a working first supermarket integration with **Netto**.

### Confirmed working

- Auth0 login in the frontend.
- Backend user provisioning through `POST /api/users/ensure`.
- Chain-generic `StoreConnection` API and service surface.
- Netto linking through DSG's `customer-program` Authorization Code + PKCE flow.
- Backend token persistence in `store_connections`.
- Manual receipt sync through DSG Club API.
- Idempotent persistence of `Receipt`.
- Persistence of complete `ReceiptLine` data, including:
  - product descriptions
  - EAN values when present
  - prices
  - discounts
  - quantity
  - item type

### Not finished yet

- Background polling / automatic sync.
- Connection health and retry state.
- Finished receipt-to-inventory processing.
- Finished frontend receipt browsing UX.

## API Surface

Current store connection endpoints:

- `GET /api/users/{userId:guid}/store-connections`
- `POST /api/users/{userId:guid}/store-connections/{chain}`
- `POST /api/users/{userId:guid}/store-connections/{connectionId:guid}/sync`
- `DELETE /api/users/{userId:guid}/store-connections/{connectionId:guid}`

Current user provisioning endpoint used by the frontend:

- `POST /api/users/ensure`

## Authentication Overview

Pantio currently uses two authentication layers:

### Pantio authentication

- Auth0 is used for user login in the frontend.
- The frontend requests a bearer token for the Pantio API audience.
- The backend validates Auth0 JWTs through `JwtBearer`.
- The frontend ensures the local backend user exists after login.

### Netto authentication

- Netto linking uses DSG's `customer-program` OAuth client.
- The frontend opens a PKCE redirect flow against `https://p-idp.dsgapps.dk/apps`.
- DSG redirects back to the frontend with an authorization code.
- The frontend posts the code and PKCE verifier to the backend.
- The backend exchanges the code at `https://idp.dsgapps.dk/token`.
- The backend stores:
  - `access_token`
  - `refresh_token`
  - `id_token`
  - `token_expires_at`

## Local Development

Current local Docker setup:

- frontend: `http://localhost:3000`
- backend: `http://localhost:5000`
- postgres: `localhost:5432`

Important current redirect URI:

```text
http://localhost:3000/
```

The frontend and backend must both use the same Netto redirect URI for the token exchange flow to work.

The frontend receives build-time `VITE_*` values through Docker build args.
The backend receives Auth0 and Netto runtime settings through container environment variables.

## Testing Approach

- Controller tests mock services.
- Service tests mock repositories and other dependencies.
- Repository tests use a real EF Core context backed by InMemory.

Typical commands:

```bash
dotnet test backend/PantioTest/PantioTest.csproj
dotnet build backend/PantioAPI/PantioAPI.csproj
```

## Important Current Notes

- Netto is the only live supermarket integration at the moment.
- Receipt import is now trusted; inventory creation from receipt lines is not yet considered complete.
- Existing imported receipts are keyed by DSG receipt id and imported idempotently.
- Receipt detail parsing had to be made tolerant of DSG response shape differences during implementation.
