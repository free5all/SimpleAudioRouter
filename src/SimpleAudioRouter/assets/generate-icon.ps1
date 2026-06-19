# Regenerates assets/app.ico (run from repo root or this folder).
$ErrorActionPreference = "Stop"
$assetsDir = Split-Path -Parent $MyInvocation.MyCommand.Path
Add-Type -AssemblyName System.Drawing

function New-AppBitmap([int]$size) {
    $bmp = New-Object System.Drawing.Bitmap $size, $size
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.Clear([System.Drawing.Color]::FromArgb(255, 17, 17, 27))
    $margin = [math]::Max(1, [int]($size * 0.12))
    $inner = $size - (2 * $margin)
    $mid = $margin + [int]($inner / 2)
    $top = $margin + [int]($inner * 0.18)
    $bottom = $margin + [int]($inner * 0.82)
    $leftBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 148, 226, 213))
    $rightBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 137, 180, 250))
    $accentPen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255, 137, 180, 250)), ([math]::Max(1, $size / 16.0))
    $pointsL = @(
        [System.Drawing.Point]::new($mid, $top),
        [System.Drawing.Point]::new($margin, $bottom),
        [System.Drawing.Point]::new($mid, $bottom)
    )
    $pointsR = @(
        [System.Drawing.Point]::new($mid, $top),
        [System.Drawing.Point]::new($margin + $inner, $bottom),
        [System.Drawing.Point]::new($mid, $bottom)
    )
    $g.FillPolygon($leftBrush, $pointsL)
    $g.FillPolygon($rightBrush, $pointsR)
    $g.DrawLine($accentPen, $mid, $top, $mid, $bottom)
    $g.Dispose(); $leftBrush.Dispose(); $rightBrush.Dispose(); $accentPen.Dispose()
    return $bmp
}

$bmp = New-AppBitmap 256
$icon = [System.Drawing.Icon]::FromHandle($bmp.GetHicon())
$outPath = Join-Path $assetsDir "app.ico"
$stream = [System.IO.File]::Open($outPath, [System.IO.FileMode]::Create)
$icon.Save($stream)
$stream.Close()
$icon.Dispose(); $bmp.Dispose()
Write-Host "Wrote $outPath"
