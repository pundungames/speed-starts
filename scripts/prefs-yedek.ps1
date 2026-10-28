# prefs-yedek.ps1 - agent-kit v2
# Unity Editor'un PlayerPrefs kaydini (HKCU\Software\Unity\UnityEditor\<company>\<product>) ve varsa oyunun
# PlayerPrefs kaydini (HKCU\Software\<company>\<product>) yedekler, karsilastirir, geri yukler.
#
# Neden: ana Editor'de MCP run_tests kullanicinin dev save'ini iki kez sildi (21.10, 27.10).
# hook-guard-mcp.ps1 her run_tests'ten once '-Al -Sessiz' cagirir. Kosudan sonra '-Karsilastir' ile bak;
# fark varsa '-GeriYukle'.
#
# Kullanim (proje kokunden):
#   powershell -NoProfile -File scripts/prefs-yedek.ps1 -Al [-Sessiz]
#   powershell -NoProfile -File scripts/prefs-yedek.ps1 -Karsilastir [-Yedek <dosya>]   # 0 ayni / 1 fark / 2 yapilamadi
#   powershell -NoProfile -File scripts/prefs-yedek.ps1 -GeriYukle [-Yedek <dosya>]
# Yedekler: <ana worktree>\<LogKlasoru>\prefs-yedek\<yyyyMMdd-HHmmss>-editor.reg (+ ayni damgali -player.reg).
# Her turden en yeni -Tut (vars. 20) yedek tutulur. -Yedek verilmezse en yeni -editor.reg ve ayni damgali
# -player.reg kullanilir. -Root verilmezse ana worktree ('git worktree list' ilk satiri) kullanilir.
#
# Karsilastirma SATIR bazindadir (Compare-Object), hash degil: reg export sirasi degisebilir, hash bunu fark sanar.
# .reg'deki hex devam satirlari (\ ile biten) once tek mantiksal satira birlestirilir.

[CmdletBinding()]
param(
    [switch]$Al,
    [switch]$Karsilastir,
    [switch]$GeriYukle,
    [string]$Yedek,
    [switch]$Sessiz,
    [string]$Root,
    [int]$Tut = 20
)

$ErrorActionPreference = 'Continue'
. (Join-Path $PSScriptRoot 'kit-lib.ps1')

function Write-Bilgi([string]$Mesaj) { if (-not $Sessiz) { Write-Host $Mesaj } }

$kipler = @()
if ($Al) { $kipler += 'Al' }
if ($Karsilastir) { $kipler += 'Karsilastir' }
if ($GeriYukle) { $kipler += 'GeriYukle' }
if ($kipler.Count -ne 1) {
    Write-Host 'Kullanim: prefs-yedek.ps1 -Al [-Sessiz] | -Karsilastir [-Yedek <dosya>] | -GeriYukle [-Yedek <dosya>]  [-Root <proje>]'
    exit 2
}

if (-not $Root) { $Root = Get-AnaWorktree -Root (Get-Location).Path }
$cfg = Get-KitConfig -Root $Root
$kimlik = Get-UnityKimlik -ProjeYolu $Root -Config $cfg
$klasor = Join-Path (Join-Path $Root $cfg.LogKlasoru) 'prefs-yedek'

function ConvertTo-RegSaglayici([string]$Anahtar) {
    $a = $Anahtar -replace '^HKCU\\', 'HKEY_CURRENT_USER\'
    return ('Registry::' + $a)
}

function Test-RegAnahtar([string]$Anahtar) {
    return (Test-Path -LiteralPath (ConvertTo-RegSaglayici $Anahtar))
}

function Export-RegAnahtar([string]$Anahtar, [string]$Dosya) {
    $null = & reg.exe export $Anahtar $Dosya /y 2>$null
    return (($LASTEXITCODE -eq 0) -and (Test-Path -LiteralPath $Dosya))
}

function Test-UnityAcik([string]$Proje) {
    $p = $Proje.ToLowerInvariant().TrimEnd('\')
    $desen = [regex]::Escape($p) + '(?=["''\s\\]|$)'
    foreach ($s in @(Get-CimInstance Win32_Process -Filter "Name='Unity.exe'" -ErrorAction SilentlyContinue)) {
        $cl = [string]$s.CommandLine
        if (-not $cl) { return $true }
        if ([regex]::IsMatch($cl.Replace('/', '\').ToLowerInvariant(), $desen)) { return $true }
    }
    return $false
}

function Read-RegKayitlari([string]$Dosya) {
    # Sonuc: @{ Kok = '<ilk [anahtar]>'; Satirlar = List[string] }; her satir "<anahtar>`t<deger satiri>".
    $satirlar = New-Object 'System.Collections.Generic.List[string]'
    $kok = $null
    $bolum = ''
    $birikim = $null
    if ($Dosya -and (Test-Path -LiteralPath $Dosya)) {
        foreach ($l in @(Get-Content -LiteralPath $Dosya)) {
            $t = ([string]$l).TrimEnd()
            if ($null -ne $birikim) { $t = $birikim + $t.TrimStart(); $birikim = $null }
            if ($t.EndsWith('\')) { $birikim = $t.Substring(0, $t.Length - 1); continue }
            if ($t.Length -eq 0 -or $t.StartsWith('Windows Registry Editor') -or $t.StartsWith('REGEDIT4')) { continue }
            if ($t.StartsWith('[') -and $t.EndsWith(']')) {
                $bolum = $t.Substring(1, $t.Length - 2)
                if (-not $kok) { $kok = $bolum }
                $satirlar.Add("$bolum`t")
                continue
            }
            $satirlar.Add("$bolum`t$t")
        }
        if ($null -ne $birikim) { $satirlar.Add("$bolum`t$birikim") }
    }
    return @{ Kok = $kok; Satirlar = $satirlar }
}

function Get-RegDegerAdi([string]$Kayit, [string]$Kok) {
    $parca = $Kayit.Split([char]9, 2)
    $bolum = $parca[0]
    $satir = ''
    if ($parca.Count -gt 1) { $satir = $parca[1] }
    if ($satir -match '^"((?:[^"\\]|\\.)*)"=') { $ad = $Matches[1] -replace '\\(.)', '$1' }
    elseif ($satir.StartsWith('@=')) { $ad = '(varsayilan deger)' }
    elseif (-not $satir) { $ad = '(anahtarin kendisi)' }
    else { $ad = $satir }
    $alt = ''
    if ($Kok -and -not $bolum.Equals($Kok, [StringComparison]::OrdinalIgnoreCase)) {
        if ($bolum.StartsWith($Kok + '\', [StringComparison]::OrdinalIgnoreCase)) { $alt = $bolum.Substring($Kok.Length + 1) + '\' }
        else { $alt = $bolum + '\' }
    }
    return "$alt$ad"
}

function Compare-RegYedek([string]$YedekDosya) {
    $eski = Read-RegKayitlari $YedekDosya
    if (-not $eski.Kok) { Write-Host "Yedek okunamadi ya da bos: $YedekDosya"; return 2 }
    $simdi = @{ Kok = $eski.Kok; Satirlar = (New-Object 'System.Collections.Generic.List[string]') }
    if (Test-RegAnahtar $eski.Kok) {
        $gecici = Join-Path ([System.IO.Path]::GetTempPath()) ('prefs-simdi-' + [guid]::NewGuid().ToString('N') + '.reg')
        try {
            if (-not (Export-RegAnahtar $eski.Kok $gecici)) { Write-Host "Simdiki kayit export edilemedi: $($eski.Kok)"; return 2 }
            $simdi = Read-RegKayitlari $gecici
        } finally { Remove-Item -LiteralPath $gecici -Force -ErrorAction SilentlyContinue }
    }
    $farklar = @(Compare-Object -ReferenceObject @($eski.Satirlar) -DifferenceObject @($simdi.Satirlar) -CaseSensitive)
    $yedekte = New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::OrdinalIgnoreCase)
    $simdide = New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::OrdinalIgnoreCase)
    foreach ($f in $farklar) {
        $ad = Get-RegDegerAdi ([string]$f.InputObject) $eski.Kok
        if ($f.SideIndicator -eq '<=') { [void]$yedekte.Add($ad) } else { [void]$simdide.Add($ad) }
    }
    $silinen = @($yedekte | Where-Object { -not $simdide.Contains($_) } | Sort-Object)
    $degisen = @($yedekte | Where-Object { $simdide.Contains($_) } | Sort-Object)
    $eklenen = @($simdide | Where-Object { -not $yedekte.Contains($_) } | Sort-Object)
    Write-Host "Yedek  : $YedekDosya"
    Write-Host "Anahtar: $($eski.Kok)"
    if ($farklar.Count -eq 0) { Write-Host 'Fark yok.'; return 0 }
    if ($silinen.Count -gt 0) { Write-Host "Silinen ($($silinen.Count)): $($silinen -join ', ')" }
    if ($degisen.Count -gt 0) { Write-Host "Degisen ($($degisen.Count)): $($degisen -join ', ')" }
    if ($eklenen.Count -gt 0) { Write-Host "Eklenen ($($eklenen.Count)): $($eklenen -join ', ')" }
    if ($silinen.Count -gt 0 -or $degisen.Count -gt 0) { Write-Host 'Geri yuklemek icin: powershell -NoProfile -File scripts/prefs-yedek.ps1 -GeriYukle' }
    return 1
}

function Get-YedekDosyalari {
    if ($Yedek) {
        if (-not (Test-Path -LiteralPath $Yedek)) { return @() }
        return @((Resolve-Path -LiteralPath $Yedek).Path)
    }
    $son = Get-ChildItem -LiteralPath $klasor -Filter '*-editor.reg' -File -ErrorAction SilentlyContinue | Sort-Object Name -Descending | Select-Object -First 1
    if (-not $son) { return @() }
    $liste = @($son.FullName)
    $oyuncu = Join-Path $klasor ($son.Name -replace '-editor\.reg$', '-player.reg')
    if (Test-Path -LiteralPath $oyuncu) { $liste += $oyuncu }
    return $liste
}

# --- -Al ---------------------------------------------------------------------------
if ($Al) {
    if (-not $kimlik.Company -or -not $kimlik.Product) {
        try { Write-KitLog -Ad 'hook-sorunlari' -Mesaj "prefs-yedek: Unity kimligi (companyName/productName) bulunamadi ($Root), yedek alinmadi." -Root $Root } catch { }
        Write-Bilgi "Unity kimligi (companyName/productName) bulunamadi ($Root); yedek alinmadi."
        exit 0
    }
    $damga = Get-Date -Format 'yyyyMMdd-HHmmss'
    $turler = @(
        @{ Ek = 'editor'; Anahtar = $kimlik.EditorPrefs },
        @{ Ek = 'player'; Anahtar = $kimlik.PlayerPrefs }
    )
    foreach ($tur in $turler) {
        if (-not (Test-RegAnahtar $tur.Anahtar)) { Write-Bilgi "Anahtar yok, gecildi: $($tur.Anahtar)"; continue }
        New-Item -ItemType Directory -Force -Path $klasor | Out-Null
        $dosya = Join-Path $klasor "$damga-$($tur.Ek).reg"
        if (Export-RegAnahtar $tur.Anahtar $dosya) {
            Write-Bilgi "Yedek alindi: $dosya"
        } else {
            try { Write-KitLog -Ad 'hook-sorunlari' -Mesaj "prefs-yedek: reg export basarisiz: $($tur.Anahtar)" -Root $Root } catch { }
            Write-Bilgi "UYARI: reg export basarisiz: $($tur.Anahtar)"
        }
        Get-ChildItem -LiteralPath $klasor -Filter "*-$($tur.Ek).reg" -File -ErrorAction SilentlyContinue |
            Sort-Object Name -Descending | Select-Object -Skip $Tut | Remove-Item -Force -ErrorAction SilentlyContinue
    }
    exit 0
}

# --- -Karsilastir ------------------------------------------------------------------
if ($Karsilastir) {
    $dosyalar = @(Get-YedekDosyalari)
    if ($dosyalar.Count -eq 0) { Write-Host "Karsilastirilacak yedek yok ($klasor). Once: prefs-yedek.ps1 -Al"; exit 2 }
    $sonuc = 0
    foreach ($d in $dosyalar) {
        $r = Compare-RegYedek $d
        if ($r -gt $sonuc) { $sonuc = $r }
    }
    exit $sonuc
}

# --- -GeriYukle --------------------------------------------------------------------
if ($GeriYukle) {
    $dosyalar = @(Get-YedekDosyalari)
    if ($dosyalar.Count -eq 0) { Write-Host "Geri yuklenecek yedek yok ($klasor)."; exit 2 }
    if (Test-UnityAcik $Root) {
        Write-Warning ("Unity Editor bu projeyi acik tutuyor. Editor kapanirken (ya da PlayerPrefs.Save'de) bellekteki degerleri yeniden yazip " +
            "bu geri yuklemeyi ezebilir. Play modunu kapat; geri yukledikten sonra Unity'de PlayerPrefs.HasKey ile dogrula, ezilirse Editor'u kapatip tekrar calistir.")
    }
    $hata = 0
    foreach ($d in $dosyalar) {
        $null = & reg.exe import $d 2>$null
        if ($LASTEXITCODE -eq 0) { Write-Host "Geri yuklendi: $d" } else { Write-Host "HATA: reg import basarisiz (exit $LASTEXITCODE): $d"; $hata++ }
    }
    Write-Host 'Not: reg import silinen/degisen degerleri geri yazar; yedekten SONRA eklenen degerleri silmez.'
    if ($hata -gt 0) { exit 1 }
    exit 0
}

exit 0
