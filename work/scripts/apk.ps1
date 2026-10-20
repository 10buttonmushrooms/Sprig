#requires -Version 5.1
# Dot-source to resolve an APK build. No name is the default Sprig APK. A name selects
# Bloom\<Name>, whose bloom.ini [Apk] section gives the APK its Label, Package and Icon.

function Read-BloomIni {
    param([string]$Path)
    # Matches Bloom.cmake: case-sensitive names, and ; or # starts a comment anywhere.
    $sections = [System.Collections.Hashtable]::new([System.StringComparer]::Ordinal)
    $section = ''
    foreach ($raw in [System.IO.File]::ReadAllLines($Path)) {
        $line = ($raw -replace '[;#].*$', '').Trim()
        if ($line -match '^\[([^\]]+)\]$') {
            $section = $Matches[1].Trim()
            if (-not $sections.ContainsKey($section)) {
                $sections[$section] = [System.Collections.Hashtable]::new([System.StringComparer]::Ordinal)
            }
        } elseif ($section -and $line -match '^([^=]+?)\s*=\s*(.*)$') {
            $sections[$section][$Matches[1]] = $Matches[2]
        }
    }
    return $sections
}

function Get-SprigApk {
    param([string]$Name)
    $root = (Resolve-Path "$PSScriptRoot\..\..").Path
    $apk = [ordered]@{
        Name     = 'Sprig'
        Module   = $null
        Label    = $null
        Package  = 'com.ea.gp.pvzheroes'
        Icon     = $null
        BuildOut = Join-Path $root 'work\build-out'
        Unpacked = Join-Path $root 'work\apk-unpacked'
        OutApk   = Join-Path $root 'out\Sprig.apk'
    }
    if ($Name) {
        if ($Name -cnotmatch '^[A-Za-z][A-Za-z0-9_-]*$' -or $Name -eq 'Sprig') {
            throw "Invalid APK module name: $Name"
        }
        $module = Join-Path $root "Bloom\$Name"
        $manifest = Join-Path $module 'bloom.ini'
        if (-not (Test-Path -LiteralPath $manifest -PathType Leaf)) { throw "Bloom\$Name\bloom.ini not found." }
        $ini = Read-BloomIni $manifest
        if (-not $ini.ContainsKey('Apk')) { throw "Bloom\$Name\bloom.ini has no [Apk] section." }
        $settings = $ini['Apk']

        $apk.Name = $Name
        $apk.Module = $module
        $apk.Label = if ($settings['Label']) { $settings['Label'] } else { $Name }
        $apk.Package = if ($settings['Package']) { $settings['Package'] } else {
            'com.ea.gp.pvzheroes.' + ($Name.ToLowerInvariant() -replace '-', '_')
        }
        if ($apk.Package -cnotmatch '^[A-Za-z][A-Za-z0-9_]*(\.[A-Za-z][A-Za-z0-9_]*)+$') {
            throw "Invalid [Apk] Package in Bloom\$Name\bloom.ini: $($apk.Package)"
        }
        if ($settings['Icon']) {
            $apk.Icon = Join-Path $module $settings['Icon']
            if (-not (Test-Path -LiteralPath $apk.Icon -PathType Leaf)) { throw "[Apk] Icon not found: $($apk.Icon)" }
        }
        $apk.BuildOut = Join-Path $root "work\build-out\apk\$Name"
        $apk.Unpacked = Join-Path $root "work\apk-unpacked-$Name"
        $apk.OutApk = Join-Path $root "out\$Name.apk"
    }
    $apk.CMakeDir = Join-Path $apk.BuildOut 'cmake'
    return [pscustomobject]$apk
}

function Write-IconBitmap {
    param($Source, [string]$Path, [int]$Width, [int]$Height, [string]$Kind, $Background)
    $bitmap = New-Object System.Drawing.Bitmap($Width, $Height, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $square = $null
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    try {
        $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
        $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
        $graphics.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
        $graphics.Clear([System.Drawing.Color]::Transparent)
        # TileFlipXY stops bicubic sampling from fading the outermost pixels.
        $attributes = New-Object System.Drawing.Imaging.ImageAttributes
        $attributes.SetWrapMode([System.Drawing.Drawing2D.WrapMode]::TileFlipXY)
        $draw = {
            param($g, [single]$x, [single]$y, [single]$w, [single]$h)
            $rect = New-Object System.Drawing.Rectangle([int]$x, [int]$y, [int][Math]::Round($w), [int][Math]::Round($h))
            $g.DrawImage($Source, $rect, 0, 0, $Source.Width, $Source.Height, [System.Drawing.GraphicsUnit]::Pixel, $attributes)
        }
        switch ($Kind) {
            'background' { $graphics.Clear($Background) }
            # Adaptive icon layers are 108dp; launchers show only the middle 72dp.
            'foreground' { $inset = $Width * 18 / 108; & $draw $graphics $inset $inset ($Width - 2 * $inset) ($Height - 2 * $inset) }
            'round' {
                $square = New-Object System.Drawing.Bitmap($Width, $Height, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
                $squareGraphics = [System.Drawing.Graphics]::FromImage($square)
                try {
                    $squareGraphics.InterpolationMode = $graphics.InterpolationMode
                    $squareGraphics.PixelOffsetMode = $graphics.PixelOffsetMode
                    & $draw $squareGraphics 0 0 $Width $Height
                } finally { $squareGraphics.Dispose() }
                $brush = New-Object System.Drawing.TextureBrush($square)
                try { $graphics.FillEllipse($brush, 0, 0, $Width, $Height) } finally { $brush.Dispose() }
            }
            default { & $draw $graphics 0 0 $Width $Height }
        }
        $bitmap.Save($Path, [System.Drawing.Imaging.ImageFormat]::Png)
    } finally {
        $graphics.Dispose()
        $bitmap.Dispose()
        if ($square) { $square.Dispose() }
    }
}

function Set-ApkIcon {
    param([string]$ResDir, [string]$Icon)
    Add-Type -AssemblyName System.Drawing
    $source = [System.Drawing.Image]::FromFile($Icon)
    try {
        # The average border color fills the adaptive background layer around the art.
        $bitmap = New-Object System.Drawing.Bitmap($source)
        try {
            $r = 0; $g = 0; $b = 0; $n = 0
            for ($i = 0; $i -lt $bitmap.Width; $i++) {
                foreach ($p in @($bitmap.GetPixel($i, 0), $bitmap.GetPixel($i, $bitmap.Height - 1))) { $r += $p.R; $g += $p.G; $b += $p.B; $n++ }
            }
            for ($i = 0; $i -lt $bitmap.Height; $i++) {
                foreach ($p in @($bitmap.GetPixel(0, $i), $bitmap.GetPixel($bitmap.Width - 1, $i))) { $r += $p.R; $g += $p.G; $b += $p.B; $n++ }
            }
            $background = [System.Drawing.Color]::FromArgb([int]($r / $n), [int]($g / $n), [int]($b / $n))
        } finally { $bitmap.Dispose() }

        $targets = @(Get-ChildItem -LiteralPath $ResDir -Directory | Where-Object { $_.Name -like 'mipmap*' -or $_.Name -like 'drawable*' } |
            ForEach-Object { Get-ChildItem -LiteralPath $_.FullName -File } |
            Where-Object { $_.Name -match '^(ic_launcher|app_icon)(_round|_foreground|_background)?\.png$' })
        if (-not $targets) { throw "No launcher icon PNGs found under $ResDir" }
        foreach ($file in $targets) {
            $existing = [System.Drawing.Image]::FromFile($file.FullName)
            $width = $existing.Width; $height = $existing.Height
            $existing.Dispose()
            $kind = switch -Regex ($file.BaseName) {
                '_foreground$' { 'foreground' }
                '_background$' { 'background' }
                '_round$'      { 'round' }
                default        { 'square' }
            }
            Write-IconBitmap -Source $source -Path $file.FullName -Width $width -Height $height -Kind $kind -Background $background
        }
        Write-Host "Replaced $($targets.Count) launcher icon images."
    } finally {
        $source.Dispose()
    }
}

function Set-ApkIdentity {
    param($Target, [string]$PristineManifest)
    $utf8 = [System.Text.UTF8Encoding]::new($false)
    $unpacked = $Target.Unpacked

    # Every manifest reference to the package is the package itself, a permission, a
    # provider authority or a URL scheme, so renaming the prefix keeps them all unique.
    $manifest = [System.IO.File]::ReadAllText($PristineManifest)
    # Package names are validated to [A-Za-z0-9_.], so the replacement has no $ escapes.
    $manifest = [regex]::Replace($manifest, '"com\.ea\.gp\.pvzheroes(?=[".])', '"' + $Target.Package)
    # EA's Nimble SDK keeps identifying as the original game, as Rogue-Heroes does.
    $nimble = '<meta-data android:name="com.ea.nimble.configuration"'
    if (-not $manifest.Contains($nimble)) { throw 'Nimble configuration meta-data not found in AndroidManifest.xml.' }
    $manifest = [regex]::Replace($manifest, '\s*<meta-data android:name="NimbleBundleIdOverride" android:value="[^"]*"/>', '')
    $manifest = $manifest.Replace($nimble, '<meta-data android:name="NimbleBundleIdOverride" android:value="com.ea.gp.pvzheroes"/>' + "`n        " + $nimble)
    [System.IO.File]::WriteAllText((Join-Path $unpacked 'AndroidManifest.xml'), $manifest, $utf8)

    $strings = Join-Path $unpacked 'res\values\strings.xml'
    $text = [System.IO.File]::ReadAllText($strings)
    $match = [regex]::Match($text, '(<string\s+name="app_name">)[^<]*(</string>)')
    if (-not $match.Success) { throw 'app_name string not found.' }
    $label = [System.Security.SecurityElement]::Escape($Target.Label)
    $text = $text.Substring(0, $match.Index) + $match.Groups[1].Value + $label + $match.Groups[2].Value + $text.Substring($match.Index + $match.Length)
    [System.IO.File]::WriteAllText($strings, $text, $utf8)

    if ($Target.Icon) { Set-ApkIcon -ResDir (Join-Path $unpacked 'res') -Icon $Target.Icon }
    Write-Host "APK identity: $($Target.Label) ($($Target.Package))"
}
