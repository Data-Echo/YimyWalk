[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',
    [string]$OutputPath = 'artifacts\publish\yimywalk-win-x64',
    [string]$DotnetPath,
    [ValidatePattern('^\d+\.\d+\.\d+$')]
    [string]$Version = '0.1.2'
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$build = Join-Path $PSScriptRoot 'build-yimywalk.ps1'
if (-not (Test-Path -LiteralPath $build -PathType Leaf)) {
    throw "Build script not found: $build"
}

$output = if ([IO.Path]::IsPathRooted($OutputPath)) {
    [IO.Path]::GetFullPath($OutputPath)
} else {
    [IO.Path]::GetFullPath((Join-Path $root $OutputPath))
}
& $build -Configuration $Configuration -PublishPath $output -DotnetPath $DotnetPath -Version $Version
if ($LASTEXITCODE -ne 0) {
    throw "YimyWalk publish failed with exit code $LASTEXITCODE."
}

Write-Host "Release package: $output"
