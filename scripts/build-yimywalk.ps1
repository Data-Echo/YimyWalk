[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',
    [string]$PublishPath,
    [string]$DotnetPath,
    [ValidatePattern('^\d+\.\d+\.\d+$')]
    [string]$Version = '0.1.5'
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$dotnet = if (-not [string]::IsNullOrWhiteSpace($DotnetPath)) {
    [IO.Path]::GetFullPath($DotnetPath)
} elseif (Test-Path (Join-Path $root '.tools\dotnet\dotnet.exe')) {
    Join-Path $root '.tools\dotnet\dotnet.exe'
} else {
    $command = Get-Command dotnet.exe -ErrorAction SilentlyContinue
    if ($null -eq $command) { throw 'No .NET SDK found. Install .NET 8 SDK or pass -DotnetPath.' }
    $command.Source
}
if (-not (Test-Path -LiteralPath $dotnet -PathType Leaf)) { throw "dotnet executable not found: $dotnet" }

Push-Location $root
try {
    & $dotnet restore YimyWalk.sln --runtime win-x64 -p:Version=$Version -p:VersionPrefix=$Version
    if ($LASTEXITCODE -ne 0) { throw "dotnet restore failed with exit code $LASTEXITCODE" }
    $testProject = Join-Path $root 'tests\YimyWalk.Core.Tests\YimyWalk.Core.Tests.csproj'
    if (Test-Path -LiteralPath $testProject -PathType Leaf) {
        & $dotnet test YimyWalk.sln --configuration $Configuration --no-restore
        if ($LASTEXITCODE -ne 0) { throw "dotnet test failed with exit code $LASTEXITCODE" }
    } else {
        Write-Host 'Tests are not included in this product source export; skipping test project.'
    }

    $defaultPublish = Join-Path $root "src\YimyWalk.App\bin\x64\$Configuration\net8.0-windows\win-x64\publish"
    if ([string]::IsNullOrWhiteSpace($PublishPath)) { $PublishPath = $defaultPublish }
    $publish = [IO.Path]::GetFullPath($PublishPath)
    $rootWithSeparator = ([IO.Path]::GetFullPath($root)).TrimEnd('\') + '\'
    if (-not $publish.StartsWith($rootWithSeparator, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Publish path must stay inside the workspace: $publish"
    }
    $allowedPublishRoots = @(
        (Join-Path $root 'artifacts'),
        (Join-Path $root 'src\YimyWalk.App\bin')) |
        ForEach-Object { ([IO.Path]::GetFullPath($_)).TrimEnd('\') + '\' }
    if (-not ($allowedPublishRoots | Where-Object { $publish.StartsWith($_, [StringComparison]::OrdinalIgnoreCase) })) {
        throw "Publish path must be under artifacts or the App bin directory: $publish"
    }
    if (Test-Path -LiteralPath $publish) { Remove-Item -LiteralPath $publish -Recurse -Force }
    New-Item -ItemType Directory -Path $publish -Force | Out-Null

    & $dotnet publish src\YimyWalk.App\YimyWalk.App.csproj --configuration $Configuration --runtime win-x64 --self-contained true --no-restore -p:Platform=x64 -p:Version=$Version -p:VersionPrefix=$Version -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -p:PublishDir="$publish\"
    if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed with exit code $LASTEXITCODE" }

    $publishedAppHost = Join-Path $publish 'YimyWalk.App.exe'
    $releaseAppHost = Join-Path $publish 'YimyWalk.exe'
    if (-not (Test-Path -LiteralPath $publishedAppHost -PathType Leaf)) {
        throw 'Publish output is missing the YimyWalk application executable.'
    }
    if (Test-Path -LiteralPath $releaseAppHost) { Remove-Item -LiteralPath $releaseAppHost -Force }
    Move-Item -LiteralPath $publishedAppHost -Destination $releaseAppHost

    Get-ChildItem -LiteralPath $publish -File -Recurse -Filter '*.pdb' | Remove-Item -Force

    $characters = Join-Path $publish 'Characters'
    if (Test-Path $characters) { Remove-Item -LiteralPath $characters -Recurse -Force }
    Copy-Item -LiteralPath (Join-Path $root 'Characters') -Destination $characters -Recurse

    Copy-Item -LiteralPath (Join-Path $root 'THIRD_PARTY_NOTICES.md') -Destination (Join-Path $publish 'THIRD_PARTY_NOTICES.md') -Force
    Copy-Item -LiteralPath (Join-Path $root 'RELEASE_NOTES.md') -Destination (Join-Path $publish 'RELEASE_NOTES.md') -Force
    Copy-Item -LiteralPath (Join-Path $root 'Start-YimyWalk.ps1') -Destination (Join-Path $publish 'Start-YimyWalk.ps1') -Force
    Copy-Item -LiteralPath (Join-Path $root 'scripts\run-yimywalk.ps1') -Destination (Join-Path $publish 'run-yimywalk.ps1') -Force
    $licenses = Join-Path $publish 'licenses'
    if (Test-Path $licenses) { Remove-Item -LiteralPath $licenses -Recurse -Force }
    Copy-Item -LiteralPath (Join-Path $root 'licenses') -Destination $licenses -Recurse
    $packageReadme = @"
YimyWalk v$Version offline Windows x64 package

Run YimyWalk.exe from this directory. This self-contained package includes the
Windows x64 .NET runtime and does not require a separate runtime installation.
Start-YimyWalk.ps1 is retained as an optional diagnostic wrapper.

Characters/<role-id>/sprite.png is the required role format. The package does not
enable networking, startup registration, or administrator privileges.
Third-party license and attribution information is in THIRD_PARTY_NOTICES.md.
"@
    [IO.File]::WriteAllText((Join-Path $publish 'PACKAGE_README.txt'), $packageReadme.TrimStart(), [Text.UTF8Encoding]::new($false))

    $roles = @(Get-ChildItem -LiteralPath $characters -Directory | Sort-Object Name | ForEach-Object { $_.Name })
    $manifest = [ordered]@{
        formatVersion = 1
        product = 'YimyWalk'
        version = $Version
        architecture = 'win-x64'
        targetFramework = 'net8.0-windows'
        selfContained = $true
        requiredRuntimes = @()
        roles = $roles
        licenseFiles = @('THIRD_PARTY_NOTICES.md', 'licenses/ImageSharp-LICENSE.txt', 'licenses/Apache-2.0.txt')
        networkEnabledByDefault = $false
        startupRegistrationEnabled = $false
        requiresAdministrator = $false
    }
    $manifestPath = Join-Path $publish 'PACKAGE_MANIFEST.json'
    $manifest | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath $manifestPath -Encoding utf8

    $requiredFiles = @(
        'YimyWalk.exe', 'THIRD_PARTY_NOTICES.md', 'RELEASE_NOTES.md',
        'Start-YimyWalk.ps1', 'run-yimywalk.ps1', 'licenses/ImageSharp-LICENSE.txt', 'licenses/Apache-2.0.txt',
        'PACKAGE_README.txt', 'PACKAGE_MANIFEST.json')
    foreach ($file in $requiredFiles) {
        if (-not (Test-Path -LiteralPath (Join-Path $publish $file) -PathType Leaf)) {
            throw "Publish package is missing required file: $file"
        }
    }
    if ($roles.Count -eq 0 -or ($roles | Where-Object { -not (Test-Path -LiteralPath (Join-Path (Join-Path $characters $_) 'sprite.png') -PathType Leaf) }).Count -gt 0) {
        throw 'Publish package has no complete character role.'
    }

    $checksumLines = @(Get-ChildItem -LiteralPath $publish -File -Recurse |
        Where-Object { $_.Name -ne 'PACKAGE_CHECKSUMS.txt' } |
        Sort-Object FullName |
        ForEach-Object {
            $relative = $_.FullName.Substring($publish.Length).TrimStart('\') -replace '\\', '/'
            $hash = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash
            "$hash  $relative"
        })
    [IO.File]::WriteAllLines((Join-Path $publish 'PACKAGE_CHECKSUMS.txt'), $checksumLines, [Text.UTF8Encoding]::new($false))
    Write-Host "Published YimyWalk to $publish"
}
finally { Pop-Location }
