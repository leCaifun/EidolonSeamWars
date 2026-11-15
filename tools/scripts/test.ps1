# Запуск тестов solution. Запуск: pwsh tools/scripts/test.ps1 [-Configuration Release]
[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)

Push-Location $root
try {
    Write-Host "Тесты EidolonSeamWars.sln ($Configuration)..."
    & dotnet test 'EidolonSeamWars.sln' -c $Configuration --logger 'console;verbosity=normal'
    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }
}
finally {
    Pop-Location
}
