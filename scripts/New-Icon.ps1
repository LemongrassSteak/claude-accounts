$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$assets = Join-Path (Split-Path $PSScriptRoot -Parent) 'src\ClaudeAccounts.App\Assets'
New-Item -ItemType Directory -Path $assets -Force | Out-Null
$bitmap = New-Object Drawing.Bitmap 256,256
$graphics = [Drawing.Graphics]::FromImage($bitmap)
$graphics.SmoothingMode = [Drawing.Drawing2D.SmoothingMode]::AntiAlias
$graphics.Clear([Drawing.Color]::FromArgb(246,244,239))
$rear = New-Object Drawing.SolidBrush ([Drawing.Color]::FromArgb(87,113,106))
$front = New-Object Drawing.SolidBrush ([Drawing.Color]::FromArgb(166,78,50))
$white = New-Object Drawing.SolidBrush ([Drawing.Color]::White)
$graphics.FillRectangle($rear, 40,45,132,145)
$graphics.FillRectangle($white, 76,76,145,145)
$graphics.FillRectangle($front, 84,84,132,132)
$graphics.FillEllipse($white, 122,112,56,56)
$graphics.FillRectangle($white, 112,176,76,10)
$bitmap.Save((Join-Path $assets 'app.png'), [Drawing.Imaging.ImageFormat]::Png)
$stream = New-Object IO.MemoryStream
$bitmap.Save($stream, [Drawing.Imaging.ImageFormat]::Png)
$png = $stream.ToArray()
$writer = New-Object IO.BinaryWriter ([IO.File]::Create((Join-Path $assets 'app.ico')))
try {
    $writer.Write([uint16]0); $writer.Write([uint16]1); $writer.Write([uint16]1)
    $writer.Write([byte]0); $writer.Write([byte]0); $writer.Write([byte]0); $writer.Write([byte]0)
    $writer.Write([uint16]1); $writer.Write([uint16]32)
    $writer.Write([uint32]$png.Length); $writer.Write([uint32]22); $writer.Write($png)
} finally {
    $writer.Dispose(); $stream.Dispose(); $graphics.Dispose(); $bitmap.Dispose()
    $rear.Dispose(); $front.Dispose(); $white.Dispose()
}
