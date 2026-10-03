[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',
    [switch]$Wait
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$exe = Join-Path $root "src\YimyWalk.App\bin\x64\$Configuration\net8.0-windows\win-x64\publish\YimyWalk.App.exe"
if (-not (Test-Path $exe)) { throw "Published executable not found. Run scripts/build-yimywalk.ps1 first." }

if ($Wait) {
    & $exe
} else {
    Start-Process -FilePath $exe -WorkingDirectory $root | Out-Null
}
