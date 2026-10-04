[CmdletBinding()]
param(
    [ValidateRange(1, 20)][int]$Rounds = 1,
    [ValidatePattern('^[A-Za-z0-9_-]+$')][string]$Role = 'cat',
    [switch]$Landing
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$dotnet = Join-Path $root '.tools\dotnet\dotnet.exe'
Push-Location $root
try {
    & $dotnet build tools\DesktopSmoke\DesktopSmoke.csproj --configuration Release -p:Platform=x64
    if ($LASTEXITCODE -ne 0) { throw "DesktopSmoke build failed: $LASTEXITCODE" }
    $smokeArgs = @($Rounds, $Role)
    if ($Landing) { $smokeArgs += 'landing' }
    & (Join-Path $root 'tools\DesktopSmoke\bin\x64\Release\net8.0-windows\DesktopSmoke.exe') @smokeArgs
    if ($LASTEXITCODE -ne 0) { throw "DesktopSmoke failed: $LASTEXITCODE" }
}
finally { Pop-Location }
