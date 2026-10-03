param(
    [string]$InstallDir = (Join-Path (Split-Path $PSScriptRoot -Parent) '.tools\dotnet'),
    [string]$Channel = '8.0'
)

$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
$tools = Join-Path $repo '.tools'
$installer = Join-Path $tools 'dotnet-install.ps1'
New-Item -ItemType Directory -Path $tools -Force | Out-Null
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
Invoke-WebRequest -Uri 'https://dot.net/v1/dotnet-install.ps1' -OutFile $installer
& $installer -Channel $Channel -InstallDir $InstallDir -NoPath
if ($LASTEXITCODE -ne 0) { throw "dotnet SDK installation failed with exit code $LASTEXITCODE." }
& (Join-Path $InstallDir 'dotnet.exe') --version
