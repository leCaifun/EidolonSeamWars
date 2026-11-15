# Сборка solution. Запуск: pwsh tools/scripts/build.ps1 [-Configuration Release]
[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)

Push-Location $root
try {
    Write-Host "Сборка EidolonSeamWars.sln ($Configuration)..."
    & dotnet build 'EidolonSeamWars.sln' -c $Configuration
    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }
}
finally {
    Pop-Location
}
