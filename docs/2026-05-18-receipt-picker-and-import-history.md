# Receipt Picker & Import History

**Date:** 2026-05-18  
**Status:** Completed

## Problem

After connecting a Netto account the app immediately synced all historical receipts without user input, and there was no visibility into what had been synced.

## Features Implemented

### 1. Manual receipt selection on first connect

After linking a Netto account, the user is taken to `/store/netto` where they see a scrollable list of their historical receipts from DSG (sorted newest-first, basic info only: store, date, total). They choose which receipts to import before anything is written to the inventory. Importing is blocked if the user has no inventory yet — a clear error message is shown.

- **Select subset** → only those receipts are imported. All non-selected receipts are inserted as stubs (no line items) so they are permanently excluded from all future syncs via deduplication.
- **"Start forfra"** (start fresh) → nothing historical is imported; all current receipts are stubbed so future syncs only pick up new receipts.

Reconnecting a previously linked account resets `LastPolledAt` so the picker is shown again.

### 2. Receipt import history

Every sync operation (manual and auto) is logged to a new `sync_logs` table. The `/store/netto` active state shows a chronological history of each sync: timestamp, status (Success / Failed), receipts imported, inventory items added.

## Database Changes

**`store_connections`** — new column:
```sql
import_horizon TIMESTAMP NULL
```

**New table `sync_logs`:**
```sql
id                        UUID PRIMARY KEY
store_connection_id       UUID NOT NULL REFERENCES store_connections(id)
synced_at                 TIMESTAMP NOT NULL
status                    VARCHAR NOT NULL   -- 'Success' | 'Failed'
imported_receipt_count    INT NOT NULL DEFAULT 0
processed_inventory_count INT NOT NULL DEFAULT 0
error_message             TEXT NULL
```

## New API Endpoints

| Method | Path | Purpose |
|---|---|---|
| `GET` | `/api/users/{userId}/store-connections/{id}/pending-receipts` | Fetch DSG receipt summaries without importing |
| `POST` | `/api/users/{userId}/store-connections/{id}/import` | Import selected receipts + set import horizon |
| `GET` | `/api/users/{userId}/store-connections/{id}/sync-history` | Return sync log for this connection |

## Frontend Routes

| Path | View | Purpose |
|---|---|---|
| `/store` | `ConnectStoreView` | Store overview cards (click to drill in) |
| `/store/netto` | `NettoDetailView` | Netto detail: connect form / receipt picker / active + history |

## Files Changed

**Backend:**
- `PantioClassLibrary/Entities/StoreConnection.cs` — added `ImportHorizon`
- `PantioClassLibrary/Entities/SyncLog.cs` — new entity
- `PantioClassLibrary/DTO/PendingReceiptDto.cs` — new
- `PantioClassLibrary/DTO/ImportSelectedReceiptsDto.cs` — new
- `PantioClassLibrary/DTO/SyncLogDto.cs` — new
- `PantioClassLibrary/Interfaces/Services/IStoreConnectionService.cs` — 3 new method signatures
- `PantioClassLibrary/Interfaces/Repository/IStoreConnectionRepository.cs` — 2 new method signatures
- `PantioRepository/EntityFramework/PantioDbContext.cs` — added `SyncLogs` DbSet + index
- `PantioRepository/EntityFramework/Repositories/StoreConnectionRepository.cs` — `SaveSyncLogAsync`, `GetSyncLogsAsync`
- `PantioRepository/EntityFramework/EFMigrations/` — migration `AddSyncLogsAndImportHorizon`
- `PantioAPI/Services/StoreConnectionService.cs` — `GetPendingReceiptsAsync`, `ImportSelectedAsync`, `GetSyncHistoryAsync`; modified `SyncAsync` (sync logging, removed date-based horizon filter — deduplication handles exclusion); modified `LinkAsync` (reset `LastPolledAt` on reconnect); `ImportSelectedAsync` stubs non-selected receipts and blocks import if no inventory exists
- `PantioAPI/Controllers/StoreConnectionController.cs` — 3 new endpoints

**Frontend:**
- `src/services/types.ts` — `PendingReceiptDto`, `ImportSelectedReceiptsDto`, `SyncLogDto`
- `src/services/storeConnection.ts` — `getPendingReceipts`, `importSelected`, `getSyncHistory`
- `src/stores/storeConnection.ts` — new state + actions for pending receipts, import, sync history
- `src/views/store/ConnectStoreView.vue` — simplified to store overview cards
- `src/views/store/NettoDetailView.vue` — new view (3 modes: disconnected / pending / active)
- `src/router/index.ts` — `/store/netto` route; Netto OAuth callback now forwards to `/store/netto`

## Bug Fixes (post-release)

### 1. Receipt dates all showing as today
DSG's `createdAt` field was null or unparseable for many receipts, causing `ParseCreatedAt` to fall back to `DateTime.UtcNow`. Fixed by extracting the Unix epoch from the last segment of the receipt ID (`{storeId}-{pos}-{txn}-{epochSeconds}`) as a secondary fallback. File: `PantioAPI/Services/NettoAuthClient.cs`.

### 2. Scrollable receipt list
With 100+ receipts the import buttons scrolled off screen. Fixed by adding `max-height: min(55vh, 380px); overflow-y: auto` to `.receipt-list` in `NettoDetailView.vue`. The select-all row and action buttons remain fixed outside the scroll container.

### 3. Import allowed without an inventory
`ImportSelectedAsync` did not verify an inventory existed first. Receipt lines were stored in the DB but never converted to inventory items. Fixed by adding an inventory check at the top of `ImportSelectedAsync` — throws `InvalidOperationException` which the controller maps to HTTP 422. The view parses the response body and displays the message to the user.

### 4. Subsequent syncs ignoring the date cutoff
The `ImportHorizon` date-based filter in `SyncAsync` was unreliable (date parse issues, timezone edge cases). Replaced with ID-based stub exclusion: `ImportSelectedAsync` now inserts all non-selected receipt IDs as stubs (empty line items) so `GetExistingReceiptIdsAsync` deduplication in `SyncAsync` finds them and skips them permanently. The `ImportHorizon` filter has been removed from `SyncAsync`.
