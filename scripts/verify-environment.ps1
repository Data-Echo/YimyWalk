$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
$sdk = Join-Path $repo '.tools\dotnet\dotnet.exe'
$solution = Join-Path $repo 'YimyWalk.sln'

Write-Output "Repo: $repo"
Write-Output "OS: $((Get-CimInstance Win32_OperatingSystem).Caption) $((Get-CimInstance Win32_OperatingSystem).OSArchitecture)"
Write-Output "Git: $((git --version).Trim())"

if (Test-Path -LiteralPath $sdk -PathType Leaf) {
    Write-Output "Workspace .NET: $((& $sdk --version).Trim())"
} else {
    $command = Get-Command dotnet.exe -ErrorAction SilentlyContinue
    if ($null -eq $command) {
        Write-Warning 'Workspace .NET SDK missing and dotnet.exe is not on PATH; run scripts/bootstrap-dotnet.ps1.'
    } else {
        Write-Output "System .NET: $((& $command.Source --version).Trim())"
    }
}

if (-not (Test-Path -LiteralPath $solution -PathType Leaf)) {
    throw "Solution file not found: $solution"
}

if (Test-Path -LiteralPath (Join-Path $repo 'vendor\esheep')) {
    throw 'vendor/esheep must not be present in the repository. Use an external temporary checkout for upstream audits.'
}

Write-Output 'YimyWalk source tree: present'
Write-Output 'Upstream snapshot: intentionally external'
