# Проверка контента. Запуск:
#   powershell -NoProfile -ExecutionPolicy Bypass -File tools/scripts/validate.ps1
#   powershell -NoProfile -ExecutionPolicy Bypass -File tools/scripts/validate.ps1 -Content <path>
# Скрипт вызывает реальную команду Game.SimRunner validate и возвращает её код.
[CmdletBinding()]
param(
    [string]$Content
)

$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$project = Join-Path $root 'src/Game.SimRunner/Game.SimRunner.csproj'

Write-Host '=== Проверка контента ==='
Write-Host "Проект: $project"

$arguments = @('run', '--project', $project, '--', 'validate')

if ($Content) {
    $arguments += @('--content', $Content)
} else {
    Write-Host "Каталог контента: $root/game/content/core"
}

& dotnet @arguments

exit $LASTEXITCODE
