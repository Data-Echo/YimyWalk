[CmdletBinding()]
param(
    [string]$SourcePath = 'icon_yimy.png',
    [string]$OutputDirectory = 'src\YimyWalk.App\Resources'
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$source = if ([IO.Path]::IsPathRooted($SourcePath)) { $SourcePath } else { Join-Path $root $SourcePath }
$output = if ([IO.Path]::IsPathRooted($OutputDirectory)) { $OutputDirectory } else { Join-Path $root $OutputDirectory }
if (-not (Test-Path -LiteralPath $source -PathType Leaf)) { throw "Icon source not found: $source" }
New-Item -ItemType Directory -Path $output -Force | Out-Null

Add-Type -AssemblyName System.Drawing
$image = [Drawing.Image]::FromFile($source)
try {
    if ($image.Width -ne $image.Height) { throw "Icon source must be square: $($image.Width)x$($image.Height)" }
    $sizes = @(16, 24, 32, 48, 64, 128, 256)
    $pngFrames = @()
    foreach ($size in $sizes) {
        $bitmap = New-Object Drawing.Bitmap($size, $size, [Drawing.Imaging.PixelFormat]::Format32bppArgb)
        try {
            $graphics = [Drawing.Graphics]::FromImage($bitmap)
            try {
                $graphics.Clear([Drawing.Color]::Transparent)
                $graphics.CompositingMode = [Drawing.Drawing2D.CompositingMode]::SourceOver
                $graphics.CompositingQuality = [Drawing.Drawing2D.CompositingQuality]::HighQuality
                $graphics.InterpolationMode = [Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
                $graphics.PixelOffsetMode = [Drawing.Drawing2D.PixelOffsetMode]::HighQuality
                $graphics.SmoothingMode = [Drawing.Drawing2D.SmoothingMode]::HighQuality
                $graphics.DrawImage($image, 0, 0, $size, $size)
            }
            finally { $graphics.Dispose() }

            $png = New-Object IO.MemoryStream
            $bitmap.Save($png, [Drawing.Imaging.ImageFormat]::Png)
            $pngFrames += [pscustomobject]@{ Size = $size; Bytes = $png.ToArray() }
            if ($size -eq 512) { continue }
        }
        finally { $bitmap.Dispose() }
    }

    $preview = New-Object Drawing.Bitmap(512, 512, [Drawing.Imaging.PixelFormat]::Format32bppArgb)
    try {
        $graphics = [Drawing.Graphics]::FromImage($preview)
        try {
            $graphics.Clear([Drawing.Color]::Transparent)
            $graphics.CompositingMode = [Drawing.Drawing2D.CompositingMode]::SourceOver
            $graphics.CompositingQuality = [Drawing.Drawing2D.CompositingQuality]::HighQuality
            $graphics.InterpolationMode = [Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
            $graphics.PixelOffsetMode = [Drawing.Drawing2D.PixelOffsetMode]::HighQuality
            $graphics.SmoothingMode = [Drawing.Drawing2D.SmoothingMode]::HighQuality
            $graphics.DrawImage($image, 0, 0, 512, 512)
        }
        finally { $graphics.Dispose() }
        $preview.Save((Join-Path $output 'icon_yimy.png'), [Drawing.Imaging.ImageFormat]::Png)
    }
    finally { $preview.Dispose() }

    $iconPath = Join-Path $output 'yimywalk.ico'
    $stream = New-Object IO.FileStream($iconPath, [IO.FileMode]::Create, [IO.FileAccess]::Write)
    try {
        $writer = New-Object IO.BinaryWriter($stream)
        $writer.Write([UInt16]0)
        $writer.Write([UInt16]1)
        $writer.Write([UInt16]$pngFrames.Count)
        $offset = 6 + (16 * $pngFrames.Count)
        foreach ($frame in $pngFrames) {
            $dimension = if ($frame.Size -eq 256) { 0 } else { $frame.Size }
            $writer.Write([Byte]$dimension)
            $writer.Write([Byte]$dimension)
            $writer.Write([Byte]0)
            $writer.Write([Byte]0)
            $writer.Write([UInt16]1)
            $writer.Write([UInt16]32)
            $writer.Write([UInt32]$frame.Bytes.Length)
            $writer.Write([UInt32]$offset)
            $offset += $frame.Bytes.Length
        }
        foreach ($frame in $pngFrames) { $writer.Write($frame.Bytes) }
        $writer.Flush()
    }
    finally { $stream.Dispose() }
}
finally { $image.Dispose() }

Write-Output "Generated $output\icon_yimy.png (512x512)"
Write-Output "Generated $output\yimywalk.ico ($($sizes -join ',') px)"
