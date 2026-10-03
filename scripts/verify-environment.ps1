$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
$sdk = Join-Path $repo '.tools\dotnet\dotnet.exe'
$msbuild = 'D:\Program Files\Visual\product\MSBuild\Current\Bin\MSBuild.exe'
Write-Output "Repo: $repo"
Write-Output "OS: $((Get-CimInstance Win32_OperatingSystem).Caption) $((Get-CimInstance Win32_OperatingSystem).OSArchitecture)"
Write-Output "Git: $((git --version).Trim())"
if (Test-Path -LiteralPath $sdk) { Write-Output "Workspace .NET: $((& $sdk --version).Trim())" } else { Write-Warning "Workspace .NET SDK missing; run scripts/bootstrap-dotnet.ps1." }
if (Test-Path -LiteralPath $msbuild) { Write-Output "MSBuild: $msbuild" } else { Write-Warning "Expected desktop MSBuild was not found at $msbuild." }
if (Test-Path -LiteralPath (Join-Path $repo 'vendor\esheep')) { Write-Output "Upstream snapshot: present" } else { throw 'vendor/esheep is missing.' }
$upstreamCommit = (& git -C $repo rev-parse upstream/master 2>$null).Trim()
if ($LASTEXITCODE -eq 0) { Write-Output "Upstream commit: $upstreamCommit" } else { Write-Warning 'upstream/master is not available.' }
$systemFramework = Test-Path -LiteralPath 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2\System.dll'
$workspaceFramework = Test-Path -LiteralPath (Join-Path $repo '.tools\reference-assemblies\build\.NETFramework\v4.7.2\System.dll')
Write-Output "Framework 4.7.2 system targeting pack: $systemFramework"
Write-Output "Framework 4.7.2 workspace references: $workspaceFramework"
