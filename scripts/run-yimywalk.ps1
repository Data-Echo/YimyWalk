[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',
    [switch]$Wait,
    [string]$PublishPath
)

$ErrorActionPreference = 'Stop'
$scriptRoot = $PSScriptRoot
$releaseExecutable = Join-Path $scriptRoot 'YimyWalk.exe'
$legacyExecutable = Join-Path $scriptRoot 'YimyWalk.App.exe'
$root = if ((Test-Path -LiteralPath $releaseExecutable -PathType Leaf) -or
    (Test-Path -LiteralPath $legacyExecutable -PathType Leaf)) {
    $scriptRoot
} else {
    Split-Path -Parent $scriptRoot
}
$defaultPublish = Join-Path $root "src\YimyWalk.App\bin\x64\$Configuration\net8.0-windows\win-x64\publish"
$publish = if ([string]::IsNullOrWhiteSpace($PublishPath)) {
    if ((Test-Path -LiteralPath (Join-Path $root 'YimyWalk.exe') -PathType Leaf) -or
        (Test-Path -LiteralPath (Join-Path $root 'YimyWalk.App.exe') -PathType Leaf)) { $root } else { $defaultPublish }
} elseif ([IO.Path]::IsPathRooted($PublishPath)) {
    [IO.Path]::GetFullPath($PublishPath)
} else {
    [IO.Path]::GetFullPath((Join-Path $root $PublishPath))
}
$exe = @(
    (Join-Path $publish 'YimyWalk.exe'),
    (Join-Path $publish 'YimyWalk.App.exe')) |
    Where-Object { Test-Path -LiteralPath $_ -PathType Leaf } |
    Select-Object -First 1

function Stop-StartupDiagnostic([string]$code, [string]$message, [string]$recovery) {
    throw "[$code] $message $recovery"
}

if (-not (Test-Path -LiteralPath $publish -PathType Container)) {
    Stop-StartupDiagnostic 'PUBLISH_MISSING' "Published directory not found: $publish" 'Run scripts/publish-yimywalk.ps1 first.'
}
if ([string]::IsNullOrWhiteSpace($exe)) {
    Stop-StartupDiagnostic 'PUBLISH_INCOMPLETE' 'The published executable is missing: YimyWalk.exe' 'Rebuild the package with scripts/publish-yimywalk.ps1.'
}

$manifestPath = Join-Path $publish 'PACKAGE_MANIFEST.json'
$selfContained = $false
if (Test-Path -LiteralPath $manifestPath -PathType Leaf) {
    try { $manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json }
    catch { Stop-StartupDiagnostic 'PACKAGE_MANIFEST_INVALID' 'The publish package manifest is unreadable.' 'Rebuild the package with scripts/publish-yimywalk.ps1.' }
    $selfContained = [bool]$manifest.selfContained
}

if (-not $selfContained) {
    $runtimeConfigPath = @(
        (Join-Path $publish 'YimyWalk.runtimeconfig.json'),
        (Join-Path $publish 'YimyWalk.App.runtimeconfig.json')) |
        Where-Object { Test-Path -LiteralPath $_ -PathType Leaf } |
        Select-Object -First 1
    if ([string]::IsNullOrWhiteSpace($runtimeConfigPath)) {
        Stop-StartupDiagnostic 'RUNTIME_CONFIG_INVALID' 'The framework-dependent runtime configuration is missing.' 'Rebuild the package with scripts/publish-yimywalk.ps1.'
    }
    try { $runtimeConfig = Get-Content -LiteralPath $runtimeConfigPath -Raw | ConvertFrom-Json }
    catch { Stop-StartupDiagnostic 'RUNTIME_CONFIG_INVALID' 'The publish runtime configuration is unreadable.' 'Rebuild the package with scripts/publish-yimywalk.ps1.' }

    $dotnetCommand = Get-Command dotnet.exe -ErrorAction SilentlyContinue
    if ($null -eq $dotnetCommand) {
        Stop-StartupDiagnostic 'RUNTIME_MISSING' 'The .NET host was not found on PATH.' 'Install the .NET 8 Windows Desktop Runtime (x64), then run this script again.'
    }
    $runtimeLines = @(& $dotnetCommand.Source --list-runtimes 2>$null)
    if ($LASTEXITCODE -ne 0) {
        Stop-StartupDiagnostic 'RUNTIME_CHECK_FAILED' 'The installed .NET runtimes could not be queried.' 'Install the .NET 8 Windows Desktop Runtime (x64), then run this script again.'
    }
    foreach ($framework in @('Microsoft.NETCore.App', 'Microsoft.WindowsDesktop.App')) {
        $found = $runtimeLines | Where-Object { $_ -match "^$([regex]::Escape($framework))\s+8\." }
        if (-not $found) {
            Stop-StartupDiagnostic 'RUNTIME_MISSING' "Required runtime is missing: $framework 8.x (x64)." 'Install the .NET 8 Windows Desktop Runtime (x64), then run this script again.'
        }
    }
}

$characters = Join-Path $publish 'Characters'
if (-not (Test-Path -LiteralPath $characters -PathType Container)) {
    Stop-StartupDiagnostic 'CHARACTER_ROOT_MISSING' 'The published Characters directory is missing.' 'Rebuild the package so Characters is copied beside YimyWalk.exe.'
}
$roles = @(Get-ChildItem -LiteralPath $characters -Directory)
if ($roles.Count -eq 0) {
    Stop-StartupDiagnostic 'CHARACTERS_EMPTY' 'The published Characters directory has no role folders.' 'Add Characters/<role-id>/sprite.png and rebuild the package.'
}
$usableRoles = @($roles | Where-Object { Test-Path -LiteralPath (Join-Path $_.FullName 'sprite.png') -PathType Leaf })
if ($usableRoles.Count -eq 0) {
    Stop-StartupDiagnostic 'CHARACTERS_INVALID' 'No published role contains sprite.png.' 'Fix the role package and validate it before rebuilding.'
}
if ($usableRoles.Count -lt $roles.Count) {
    Write-Warning 'Some role folders have no sprite.png; the application will report their details while loading.'
}

if ($Wait) {
    Push-Location $publish
    try { & $exe; exit $LASTEXITCODE }
    finally { Pop-Location }
} else {
    Start-Process -FilePath $exe -WorkingDirectory $publish | Out-Null
}
