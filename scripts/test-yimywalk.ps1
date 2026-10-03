$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
$dotnet = Join-Path $repo '.tools\dotnet\dotnet.exe'
if (-not (Test-Path -LiteralPath $dotnet)) { throw "Workspace .NET SDK not found: $dotnet" }
& $dotnet test (Join-Path $repo 'YimyWalk.sln') --configuration Release
if ($LASTEXITCODE -ne 0) { throw "YimyWalk tests failed with exit code $LASTEXITCODE." }
