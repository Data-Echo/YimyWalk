[CmdletBinding()]
param(
    [Parameter(Mandatory = $true, Position = 0)]
    [string]$Path,
    [string]$PreviewPath,
    [string]$ReferencePath,
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$dotnet = Join-Path $root '.tools\dotnet\dotnet.exe'
$project = Join-Path $root 'tools\CharacterValidator\CharacterValidator.csproj'
if (-not (Test-Path -LiteralPath $dotnet)) { throw "Workspace SDK not found: $dotnet. Run scripts/bootstrap-dotnet.ps1 first." }
if (-not (Test-Path -LiteralPath $project)) { throw "Character validator project not found: $project" }
$assets = Join-Path $root 'tools\CharacterValidator\obj\project.assets.json'
if (-not (Test-Path -LiteralPath $assets)) { throw "Character validator dependencies are not restored. Run scripts/build-yimywalk.ps1 or dotnet restore YimyWalk.sln first." }

$arguments = @('--project', $project, '--configuration', $Configuration, '--no-restore', '--', $Path)
if ($PreviewPath) { $arguments += @('--preview', $PreviewPath) }
if ($ReferencePath) { $arguments += @('--reference', $ReferencePath) }

Push-Location $root
try {
    & $dotnet run @arguments
    $exitCode = $LASTEXITCODE
}
finally { Pop-Location }
exit $exitCode
