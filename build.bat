@echo off
setlocal

set ROOT=%~dp0
set CSC=C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe
set OUTDIR=%ROOT%dist
set ICON=%ROOT%HwidViewer\app.ico

if not exist "%CSC%" (
    echo C# compiler not found at %CSC%
    exit /b 1
)

if not exist "%OUTDIR%" (
    mkdir "%OUTDIR%"
)

if not exist "%ICON%" (
    powershell -NoProfile -ExecutionPolicy Bypass -Command "$ErrorActionPreference='Stop'; Add-Type -AssemblyName System.Drawing; $iconPath='%ICON%'; $size=256; $bmp=New-Object System.Drawing.Bitmap($size,$size); $g=[System.Drawing.Graphics]::FromImage($bmp); $g.SmoothingMode=[System.Drawing.Drawing2D.SmoothingMode]::AntiAlias; $g.Clear([System.Drawing.Color]::FromArgb(24,82,163)); $brush=New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255,255,255)); $font=New-Object System.Drawing.Font('Segoe UI',120,[System.Drawing.FontStyle]::Bold,[System.Drawing.GraphicsUnit]::Pixel); $sf=New-Object System.Drawing.StringFormat; $sf.Alignment=[System.Drawing.StringAlignment]::Center; $sf.LineAlignment=[System.Drawing.StringAlignment]::Center; $g.DrawString('H',$font,$brush,[System.Drawing.RectangleF]::new(0,0,$size,$size),$sf); $ms=New-Object System.IO.MemoryStream; $bmp.Save($ms,[System.Drawing.Imaging.ImageFormat]::Png); $png=$ms.ToArray(); $fs=[System.IO.File]::Open($iconPath,[System.IO.FileMode]::Create,[System.IO.FileAccess]::Write); $bw=New-Object System.IO.BinaryWriter($fs); $bw.Write([UInt16]0); $bw.Write([UInt16]1); $bw.Write([UInt16]1); $bw.Write([byte]0); $bw.Write([byte]0); $bw.Write([byte]0); $bw.Write([byte]0); $bw.Write([UInt16]1); $bw.Write([UInt16]32); $bw.Write([UInt32]$png.Length); $bw.Write([UInt32]22); $bw.Write($png); $bw.Close(); $fs.Close(); $ms.Close(); $g.Dispose(); $brush.Dispose(); $font.Dispose(); $bmp.Dispose();"
    if errorlevel 1 (
        echo Failed to create icon file.
        exit /b 1
    )
)

"%CSC%" /nologo /target:winexe /out:"%OUTDIR%\HwidViewer.exe" ^
 /win32icon:"%ICON%" ^
 /reference:System.dll ^
 /reference:System.Core.dll ^
 /reference:System.Drawing.dll ^
 /reference:System.Windows.Forms.dll ^
 /reference:System.Management.dll ^
 "%ROOT%HwidViewer\Program.cs"

if errorlevel 1 (
    echo Build failed.
    exit /b 1
)

echo Build success: %OUTDIR%\HwidViewer.exe
