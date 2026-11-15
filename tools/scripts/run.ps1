# Запуск SimRunner. Пример: pwsh tools/scripts/run.ps1 version
# Конфигурация задаётся переменной окружения ESW_CONFIGURATION (по умолчанию Release).
[CmdletBinding()]
param(
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$Arguments = @()
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$Configuration = if ($env:ESW_CONFIGURATION) { $env:ESW_CONFIGURATION } else { 'Release' }

Push-Location $root
try {
    & dotnet run --project 'src/Game.SimRunner/Game.SimRunner.csproj' -c $Configuration -- @Arguments
    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }
}
finally {
    Pop-Location
}
