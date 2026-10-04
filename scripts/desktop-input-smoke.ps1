[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$dotnet = Join-Path $root '.tools\dotnet\dotnet.exe'
Push-Location $root
try {
    & $dotnet build tools\DesktopInputSmoke\DesktopInputSmoke.csproj --configuration Release -p:Platform=x64
    if ($LASTEXITCODE -ne 0) { throw "DesktopInputSmoke build failed: $LASTEXITCODE" }
    & (Join-Path $root 'tools\DesktopInputSmoke\bin\x64\Release\net8.0-windows\DesktopInputSmoke.exe')
    if ($LASTEXITCODE -eq 2) {
        Write-Warning 'Desktop input routing is blocked by the current interactive desktop session; GUI acceptance remains unverified.'
        exit 2
    }
    if ($LASTEXITCODE -ne 0) { throw "DesktopInputSmoke failed: $LASTEXITCODE" }
}
finally { Pop-Location }
