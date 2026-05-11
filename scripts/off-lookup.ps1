# OFF Barcode Lookup — mirrors what Pantio's GET /api/products/{ean} returns
# Usage: .\off-lookup.ps1 <EAN>
# Example: .\off-lookup.ps1 3017620422003

param(
    [Parameter(Mandatory)]
    [string]$Ean
)

Write-Host ""
Write-Host "  Looking up EAN: $Ean" -ForegroundColor Cyan
Write-Host "  Querying OpenFoodFacts..." -ForegroundColor DarkGray
Write-Host ""

$url = "https://world.openfoodfacts.org/api/v2/product/$Ean.json?fields=product_name,categories_tags,nutriments"

try {
    $response = Invoke-RestMethod $url -ErrorAction Stop
}
catch {
    Write-Host "  [ERROR] Could not reach OpenFoodFacts API." -ForegroundColor Red
    exit 1
}

if ($response.status -ne 1) {
    Write-Host "  [404] Product not found in OpenFoodFacts." -ForegroundColor Yellow
    exit 0
}

$p = $response.product
$n = $p.nutriments

Write-Host "  +-----------------------------------------+" -ForegroundColor Green
Write-Host "  | PRODUCT FOUND                           |" -ForegroundColor Green
Write-Host "  +-----------------------------------------+" -ForegroundColor Green
Write-Host ""
Write-Host "  Name       : $($p.product_name)" -ForegroundColor White
Write-Host ""
Write-Host "  Categories :" -ForegroundColor White
foreach ($tag in $p.categories_tags) {
    Write-Host "               $tag" -ForegroundColor Gray
}
Write-Host ""

if ($null -ne $n) {
    Write-Host "  Nutrition (per 100g):" -ForegroundColor White
    Write-Host ("  {0,-22} {1}" -f "Energy (kcal):", "$($n.'energy-kcal_100g') kcal") -ForegroundColor Gray
    Write-Host ("  {0,-22} {1}" -f "Carbohydrates:", "$($n.carbohydrates_100g) g") -ForegroundColor Gray
    Write-Host ("  {0,-22} {1}" -f "  of which sugars:", "$($n.sugars_100g) g") -ForegroundColor Gray
    Write-Host ("  {0,-22} {1}" -f "Fat:", "$($n.fat_100g) g") -ForegroundColor Gray
    Write-Host ("  {0,-22} {1}" -f "  of which saturated:", "$($n.'saturated-fat_100g') g") -ForegroundColor Gray
    Write-Host ("  {0,-22} {1}" -f "Protein:", "$($n.proteins_100g) g") -ForegroundColor Gray
    Write-Host ("  {0,-22} {1}" -f "Salt:", "$($n.salt_100g) g") -ForegroundColor Gray
} else {
    Write-Host "  Nutrition  : not available" -ForegroundColor DarkGray
}

Write-Host ""
Write-Host "  (This is exactly what Pantio caches and returns from GET /api/products/$Ean)" -ForegroundColor DarkGray
Write-Host ""
