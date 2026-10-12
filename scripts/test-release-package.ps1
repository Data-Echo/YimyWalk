[CmdletBinding()]
param([Parameter(Mandatory = $true)][string]$Path, [string]$Version = '0.1.5')
$ErrorActionPreference = 'Stop'
$package = (Resolve-Path -LiteralPath $Path).Path
$manifest = Get-Content -LiteralPath (Join-Path $package 'PACKAGE_MANIFEST.json') -Raw -Encoding UTF8 | ConvertFrom-Json
if ($manifest.version -ne $Version) { throw "Package version must be $Version." }
foreach ($file in @('YimyWalk.exe', 'Start-YimyWalk.ps1', 'run-yimywalk.ps1',
    'Characters/slime/sprite.png', 'Characters/咿米_Yimy/sprite.png', 'Characters/咿米_Line/sprite.png', 'THIRD_PARTY_NOTICES.md',
    'licenses/ImageSharp-LICENSE.txt', 'licenses/Apache-2.0.txt', 'RELEASE_NOTES.md', 'PACKAGE_README.txt')) {
    if (-not (Test-Path -LiteralPath (Join-Path $package $file) -PathType Leaf)) { throw "Missing: $file" }
}
if (Test-Path -LiteralPath (Join-Path $package 'Characters/cat')) { throw 'Release package must not include the built-in cat sample.' }
$executable = Get-Item -LiteralPath (Join-Path $package 'YimyWalk.exe')
$fileVersion = $executable.VersionInfo.FileVersion
if ([string]::IsNullOrWhiteSpace($fileVersion) -or $fileVersion.Split('.')[0..2] -join '.' -ne $Version) {
    throw "Executable file version does not match package version: $fileVersion"
}
$files = @(Get-ChildItem -LiteralPath $package -File -Recurse)
foreach ($file in $files) {
    $relative = $file.FullName.Substring($package.Length + 1).Replace('\', '/')
    if ($relative -match '(^|/)(tests|tools|vendor|src|obj|bin|\.git|\.tools)(/|$)|\.(pdb|cs|csproj|sln|log)$') {
        throw "Development file leaked into release: $relative"
    }
}
$listed = @{}
foreach ($line in Get-Content -LiteralPath (Join-Path $package 'PACKAGE_CHECKSUMS.txt') -Encoding UTF8) {
    if ($line -notmatch '^([0-9A-F]{64})  (.+)$') { throw "Invalid checksum line: $line" }
    $expected = $Matches[1]
    $relative = $Matches[2]
    if ([IO.Path]::IsPathRooted($relative) -or $relative -match '(^|[/\\])\.\.([/\\]|$)' -or $listed.ContainsKey($relative)) {
        throw "Unsafe or duplicate checksum path: $relative"
    }
    $actual = (Get-FileHash -LiteralPath (Join-Path $package $relative) -Algorithm SHA256).Hash
    if ($actual -ne $expected) { throw "Checksum mismatch: $relative" }
    $listed[$relative] = $true
}
if ($listed.Count -ne $files.Count - 1) { throw 'Checksum inventory does not cover the entire package.' }
Write-Output "PACKAGE PASS VERSION=$Version FILES=$($files.Count) CHECKSUMS=$($listed.Count)"
