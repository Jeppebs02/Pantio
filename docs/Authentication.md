# Authentication

## Overview

Pantio currently uses two separate authentication systems:

1. **Pantio authentication** through Auth0
2. **Netto authentication** through DSG's `customer-program` OAuth flow

These flows are separate by design. Pantio login identifies the Pantio user. Netto login links an external supermarket account to that user.

## Pantio Authentication

### Provider

- Auth0

### Current flow

1. The user logs into the frontend SPA through Auth0.
2. The frontend requests a token for the Pantio API audience.
3. The backend validates that token with ASP.NET Core `JwtBearer`.
4. The frontend calls `POST /api/users/ensure`.
5. The backend ensures the Auth0 subject exists in the local `users` table.

### Important frontend settings

- `VITE_AUTH0_DOMAIN`
- `VITE_AUTH0_CLIENT_ID`
- `VITE_AUTH0_AUDIENCE`
- `VITE_API_BASE_URL`

### Important backend settings

- `Auth0__Authority`
- `Auth0__Audience`

## Netto Authentication

### Provider chain

- DSG IDP
- Authorization Code + PKCE
- public client: `customer-program`

### Current implemented flow

1. The authenticated Pantio frontend creates a PKCE verifier and challenge.
2. The frontend redirects the user to:

```text
https://p-idp.dsgapps.dk/apps
```

3. The request includes:

- `clientId=customer-program`
- `tenantId=4`
- `channel=CustomerProgram`
- `clientFlow=gigya`
- `redirect_uri`
- `emailOrPhone`
- `code_challenge`
- `code_challenge_method=S256`
- `state`

4. DSG handles user login on its own page.
5. DSG redirects back to the frontend with an authorization code.
6. The frontend posts the code and PKCE verifier to the backend.
7. The backend exchanges the code at `https://idp.dsgapps.dk/token`.
8. The backend stores the DSG tokens in `store_connections`.

## Stored Netto Token Data

Current backend storage includes:

- `access_token`
- `refresh_token`
- `id_token`
- `token_expires_at`

These tokens are then used by the backend for manual receipt sync.

## Authenticated DSG Calls

Current DSG receipt calls use:

- `Authorization: Bearer {access_token}`
- `x-id_token: Bearer {id_token}`

against:

- `GET https://p-club.dsgapps.dk/api/cp/receipt`
- `GET https://p-club.dsgapps.dk/api/cp/receipt/details`

## Local Development Requirements

Current local frontend origin:

```text
http://localhost:3000/
```

Current local backend origin:

```text
http://localhost:5000
```

The Netto redirect URI must match on both sides:

- frontend `VITE_NETTO_REDIRECT_URI`
- backend `Netto__RedirectUri`

Current local value:

```text
http://localhost:3000/
```

## Current Limitations

- The Netto authentication flow is browser-based and manual.
- There is no background sync authorization workflow yet.
- The current frontend screen is a test harness, not a final product UX.
- Only Netto is currently implemented on top of the chain-generic backend surface.
