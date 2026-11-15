# Checks localization files:
# - compares all language files against en.json
# - checks languages.json registration
# - reports missing/extra keys
# Required languages (ru, en) must match 100%: difference is an error.
# Missing keys in other languages are warnings (ADR-026); extra keys are errors.

$ErrorActionPreference = "Stop"

function Read-Json {
    param([string]$Path)

    return (Get-Content -Raw -Encoding UTF8 -LiteralPath $Path | ConvertFrom-Json)
}

function Read-Entries {
    param([string]$Path)

    $json = Read-Json -Path $Path

    if (-not $json.entries) {
        throw "Missing entries field in $Path"
    }

    return @($json.entries.PSObject.Properties.Name)
}

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$locDir = Join-Path $scriptDir "../../game/content/core/localization"
$locDir = [System.IO.Path]::GetFullPath($locDir)

$enFile = Join-Path $locDir "en.json"
$ruFile = Join-Path $locDir "ru.json"
$languagesFile = Join-Path $locDir "languages.json"

if (-not (Test-Path -LiteralPath $enFile)) {
    throw "en.json not found: $enFile"
}

if (-not (Test-Path -LiteralPath $ruFile)) {
    throw "ru.json not found: $ruFile"
}

if (-not (Test-Path -LiteralPath $languagesFile)) {
    throw "languages.json not found: $languagesFile"
}

$enKeys = Read-Entries -Path $enFile
$ruKeys = Read-Entries -Path $ruFile

Write-Host ""
Write-Host "=== Check ru vs en key sets ==="

$ruEnDiff = Compare-Object -ReferenceObject $enKeys -DifferenceObject $ruKeys

if ($ruEnDiff) {
    $allOk = $false
    Write-Host "ERROR: ru and en key sets differ" -ForegroundColor Red

    foreach ($item in $ruEnDiff) {
        if ($item.SideIndicator -eq "=>") {
            Write-Host "  in ru but not in en: $($item.InputObject)"
        } else {
            Write-Host "  in en but not in ru: $($item.InputObject)"
        }
    }
} else {
    Write-Host "OK: ru and en key sets match" -ForegroundColor Green
}

Write-Host ""
Write-Host "=== Check language files against en ==="

$allOk = $true

Get-ChildItem -LiteralPath $locDir -Filter *.json | Where-Object {
    $_.Name -ne "languages.json" -and $_.Name -ne "en.json"
} | ForEach-Object {
    $path = $_.FullName

    try {
        $json = Read-Json -Path $path
    } catch {
        $allOk = $false
        Write-Host "FAIL: $($_.Name) - $($_.Exception.Message)" -ForegroundColor Red
        return
    }

    if (-not ($json.PSObject.Properties.Name -contains "language")) {
        $allOk = $false
        Write-Host "FAIL: $($_.Name) missing language field" -ForegroundColor Red
        return
    }

    if (-not ($json.PSObject.Properties.Name -contains "status")) {
        Write-Host "WARN: $($_.Name) missing status field" -ForegroundColor Yellow
    }

    if (-not ($json.PSObject.Properties.Name -contains "fallback")) {
        Write-Host "WARN: $($_.Name) missing fallback field" -ForegroundColor Yellow
    }

    try {
        $keys = Read-Entries -Path $path
        $diff = Compare-Object -ReferenceObject $enKeys -DifferenceObject $keys

        $missing = @()
        $extra = @()

        if ($diff) {
            $missing = @($diff | Where-Object { $_.SideIndicator -eq "<=" } | ForEach-Object { $_.InputObject })
            $extra = @($diff | Where-Object { $_.SideIndicator -eq "=>" } | ForEach-Object { $_.InputObject })
        }

        if ($missing.Count -gt 0 -or $extra.Count -gt 0) {
            Write-Host ""
            Write-Host "NOTE: $($_.Name) language=$($json.language) status=$($json.status)"

            foreach ($key in $missing) {
                Write-Host "  warning: missing $key" -ForegroundColor Yellow
            }

            foreach ($key in $extra) {
                $allOk = $false
                Write-Host "  error: extra $key" -ForegroundColor Red
            }
        } else {
            Write-Host "OK: $($_.Name) language=$($json.language) status=$($json.status)" -ForegroundColor Green
        }
    } catch {
        $allOk = $false
        Write-Host "FAIL: $($_.Name) - $($_.Exception.Message)" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "=== Check languages.json registration ==="

try {
    $languagesJson = Read-Json -Path $languagesFile
} catch {
    Write-Host "FAIL: languages.json - $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

if (-not $languagesJson.languages) {
    throw "languages.json missing languages array"
}

$registered = @($languagesJson.languages.code)

$fileLanguages = @()

Get-ChildItem -LiteralPath $locDir -Filter *.json | Where-Object {
    $_.Name -ne "languages.json"
} | ForEach-Object {
    try {
        $json = Read-Json -Path $_.FullName
    } catch {
        $allOk = $false
        Write-Host "FAIL: $($_.Name) - $($_.Exception.Message)" -ForegroundColor Red
        return
    }

    if (-not ($json.PSObject.Properties.Name -contains "language")) {
        $allOk = $false
        Write-Host "FAIL: $($_.Name) missing language field" -ForegroundColor Red
        return
    }

    $fileLanguages += $json.language
}

$registrationDiff = Compare-Object -ReferenceObject $registered -DifferenceObject $fileLanguages

if ($registrationDiff) {
    $allOk = $false

    $registeredWithoutFile = @($registrationDiff | Where-Object { $_.SideIndicator -eq "<=" } | ForEach-Object { $_.InputObject })
    $fileWithoutRegistration = @($registrationDiff | Where-Object { $_.SideIndicator -eq "=>" } | ForEach-Object { $_.InputObject })

    if ($registeredWithoutFile.Count -gt 0) {
        Write-Host "Registered languages without files:" -ForegroundColor Red

        foreach ($code in $registeredWithoutFile) {
            Write-Host "  $code"
        }
    }

    if ($fileWithoutRegistration.Count -gt 0) {
        Write-Host "Language files without registration in languages.json:" -ForegroundColor Red

        foreach ($code in $fileWithoutRegistration) {
            Write-Host "  $code"
        }
    }
} else {
    Write-Host "OK: languages.json matches language files" -ForegroundColor Green
}

Write-Host ""

if ($allOk) {
    Write-Host "Localization structure check passed." -ForegroundColor Green
    exit 0
} else {
    Write-Host "Localization structure check failed." -ForegroundColor Red
    exit 1
}