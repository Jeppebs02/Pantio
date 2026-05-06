# Netto Receipt Import Plan

## Goal

Implement backend support for Netto receipt imports through a chain-generic supermarket integration surface, with Netto as the first supported chain.

## Phase 1

- Add `StoreConnection` API endpoints for list, link, manual sync trigger, and disconnect.
- Add a repository and service layer around the existing `store_connections` table.
- Keep Netto as the only supported live chain in this phase.
- Use manual sync as a placeholder that records sync activity without calling Netto yet.

### Status

- Completed.
- Implemented in the backend with EF-backed list, link, sync placeholder, and disconnect endpoints.

## Phase 2

- Add Netto-specific auth and token lifecycle handling.
- Support backend-owned linking with persisted DSG tokens.
- Add receipt list and receipt detail client methods for the DSG Club API.

### Status

- In progress.
- This phase now exchanges DSG authorization codes for persisted tokens and refreshes tokens during manual sync.
- Completed for manual sync.
- Manual sync now exchanges DSG codes, refreshes tokens when needed, fetches receipt summaries and receipt details, and persists new `Receipt` and `ReceiptLine` rows idempotently.

## Phase 3

- Add receipt import orchestration with idempotent persistence for `Receipt` and `ReceiptLine`.
- Add a background polling worker for active connections.
- Update connection health state based on sync success and failure.

### Status

- Partially completed.
- Idempotent receipt and receipt-line import is now in place for manual sync.
- Background polling and connection health state are still pending.

## Phase 4

- Process imported receipt lines into inventory items.
- Skip non-product lines such as pant deposits.
- Use `processed_to_inventory` and `receipt_line_id` to keep imports idempotent.

### Status

- In progress.
- Manual sync now processes imported `ReceiptLine` rows with `item_type == "01"` into `InventoryItem` rows and marks those lines as processed.
- Quantity unit and category enrichment are still pending.

## Current API

- `GET /api/users/{userId:guid}/store-connections`
- `POST /api/users/{userId:guid}/store-connections/{chain}`
- `POST /api/users/{userId:guid}/store-connections/{connectionId:guid}/sync`
- `DELETE /api/users/{userId:guid}/store-connections/{connectionId:guid}`

### Link Request Body

```json
{
  "authorizationCode": "code-from-dsg-idp",
  "codeVerifier": "pkce-verifier-used-for-the-auth-request",
  "redirectUri": "http://localhost"
}
```

## Defaults Chosen

- Keep the controller and service contract chain-generic.
- Support only `Netto` in Phase 1.
- Disconnecting clears stored tokens and preserves historic receipts.
- Failure notifications remain out of scope until connection health fields are added in a later phase.
- Until backend-initiated browser login is added, the client is responsible for obtaining the DSG authorization code and PKCE verifier, and the backend owns token exchange, storage, and refresh.
- Imported receipt lines are currently written to the user's first inventory ordered by `name` then `id`, because `Inventory` does not yet have a created timestamp for an "oldest inventory" policy.
