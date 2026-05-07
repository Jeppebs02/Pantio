# Netto Integration

## Purpose

Pantio's first live supermarket integration is **Netto**. The feature allows a Pantio user to link a Netto account and import historical receipts into Pantio.

The implemented feature currently covers:

- account linking
- token storage
- manual receipt sync
- receipt header persistence
- receipt line persistence

It does not yet provide a finished inventory-creation workflow.

## Backend Components

Main backend pieces:

- `StoreConnectionController`
- `StoreConnectionService`
- `StoreConnectionRepository`
- `NettoAuthClient`

Main database tables:

- `store_connections`
- `receipts`
- `receipt_lines`

## Frontend Flow

1. The user logs into Pantio through Auth0.
2. The frontend calls `POST /api/users/ensure`.
3. The user enters the **Netto account email** in the frontend.
4. The frontend starts a PKCE redirect flow against DSG.
5. DSG shows its own login page and collects the password there.
6. DSG redirects back to the Pantio frontend with an authorization code.
7. The frontend posts the code and PKCE verifier to the backend.
8. The backend stores tokens and can then run manual sync.

## DSG / Netto Auth Parameters

Current important values:

- `clientId=customer-program`
- `tenantId=4`
- `channel=CustomerProgram`
- `clientFlow=gigya`
- `redirect_uri=http://localhost:3000/` in local development

The frontend also sends:

- `code_challenge`
- `code_challenge_method=S256`
- `emailOrPhone`
- `state`
- `nonce`
- `clientTraceId`

## Backend Token Exchange

The backend exchanges the authorization code at:

```text
https://idp.dsgapps.dk/token
```

Stored fields on `store_connections`:

- `access_token`
- `refresh_token`
- `id_token`
- `token_expires_at`
- `connected_at`
- `last_polled_at`

## Receipt Import Flow

Manual sync currently does this:

1. Load the user's `StoreConnection`.
2. Refresh DSG tokens if they are near expiry.
3. Fetch receipt summaries from:

```text
GET https://p-club.dsgapps.dk/api/cp/receipt
```

4. Skip receipts already present by `dsg_receipt_id`.
5. Fetch details for missing receipts from:

```text
GET https://p-club.dsgapps.dk/api/cp/receipt/details
```

6. Persist `Receipt` and `ReceiptLine` rows.
7. Mark `last_polled_at`.

## Confirmed Imported Data

Receipt headers currently include:

- DSG receipt id
- store name
- receipt type
- total amount in DKK
- created timestamp
- imported timestamp

Receipt lines currently include:

- `article_description`
- `ean`
- `sales_price_dkk`
- `normal_price_dkk`
- `discount_dkk`
- `qty_in_sales_unit`
- `tax_amount_dkk`
- `item_type`
- `discounts` JSON

Confirmed item types seen in data:

- `01` = product
- `02` = pant / deposit

## Implementation Notes

- The backend surface is chain-generic, but only `Netto` is supported.
- Import is idempotent at the receipt level through `dsg_receipt_id`.
- During implementation, DSG receipt detail responses turned out to have multiple response shapes.
- The detail parser now unwraps nested `lineItems` payloads instead of assuming a single fixed top-level JSON shape.

## Current Limitations

- Sync is manual only.
- Background polling is not implemented.
- Connection health and failure state are not implemented.
- Receipt-to-inventory processing is still incomplete and should not be treated as part of the trusted Netto receipt import feature.

## Useful Validation SQL

```sql
select
  (select count(*) from receipts where user_id = :user_id) as receipts,
  (select count(*) from receipt_lines rl join receipts r on r.id = rl.receipt_id where r.user_id = :user_id) as receipt_lines,
  (select last_polled_at from store_connections where user_id = :user_id and chain = 'Netto' limit 1) as last_polled_at;
```

```sql
select
  r.dsg_receipt_id,
  rl.article_description,
  rl.ean,
  rl.sales_price_dkk,
  rl.qty_in_sales_unit,
  rl.item_type
from receipt_lines rl
join receipts r on r.id = rl.receipt_id
where r.user_id = :user_id
limit 30;
```
