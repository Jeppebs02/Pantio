# ============================================================
#  Pantio - Recipe Suggestion Feature Demo
#
#  Prerequisites:
#    1. Start backend + postgres:  docker compose --env-file .env.dev up -d
#    2. Run this script from the repo root
# ============================================================

$ErrorActionPreference = "Stop"

$BaseUrl     = "http://localhost:5000"
$PgContainer = "postgres_dev"
$PgUser      = "pantio"
$PgDb        = "pantio_dev"

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
    }
    if ($body) { $params.Body = ($body | ConvertTo-Json -Depth 5) }
    try {
        return Invoke-RestMethod @params
    } catch {
        Write-Err "API call failed: $method $path"
        Write-Err $_.Exception.Message
        # Try to print the response body for richer error info
        try {
            $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
            $responseBody = $reader.ReadToEnd()
            Write-Err "Response body: $responseBody"
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
    return $out
}

# ============================================================
Write-Host ""
Write-Host "  ==========================================" -ForegroundColor Magenta
Write-Host "    Pantio - Recipe Suggestion Demo"          -ForegroundColor Magenta
Write-Host "  ==========================================" -ForegroundColor Magenta

# Step 0: Health check
Write-Header "0/6  API health check"
try {
    $health = Invoke-RestMethod -Uri "$BaseUrl/health"
    Write-Ok "API is up ($health)"
} catch {
    Write-Err "Cannot reach API at $BaseUrl"
    Write-Info "Start it:  docker compose --env-file .env.dev up -d"
    exit 1
}

# Step 1: Create test user in DB
Write-Header "1/6  Creating test user in the database"

$UserId    = [Guid]::NewGuid().ToString()
$timestamp = Get-Date -Format "yyyyMMddHHmmss"
$email     = "demo_$timestamp@pantio.test"
$phone     = "+45$(Get-Random -Minimum 10000000 -Maximum 99999999)"

Invoke-Sql "INSERT INTO users (id, email, phone_number, onboarding_done, created_at, updated_at) VALUES ('$UserId', '$email', '$phone', true, NOW(), NOW());" | Out-Null

Write-Ok "User created"
Write-Info "id    : $UserId"
Write-Info "email : $email"
Write-Info "phone : $phone"

# Step 2: Create inventory
Write-Header "2/6  Creating inventory"

$inventory   = Invoke-Api POST "/api/users/$UserId/inventories" @{ name = "Demo Koeleskab" }
$inventoryId = $inventory.id
Write-Ok "Inventory '$($inventory.name)' created  (id: $inventoryId)"

# Step 3: Add inventory items
Write-Header "3/6  Adding inventory items"

$itemDefs = @(
    @{ productName = "Kyllingefilet"; quantity = 500; quantityUnit = "g";   ean = ""; storageLocation = $null; addedVia = "Manual" },
    @{ productName = "Tomater";       quantity = 4;   quantityUnit = "stk"; ean = ""; storageLocation = $null; addedVia = "Manual" },
    @{ productName = "Pasta";         quantity = 250; quantityUnit = "g";   ean = ""; storageLocation = $null; addedVia = "Manual" },
    @{ productName = "Loeq";          quantity = 2;   quantityUnit = "stk"; ean = ""; storageLocation = $null; addedVia = "Manual" }
)

$createdItems = @()
foreach ($def in $itemDefs) {
    $item = Invoke-Api POST "/api/inventories/$inventoryId/items" $def
    $createdItems += $item
    Write-Ok "$($def.productName.PadRight(18))  $($def.quantity) $($def.quantityUnit)"
}

# Snapshot: inventory before
Write-Header "     Inventory BEFORE completing a recipe"
$before = Invoke-Api GET "/api/inventories/$inventoryId/items"
foreach ($i in $before) {
    Write-Info "$($i.productName.PadRight(20)) $($i.quantity) $($i.quantityUnit)"
}

# Step 4: Request recipe suggestions
Write-Header "4/6  Requesting recipe suggestions from Gemini AI"

Write-Info "(calling Gemini, this may take a few seconds - see API console for the full prompt...)"

$suggestionBody = @{ inventoryItemIds = @($createdItems | ForEach-Object { $_.id }) }
$result  = Invoke-Api POST "/api/users/$UserId/recipe-suggestions" $suggestionBody
$recipes = $result.suggestions

Write-Ok "$($recipes.Count) recipes returned and saved to DB"

$n = 1
foreach ($r in $recipes) {
    Write-Host ""
    Write-Host "  --- Recipe $n : $($r.name) ---" -ForegroundColor Yellow
    Write-Host "  $($r.description)" -ForegroundColor Gray
    Write-Host "  Portions : $($r.portions)   id: $($r.id)" -ForegroundColor DarkGray
    Write-Host "  Ingredients:" -ForegroundColor White
    foreach ($ing in $r.ingredients) {
        $tag = if ($ing.inventoryItemId) { " <- from inventory" } else { "" }
        Write-Host "    * $($ing.productName)  $($ing.quantity) $($ing.quantityUnit)$tag" -ForegroundColor Gray
    }
    Write-Host "  Instructions:" -ForegroundColor White
    $r.instructions.Split("`n") | ForEach-Object { Write-Host "    $_" -ForegroundColor DarkGray }
    $n++
}

# Step 5: Complete the first recipe
$chosen = $recipes[0]
Write-Header "5/6  Completing recipe: '$($chosen.name)'"

Invoke-Api POST "/api/recipes/$($chosen.id)/complete" | Out-Null
Write-Ok "Recipe marked as completed"

# Step 6: Inventory after
Write-Header "6/6  Inventory AFTER completing the recipe"

$after = Invoke-Api GET "/api/inventories/$inventoryId/items"

if ($after.Count -eq 0) {
    Write-Info "(all items were fully consumed)"
} else {
    foreach ($i in $after) {
        $prev   = $before | Where-Object { $_.id -eq $i.id }
        $change = if ($prev -and $prev.quantity -ne $i.quantity) { "  (was $($prev.quantity))" } else { "" }
        Write-Info "$($i.productName.PadRight(20)) $($i.quantity) $($i.quantityUnit)$change"
    }
}

$deleted = $before | Where-Object { $_.id -notin ($after | ForEach-Object { $_.id }) }
foreach ($d in $deleted) {
    Write-Host "  $($d.productName.PadRight(20)) DELETED (fully consumed)" -ForegroundColor Red
}

# Bonus: verify DB cleanup
Write-Header "Bonus  Verifying sibling recipes deleted from DB"

$siblings = $recipes | Where-Object { $_.id -ne $chosen.id }
foreach ($s in $siblings) {
    $count = (Invoke-Sql "SELECT COUNT(*) FROM recipes WHERE id = '$($s.id)';").Trim()
    if ($count -eq "0") {
        Write-Ok "Recipe '$($s.name)' deleted from DB"
    } else {
        Write-Err "Recipe '$($s.name)' still exists!"
    }
}

$completedAt = (Invoke-Sql "SELECT completed_at FROM recipes WHERE id = '$($chosen.id)';").Trim()
Write-Ok "completed_at = $completedAt"

Write-Host ""
Write-Host "  ==========================================" -ForegroundColor Magenta
Write-Host "    Demo complete!"                           -ForegroundColor Magenta
Write-Host "  ==========================================" -ForegroundColor Magenta
Write-Host ""
