# Headless-проверка Godot-проекта: сборка C#, импорт и запуск главной сцены.
# Запуск: powershell -NoProfile -ExecutionPolicy Bypass -File tools/scripts/run-headless.ps1
# Путь к Godot .NET: параметр -GodotPath или переменная окружения ESW_GODOT.
[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',

    [string]$GodotPath = $env:ESW_GODOT,

    [switch]$SkipBuild
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$project = Join-Path $root 'game'

if ([string]::IsNullOrWhiteSpace($GodotPath)) {
    $GodotPath = 'godot'
}

if (-not $SkipBuild) {
    Write-Host "Сборка Godot-слоя ($Configuration)..."
    & dotnet build (Join-Path $project 'EidolonSeamWars.csproj') -c $Configuration
    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }
}

Write-Host 'Импорт проекта без редактора...'
& $GodotPath --headless --path $project --editor --quit
if ($LASTEXITCODE -ne 0) {
    Write-Error "Импорт Godot завершился с кодом $LASTEXITCODE. Проверьте docs/TOOLCHAIN.md."
    exit $LASTEXITCODE
}

Write-Host 'Запуск главной сцены в headless-режиме...'
& $GodotPath --headless --path $project --quit
if ($LASTEXITCODE -ne 0) {
    Write-Error "Запуск Godot завершился с кодом $LASTEXITCODE."
    exit $LASTEXITCODE
}

Write-Host 'Headless-проверка завершена успешно.'
exit 0
