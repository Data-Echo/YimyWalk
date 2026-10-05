[CmdletBinding()]
param([switch]$Wait)

$ErrorActionPreference = 'Stop'
$runner = Join-Path $PSScriptRoot 'scripts\run-yimywalk.ps1'
if (-not (Test-Path -LiteralPath $runner -PathType Leaf)) {
    $runner = Join-Path $PSScriptRoot 'run-yimywalk.ps1'
}
if (-not (Test-Path -LiteralPath $runner -PathType Leaf)) {
    throw "YimyWalk runtime entry is missing beside $PSScriptRoot"
}
if (Test-Path -LiteralPath (Join-Path $PSScriptRoot 'YimyWalk.App.exe') -PathType Leaf) {
    & $runner -PublishPath $PSScriptRoot -Wait:$Wait
} else {
    & $runner -Wait:$Wait
}
exit $LASTEXITCODE
