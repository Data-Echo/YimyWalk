[CmdletBinding()]
param([string]$LogPath, [switch]$InventoryOnly, [ValidateRange(1, 100)][int]$Rounds = 3)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$dotnet = Join-Path $root '.tools\dotnet\dotnet.exe'
if (-not $LogPath) { $LogPath = Join-Path $root ('artifacts\dual-display-' + (Get-Date -Format 'yyyyMMdd-HHmmss') + '.log') }
$LogPath = [IO.Path]::GetFullPath($LogPath)
[IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($LogPath)) | Out-Null
Push-Location $root
try {
    & $dotnet build tools\DesktopDisplaySmoke\DesktopDisplaySmoke.csproj -c Release -p:Platform=x64
    if ($LASTEXITCODE -ne 0) { throw 'DesktopDisplaySmoke build failed.' }
    $argsList = @((Join-Path $root 'Characters'))
    if ($InventoryOnly) { $argsList += '--inventory' }
    else { $argsList += @('--rounds', $Rounds.ToString()) }
    & $dotnet (Join-Path $root 'tools\DesktopDisplaySmoke\bin\x64\Release\net8.0-windows\DesktopDisplaySmoke.dll') @argsList | Tee-Object -FilePath $LogPath
    $result = $LASTEXITCODE
    Write-Host "Desktop display evidence: $LogPath"
    exit $result
} finally { Pop-Location }
