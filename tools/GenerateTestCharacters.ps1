[CmdletBinding()]
param(
    [string]$Root
)

$ErrorActionPreference = 'Stop'
$scriptRoot = if ($PSScriptRoot) { $PSScriptRoot } else { (Get-Location).Path }
if ([string]::IsNullOrWhiteSpace($Root)) { $Root = Join-Path (Join-Path $scriptRoot '..') 'Characters' }
Add-Type -AssemblyName System.Drawing

function New-Sprite([string]$kind, [string]$directory) {
    New-Item -ItemType Directory -Force -Path $directory | Out-Null
    $bitmap = [System.Drawing.Bitmap]::new(96, 96, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $graphics.Clear([System.Drawing.Color]::Transparent)

    $body = if ($kind -eq 'cat') { [System.Drawing.Color]::FromArgb(255, 232, 155, 75) } else { [System.Drawing.Color]::FromArgb(255, 86, 205, 126) }
    $shadow = if ($kind -eq 'cat') { [System.Drawing.Color]::FromArgb(255, 155, 91, 45) } else { [System.Drawing.Color]::FromArgb(255, 35, 133, 77) }
    $face = [System.Drawing.Color]::FromArgb(255, 35, 35, 45)
    $bodyBrush = [System.Drawing.SolidBrush]::new($body)
    $shadowBrush = [System.Drawing.SolidBrush]::new($shadow)
    $faceBrush = [System.Drawing.SolidBrush]::new($face)

    try {
        for ($index = 0; $index -lt 9; $index++) {
            $ox = ($index % 3) * 32
            $oy = [math]::Floor($index / 3) * 32
            $bounce = if ($index -eq 6) { -4 } elseif ($index -eq 7) { -6 } elseif ($index -eq 8) { 2 } elseif ($index % 2 -eq 1) { -1 } else { 0 }
            $x = $ox + 4
            $y = $oy + 5 + $bounce

            if ($kind -eq 'cat') {
                $points = [System.Drawing.Point[]]@(
                    [System.Drawing.Point]::new($x + 3, $y + 8),
                    [System.Drawing.Point]::new($x + 7, $y),
                    [System.Drawing.Point]::new($x + 13, $y + 5),
                    [System.Drawing.Point]::new($x + 20, $y + 5),
                    [System.Drawing.Point]::new($x + 26, $y),
                    [System.Drawing.Point]::new($x + 29, $y + 10),
                    [System.Drawing.Point]::new($x + 27, $y + 23),
                    [System.Drawing.Point]::new($x + 7, $y + 23)
                )
                $graphics.FillPolygon($bodyBrush, $points)
                $graphics.FillEllipse($shadowBrush, $x + 2, $y + 20, 26, 7)
                $graphics.FillEllipse($faceBrush, $x + 10, $y + 11, 3, 4)
                $graphics.FillEllipse($faceBrush, $x + 20, $y + 11, 3, 4)
            } else {
                $graphics.FillEllipse($bodyBrush, $x, $y + 5, 25, 22)
                $graphics.FillRectangle($bodyBrush, $x, $y + 15, 25, 9)
                $graphics.FillEllipse($shadowBrush, $x + 2, $y + 22, 21, 5)
                $graphics.FillEllipse($faceBrush, $x + 8, $y + 13, 3, 4)
                $graphics.FillEllipse($faceBrush, $x + 16, $y + 13, 3, 4)
            }
        }

        $path = Join-Path $directory 'sprite.png'
        $bitmap.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
        Write-Host "Generated $path"
    }
    finally {
        $faceBrush.Dispose(); $shadowBrush.Dispose(); $bodyBrush.Dispose(); $graphics.Dispose(); $bitmap.Dispose()
    }
}

New-Sprite 'cat' (Join-Path $Root 'cat')
New-Sprite 'slime' (Join-Path $Root 'slime')
