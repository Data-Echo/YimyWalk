[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',
    [string]$PublishPath
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$dotnet = Join-Path $root '.tools\dotnet\dotnet.exe'
if (-not (Test-Path $dotnet)) { throw "Workspace SDK not found: $dotnet" }

Push-Location $root
try {
    & $dotnet restore YimyWalk.sln --runtime win-x64
    if ($LASTEXITCODE -ne 0) { throw "dotnet restore failed with exit code $LASTEXITCODE" }
    & $dotnet test YimyWalk.sln --configuration $Configuration --no-restore
    if ($LASTEXITCODE -ne 0) { throw "dotnet test failed with exit code $LASTEXITCODE" }

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

    & $dotnet publish src\YimyWalk.App\YimyWalk.App.csproj --configuration $Configuration --runtime win-x64 --self-contained false --no-restore -p:Platform=x64 -p:PublishDir="$publish\"
    if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed with exit code $LASTEXITCODE" }

    $characters = Join-Path $publish 'Characters'
    if (Test-Path $characters) { Remove-Item -LiteralPath $characters -Recurse -Force }
    Copy-Item -LiteralPath (Join-Path $root 'Characters') -Destination $characters -Recurse

    Copy-Item -LiteralPath (Join-Path $root 'THIRD_PARTY_NOTICES.md') -Destination (Join-Path $publish 'THIRD_PARTY_NOTICES.md') -Force
    $packageReadme = @"
YimyWalk offline Windows x64 package

Run YimyWalk.App.exe from this directory. This framework-dependent package requires:
- .NET 8 Microsoft.NETCore.App (x64)
- .NET 8 Microsoft.WindowsDesktop.App (x64)

Characters/<role-id>/sprite.png is the required role format. The package does not
enable networking, startup registration, or administrator privileges.
Third-party license and attribution information is in THIRD_PARTY_NOTICES.md.
"@
    [IO.File]::WriteAllText((Join-Path $publish 'PACKAGE_README.txt'), $packageReadme.TrimStart(), [Text.UTF8Encoding]::new($false))

    $roles = @(Get-ChildItem -LiteralPath $characters -Directory | Sort-Object Name | ForEach-Object { $_.Name })
    $manifest = [ordered]@{
        formatVersion = 1
        product = 'YimyWalk'
        architecture = 'win-x64'
        targetFramework = 'net8.0-windows'
        selfContained = $false
        requiredRuntimes = @('Microsoft.NETCore.App 8.0', 'Microsoft.WindowsDesktop.App 8.0')
        roles = $roles
        licenseFiles = @('THIRD_PARTY_NOTICES.md')
        networkEnabledByDefault = $false
        startupRegistrationEnabled = $false
        requiresAdministrator = $false
    }
    $manifestPath = Join-Path $publish 'PACKAGE_MANIFEST.json'
    $manifest | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath $manifestPath -Encoding utf8

    $requiredFiles = @(
        'YimyWalk.App.exe', 'YimyWalk.App.dll', 'YimyWalk.App.deps.json',
        'YimyWalk.App.runtimeconfig.json', 'YimyWalk.Core.dll',
        'SixLabors.ImageSharp.dll', 'THIRD_PARTY_NOTICES.md',
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
