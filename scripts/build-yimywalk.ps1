[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$dotnet = Join-Path $root '.tools\dotnet\dotnet.exe'
if (-not (Test-Path $dotnet)) { throw "Workspace SDK not found: $dotnet" }

Push-Location $root
try {
    & $dotnet restore YimyWalk.sln
    if ($LASTEXITCODE -ne 0) { throw "dotnet restore failed with exit code $LASTEXITCODE" }
    & $dotnet test YimyWalk.sln --configuration $Configuration --no-restore
    if ($LASTEXITCODE -ne 0) { throw "dotnet test failed with exit code $LASTEXITCODE" }
    & $dotnet publish src\YimyWalk.App\YimyWalk.App.csproj --configuration $Configuration --runtime win-x64 --self-contained false -p:Platform=x64
    if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed with exit code $LASTEXITCODE" }

    $publish = Join-Path $root "src\YimyWalk.App\bin\x64\$Configuration\net8.0-windows\win-x64\publish"
    $characters = Join-Path $publish 'Characters'
    if (Test-Path $characters) { Remove-Item -LiteralPath $characters -Recurse -Force }
    Copy-Item -LiteralPath (Join-Path $root 'Characters') -Destination $characters -Recurse
    Write-Host "Published YimyWalk to $publish"
}
finally { Pop-Location }
