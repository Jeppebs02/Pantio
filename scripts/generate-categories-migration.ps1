# Reads docs/english_categories.txt, assigns Danish-standard shelf life by keyword rules,
# and writes a hand-crafted EF migration to seed all categories into product_categories.

$root = Split-Path $PSScriptRoot -Parent
$inputFile  = Join-Path $root "docs\english_categories.txt"
$outputFile = Join-Path $root "backend\PantioRepository\EntityFramework\EFMigrations\20260522120000_SeedAllProductCategories.cs"

# ── Existing off_tags (ids 1-24) — skip these to avoid duplicates ──────────────
$existingOffTags = @(
    "en:fresh-meats", "en:fresh-fish", "en:milks", "en:yogurts", "en:cheeses",
    "en:eggs", "en:dairy", "en:fresh-vegetables", "en:fresh-fruits", "en:fresh-bread",
    "en:cooked-meats", "en:bread", "en:beverages", "en:juices", "en:sauces",
    "en:condiments", "en:biscuits-and-cakes", "en:chocolate", "en:frozen-foods",
    "en:canned-foods", "en:pasta", "en:rice", "en:cereals", "en:oils"
)

# ── Shelf-life keyword rules (priority order, Danish/European standards) ────────
function Get-ShelfLife([string]$name) {
    $n = $name.ToLower()
    # Frozen always wins — shelf life is dominated by freezing regardless of ingredient
    if ($n -match "\bfrozen\b|ice cream|sorbet|gelato")                             { return 180 }
    if ($n -match "fresh fish|raw fish|sashimi|sushi")                              { return 2   }
    if ($n -match "fresh meat|raw meat|fresh poultry")                              { return 4   }
    if ($n -match "\bfresh\b|raw dough")                                            { return 4   }
    if ($n -match "yogurt|yoghurt|fromage frais|quark|kefir|\bcurd\b")             { return 14  }
    if ($n -match "\bmilk\b|\bcream\b")                                             { return 7   }
    if ($n -match "\bdairy\b")                                                      { return 7   }
    if ($n -match "\bcheese\b|\bcheeses\b")                                         { return 21  }
    if ($n -match "\bbutter\b|margarine")                                           { return 21  }
    if ($n -match "\begg\b|\beggs\b")                                               { return 28  }
    if ($n -match "fresh bread|brioche|croissant|baguette")                        { return 3   }
    if ($n -match "\bbread\b|\brolls?\b|\bbuns?\b|\bmuffin\b|bakery|\bpastry\b")   { return 7   }
    if ($n -match "canned|tinned|preserved|pickled|\bpickle\b|conserve")            { return 730 }
    if ($n -match "\bpasta\b|\brice\b|\bflour\b|\bgrain\b|\boats?\b|\bbarley\b|\bwheat\b|\blegume\b|\blentil\b|\bpulse\b|\bbean\b|\bdried\b") { return 730 }
    if ($n -match "\bcereal\b|\bcereals\b")                                         { return 365 }
    if ($n -match "\bspice\b|\bspices\b|\bherb\b|\bherbs\b|seasoning|\bsalt\b|\bpepper\b|cumin|turmeric") { return 730 }
    if ($n -match "\boil\b|\boils\b")                                               { return 365 }
    if ($n -match "vinegar")                                                        { return 730 }
    if ($n -match "\bsauce\b|\bsauces\b|ketchup|mustard|mayonnaise|dressing|condiment") { return 180 }
    if ($n -match "chocolate")                                                      { return 180 }
    if ($n -match "candy|confection|\bsweet\b|\bsweets\b|toffee|caramel|nougat|liquorice|licorice") { return 180 }
    if ($n -match "biscuit|cookie|cracker|\bwafer\b|pretzel|crispbread|shortbread|gingerbread|breadstick") { return 90 }
    if ($n -match "\bcake\b|\bcakes\b")                                             { return 90  }
    if ($n -match "\bsnack\b|\bchip\b|\bcrisp\b|\bpopcorn\b")                      { return 90  }
    if ($n -match "\bnut\b|\bnuts\b|\bseed\b|\bseeds\b")                            { return 180 }
    if ($n -match "\bjuice\b|\bjuices\b|smoothie|nectar")                           { return 7   }
    if ($n -match "beverage|drink|\bsoda\b|\bcola\b|lemonade|\bbeer\b|\bwine\b|spirits|alcohol|\bwater\b") { return 30 }
    if ($n -match "\btea\b|\bcoffee\b")                                             { return 365 }
    if ($n -match "\bjam\b|\bjelly\b|marmalade|spread|\bhoney\b|syrup|compote")     { return 365 }
    if ($n -match "vegetable|vegetables|\bfruit\b|\bfruits\b|\bsalad\b|tomato|mushroom|lettuce|spinach|carrot") { return 5 }
    if ($n -match "\bbaby\b|infant")                                                { return 365 }
    return 365  # default (non-food, ambiguous)
}

# ── Derive off_tag from English display name ────────────────────────────────────
function Get-OffTag([string]$name) {
    $tag = $name.ToLower()
    $tag = $tag -replace "[''`']", ""          # remove apostrophes
    $tag = $tag -replace "[^a-z0-9\s\-]", ""  # keep only alphanum, spaces, hyphens
    $tag = $tag.Trim()
    $tag = $tag -replace "\s+", "-"            # spaces → hyphens
    $tag = $tag -replace "-+", "-"             # collapse multiple hyphens
    return "en:$tag"
}

# ── Read and process input ──────────────────────────────────────────────────────
$lines = Get-Content $inputFile -Encoding UTF8
$seenTags   = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
foreach ($tag in $existingOffTags) { $seenTags.Add($tag) | Out-Null }

$rows = [System.Collections.Generic.List[pscustomobject]]::new()
$nextId = 25

foreach ($line in $lines) {
    $line = $line.Trim()
    if ($line -eq "" -or $line.StartsWith("#")) { continue }

    $offTag = Get-OffTag $line
    if (-not $seenTags.Add($offTag)) { continue }  # duplicate — skip

    $shelfLife   = Get-ShelfLife $line
    $displayName = $line -replace '\\', '\\\\'       # escape backslashes first
    $displayName = $displayName -replace '"', '\"'   # escape double quotes for C# string literals

    $rows.Add([pscustomobject]@{
        Id          = $nextId
        ShelfLife   = $shelfLife
        DisplayName = $displayName
        OffTag      = $offTag
    })
    $nextId++
}

Write-Host "Generating migration with $($rows.Count) new categories (ids 25 - $($nextId-1))..."

# ── Build migration C# ─────────────────────────────────────────────────────────
$sb = [System.Text.StringBuilder]::new()

$sb.AppendLine("using Microsoft.EntityFrameworkCore.Migrations;") | Out-Null
$sb.AppendLine("") | Out-Null
$sb.AppendLine("#nullable disable") | Out-Null
$sb.AppendLine("") | Out-Null
$sb.AppendLine("#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional") | Out-Null
$sb.AppendLine("") | Out-Null
$sb.AppendLine("namespace PantioRepository.EntityFramework.EFMigrations") | Out-Null
$sb.AppendLine("{") | Out-Null
$sb.AppendLine("    /// <inheritdoc />") | Out-Null
$sb.AppendLine("    public partial class SeedAllProductCategories : Migration") | Out-Null
$sb.AppendLine("    {") | Out-Null
$sb.AppendLine("        /// <inheritdoc />") | Out-Null
$sb.AppendLine("        protected override void Up(MigrationBuilder migrationBuilder)") | Out-Null
$sb.AppendLine("        {") | Out-Null
$sb.AppendLine("            migrationBuilder.InsertData(") | Out-Null
$sb.AppendLine('                table: "product_categories",') | Out-Null
$sb.AppendLine('                columns: new[] { "id", "default_shelf_life_days", "display_name", "off_tag" },') | Out-Null
$sb.AppendLine("                values: new object[,]") | Out-Null
$sb.AppendLine("                {") | Out-Null

for ($i = 0; $i -lt $rows.Count; $i++) {
    $r     = $rows[$i]
    $comma = if ($i -lt $rows.Count - 1) { "," } else { "" }
    $sb.AppendLine("                    { $($r.Id), $($r.ShelfLife), `"$($r.DisplayName)`", `"$($r.OffTag)`" }$comma") | Out-Null
}

$sb.AppendLine("                });") | Out-Null
$sb.AppendLine("        }") | Out-Null
$sb.AppendLine("") | Out-Null
$sb.AppendLine("        /// <inheritdoc />") | Out-Null
$sb.AppendLine("        protected override void Down(MigrationBuilder migrationBuilder)") | Out-Null
$sb.AppendLine("        {") | Out-Null
$sb.AppendLine('            migrationBuilder.Sql("DELETE FROM product_categories WHERE id >= 25");') | Out-Null
$sb.AppendLine("        }") | Out-Null
$sb.AppendLine("    }") | Out-Null
$sb.AppendLine("}") | Out-Null

[System.IO.File]::WriteAllText($outputFile, $sb.ToString(), [System.Text.Encoding]::UTF8)
Write-Host "Written to: $outputFile"
