# ============================================================
#  Pantio - Shopping List Feature Demo
#
#  Prerequisites:
#    1. Start backend + postgres:  docker compose --env-file .env.dev up -d
#    2. Run this script from the repo root
#
#  Covers both flows:
#    FLOW A - Manual shopping list (create, add, duplicate-merge, toggle, delete items)
#    FLOW B - Shopping list from recipe (missing ingredients only)
# ============================================================

$ErrorActionPreference = "Stop"

$BaseUrl     = "http://localhost:5000"
$PgContainer = "postgres_dev"
$PgUser      = "pantio"
$PgDb        = "pantio_dev"
$AccessToken = "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCIsImtpZCI6InZ2dU56Q3RFQ2ZKSWJnN0tBekt0byJ9.eyJpc3MiOiJodHRwczovL2Rldi1vZ2l0aGR6empqYWZoZHd1LmV1LmF1dGgwLmNvbS8iLCJzdWIiOiJhdXRoMHw2OWZjNTM3YzYzYzQzMWExZGUxZWNkNjciLCJhdWQiOlsiaHR0cHM6Ly9hcGkucGFudGlvLmNvbSIsImh0dHBzOi8vZGV2LW9naXRoZHp6amphZmhkd3UuZXUuYXV0aDAuY29tL3VzZXJpbmZvIl0sImlhdCI6MTc3ODQzODY2MCwiZXhwIjoxNzc4NTI1MDYwLCJzY29wZSI6Im9wZW5pZCBwcm9maWxlIGVtYWlsIiwiYXpwIjoiZldmcUhrUU5ERlJvWWdveU5MaXRmeUlnSlluY3JHYm4iLCJwZXJtaXNzaW9ucyI6W119.OOjoaXqqk2mx-FqtzAh5U0NeNY8DWmja7Shz3aVOCBNqddWL4hHEkl5154eNxztto5NiXYAYqZrJtcDcvNz9fSEJ0YLU_arvz1paluL9mz9aYz7X4xYdu8DlW9UYUF4jwmtbGxMINc2gow2RRwps08_jkzQP4_TmjXVy0krcE2vCqezxKTUH7e-h957-__N_LL3Y6IW5os0nDUIoY3veVn6UwI3ixdRJlbd46RnYjsasK_Ek5DV9KYAZMbjyhCjAbMrzePMKyLiEZj6sFQKChINuqkQQtZV5akY950i6GUC81F-P6aofrO0HqtDebLF0oDx9gF2YebPvy3gD1fp0hg"
$UserId      = "ee497787-c7a3-494f-92ae-96aa67e03933"

function Write-Header($msg) {
    Write-Host ""
    Write-Host "  $msg" -ForegroundColor Cyan
    Write-Host "  $("-" * $msg.Length)" -ForegroundColor DarkGray
}
function Write-Ok($msg)   { Write-Host "  [OK] $msg" -ForegroundColor Green }
function Write-Err($msg)  { Write-Host "  [!!] $msg" -ForegroundColor Red   }
function Write-Info($msg) { Write-Host "  $msg"       -ForegroundColor Gray  }

function Invoke-Api($method, $path, $body = $null) {
    $params = @{
        Method      = $method
        Uri         = "$BaseUrl$path"
        ContentType = "application/json"
        Headers     = @{ Authorization = "Bearer $AccessToken" }
    }
    if ($body) { $params.Body = ($body | ConvertTo-Json -Depth 5) }
    try {
        return Invoke-RestMethod @params
    } catch {
        Write-Err "API call failed: $method $path"
        Write-Err $_.Exception.Message
        try {
            $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
            Write-Err "Response body: $($reader.ReadToEnd())"
        } catch {}
        exit 1
    }
}

function Invoke-Sql($query) {
    $out = docker exec $PgContainer psql -U $PgUser -d $PgDb -t -c $query 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Err "SQL failed: $out"
        exit 1
    }
    return ([string]$out).Trim()
}

# ============================================================
Write-Host ""
Write-Host "  ==========================================" -ForegroundColor Magenta
Write-Host "    Pantio - Shopping List Demo"              -ForegroundColor Magenta
Write-Host "  ==========================================" -ForegroundColor Magenta

# -- Step 0: Health check
Write-Header "0/7  API health check"
try {
    $health = Invoke-RestMethod -Uri "$BaseUrl/health"
    Write-Ok "API is up ($health)"
} catch {
    Write-Err "Cannot reach API at $BaseUrl"
    Write-Info "Start it:  docker compose --env-file .env.dev up -d"
    exit 1
}

# -- Step 1: Identify demo user
Write-Header "1/7  Demo user"
Write-Ok "Using existing Pantio user"
Write-Info "id    : $UserId"
Write-Info "email : matiasholmn@gmail.com"

# -- Step 2: Create inventory with a few items
Write-Header "2/7  Creating inventory and adding items"

$inventory   = Invoke-Api POST "/api/users/$UserId/inventories" @{ name = "Demo Køleskab" }
$inventoryId = $inventory.id
Write-Ok "Inventory '$($inventory.name)' created  (id: $inventoryId)"

# Intentionally add only SOME items so Flow B produces missing ones
$itemDefs = @(
    @{ productName = "Pasta";   quantity = 250; quantityUnit = "g";   ean = ""; storageLocation = $null; addedVia = "Manual" },
    @{ productName = "Løg";     quantity = 2;   quantityUnit = "stk"; ean = ""; storageLocation = $null; addedVia = "Manual" }
)

$createdItems = @()
foreach ($def in $itemDefs) {
    $item = Invoke-Api POST "/api/inventories/$inventoryId/items" $def
    $createdItems += $item
    Write-Ok "$($def.productName.PadRight(15)) $($def.quantity) $($def.quantityUnit)"
}

# -- Step 3: Get recipe suggestions (needed for Flow B)
Write-Header "3/7  Fetching recipe suggestions from Gemini (needed for Flow B)"

Write-Info "(calling Gemini, this may take a few seconds...)"
$suggestionBody = @{ inventoryItemIds = @($createdItems | ForEach-Object { $_.id }) }
$result  = Invoke-Api POST "/api/users/$UserId/recipe-suggestions" $suggestionBody
$recipes = $result.suggestions

Write-Ok "$($recipes.Count) recipes returned and saved to DB"
$chosenRecipe = $recipes[0]
Write-Info "Using recipe '$($chosenRecipe.name)'  (id: $($chosenRecipe.id))"
Write-Info "Ingredients in recipe:"
foreach ($ing in $chosenRecipe.ingredients) {
    $tag = if ($ing.inInventory) { "  [in inventory]" } else { "  [MISSING]" }
    Write-Host "    * $($ing.productName.PadRight(22)) $($ing.quantity) $($ing.measuringUnit)$tag" -ForegroundColor Gray
}

# ============================================================
Write-Host ""
Write-Host "  ==========================================" -ForegroundColor Yellow
Write-Host "    FLOW A  -  Manual shopping list"          -ForegroundColor Yellow
Write-Host "  ==========================================" -ForegroundColor Yellow

# -- Step 4: Manual shopping list
Write-Header "4/7  Creating a manual shopping list"

$manualList   = Invoke-Api POST "/api/users/$UserId/shopping-lists" @{ name = "Uge 20 indkøb" }
$manualListId = $manualList.id
Write-Ok "Shopping list '$($manualList.name)' created  (id: $manualListId)"

$manualItemDefs = @(
    @{ name = "Mælk";  quantity = 2;   measuringUnit = "L"   },
    @{ name = "Brød";  quantity = 1;   measuringUnit = "stk" },
    @{ name = "Ost";   quantity = 200; measuringUnit = "g"   }
)

$manualItems = @()
foreach ($def in $manualItemDefs) {
    $item = Invoke-Api POST "/api/users/$UserId/shopping-lists/$manualListId/items" $def
    $manualItems += $item
    Write-Ok "Added: $($def.name.PadRight(12)) $($def.quantity) $($def.measuringUnit)"
}

# Duplicate-merge: adding "Mælk" again should sum the quantity, not create a new row
Write-Info ""
Write-Info "Adding 'Mælk' again (qty 1 L) — should merge into existing item"
Invoke-Api POST "/api/users/$UserId/shopping-lists/$manualListId/items" @{ name = "Mælk"; quantity = 1; measuringUnit = "L" } | Out-Null

$listAfterDup = Invoke-Api GET "/api/users/$UserId/shopping-lists/$manualListId"
$mælkItem     = $listAfterDup.items | Where-Object { $_.name -eq "Mælk" }

if ($listAfterDup.items.Count -eq $manualItemDefs.Count) {
    Write-Ok "No duplicate created — still $($listAfterDup.items.Count) items"
} else {
    Write-Err "Expected $($manualItemDefs.Count) items, got $($listAfterDup.items.Count) — duplicate was created!"
    exit 1
}

if ($mælkItem.quantity -eq 3) {
    Write-Ok "Quantity merged correctly: Mælk is now $($mælkItem.quantity) L"
} else {
    Write-Err "Expected Mælk quantity=3, got $($mælkItem.quantity)"
    exit 1
}

# Toggle the first item (mark as checked)
$toggleTarget = $manualItems[0]
Write-Info ""
Write-Info "Toggling '$($toggleTarget.name)' -> checked"
$toggled = Invoke-Api PATCH "/api/users/$UserId/shopping-lists/$manualListId/items/$($toggleTarget.id)/toggle"
if ($toggled.isChecked -eq $true) {
    Write-Ok "'$($toggled.name)' is now checked"
} else {
    Write-Err "Expected isChecked=true, got: $($toggled.isChecked)"
    exit 1
}

# Toggle back (uncheck)
Write-Info "Toggling '$($toggleTarget.name)' -> unchecked"
$toggled2 = Invoke-Api PATCH "/api/users/$UserId/shopping-lists/$manualListId/items/$($toggleTarget.id)/toggle"
if ($toggled2.isChecked -eq $false) {
    Write-Ok "'$($toggled2.name)' is back to unchecked"
} else {
    Write-Err "Expected isChecked=false, got: $($toggled2.isChecked)"
    exit 1
}

# Delete the last item
$deleteTarget = $manualItems[-1]
Write-Info ""
Write-Info "Deleting item '$($deleteTarget.name)'"
Invoke-Api DELETE "/api/users/$UserId/shopping-lists/$manualListId/items/$($deleteTarget.id)" | Out-Null
Write-Ok "Deleted '$($deleteTarget.name)'"

# Fetch and verify the list
$fetchedList   = Invoke-Api GET "/api/users/$UserId/shopping-lists/$manualListId"
$expectedCount = $manualItemDefs.Count - 1
Write-Host ""
Write-Host "  Current state of '$($fetchedList.name)':" -ForegroundColor White
foreach ($i in $fetchedList.items) {
    $checked = if ($i.isChecked) { "[x]" } else { "[ ]" }
    Write-Host "    $checked $($i.name.PadRight(15)) $($i.quantity) $($i.measuringUnit)" -ForegroundColor Gray
}

if ($fetchedList.items.Count -eq $expectedCount) {
    Write-Ok "Item count correct after delete ($($fetchedList.items.Count) items)"
} else {
    Write-Err "Expected $expectedCount items, got $($fetchedList.items.Count)"
    exit 1
}

# ============================================================
Write-Host ""
Write-Host "  ==========================================" -ForegroundColor Yellow
Write-Host "    FLOW B  -  Shopping list from recipe"     -ForegroundColor Yellow
Write-Host "  ==========================================" -ForegroundColor Yellow

# -- Step 5: From-recipe shopping list
Write-Header "5/7  Creating shopping list from recipe"

$fromRecipeBody = @{
    recipeId    = $chosenRecipe.id
    inventoryId = $inventoryId
    name        = "Manglende ingredienser: $($chosenRecipe.name)"
}

$recipeList   = Invoke-Api POST "/api/users/$UserId/shopping-lists/from-recipe" $fromRecipeBody
$recipeListId = $recipeList.id
Write-Ok "Shopping list '$($recipeList.name)' created  (id: $recipeListId)"

Write-Host ""
Write-Host "  Missing ingredients added to list:" -ForegroundColor White
foreach ($i in $recipeList.items) {
    Write-Host "    * $($i.name.PadRight(22)) $($i.quantity) $($i.measuringUnit)" -ForegroundColor Gray
}

# Verify that no inventory item appears in the shopping list
$inventoryNames = $createdItems | ForEach-Object { $_.productName.ToLower().Trim() }
$listNames      = $recipeList.items | ForEach-Object { $_.name.ToLower().Trim() }
$wrongItems     = @()
foreach ($listName in $listNames) {
    foreach ($invName in $inventoryNames) {
        if ($invName -like "*$listName*" -or $listName -like "*$invName*") {
            $wrongItems += $listName
        }
    }
}

if ($wrongItems.Count -eq 0) {
    Write-Ok "Correct - no inventory items appear in the missing-ingredients list"
} else {
    Write-Err "Found inventory items incorrectly added to list: $($wrongItems -join ', ')"
    exit 1
}

# -- Step 6: Get all lists for user
Write-Header "6/7  Fetching all shopping lists for user"

$allLists = Invoke-Api GET "/api/users/$UserId/shopping-lists"
Write-Ok "$($allLists.Count) shopping list(s) found"
foreach ($l in $allLists) {
    Write-Info "$($l.name.PadRight(45)) ($($l.items.Count) items)"
}

if ($allLists.Count -ne 2) {
    Write-Err "Expected 2 lists, got $($allLists.Count)"
    exit 1
}

# -- Step 7: DB verification + cleanup
Write-Header "7/7  DB verification"

$dbListCount = Invoke-Sql "SELECT COUNT(*) FROM shopping_lists WHERE user_id = '$UserId';"
Write-Ok "shopping_lists rows for user: $dbListCount (expected 2)"

$dbItemCount = Invoke-Sql "SELECT COUNT(*) FROM shopping_list_items WHERE shopping_list_id IN (SELECT id FROM shopping_lists WHERE user_id = '$UserId');"
Write-Ok "shopping_list_items rows for user: $dbItemCount"

# Delete one list and verify cascade
Write-Info ""
Write-Info "Deleting manual list and verifying cascade..."
Invoke-Api DELETE "/api/users/$UserId/shopping-lists/$manualListId" | Out-Null

$afterDelete = Invoke-Sql "SELECT COUNT(*) FROM shopping_lists WHERE user_id = '$UserId';"
if ([int]$afterDelete -eq 1) {
    Write-Ok "Manual list deleted - 1 list remains"
} else {
    Write-Err "Expected 1 list after delete, got: $afterDelete"
    exit 1
}

$orphanedItems = Invoke-Sql "SELECT COUNT(*) FROM shopping_list_items WHERE shopping_list_id = '$manualListId';"
if ([int]$orphanedItems -eq 0) {
    Write-Ok "Cascade delete confirmed - 0 orphaned items"
} else {
    Write-Err "Found $orphanedItems orphaned items after list delete!"
    exit 1
}

Write-Host ""
Write-Host "  ==========================================" -ForegroundColor Magenta
Write-Host "    Demo complete!"                           -ForegroundColor Magenta
Write-Host "  ==========================================" -ForegroundColor Magenta
Write-Host ""
