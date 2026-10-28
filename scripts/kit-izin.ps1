# kit-izin.ps1 - agent-kit v2
# Bilincli gecis izni: bir hook engeli kullaniciya SORULDUKTAN ve onay alindiktan sonra supervizor
# zaman damgali bir isaret birakir; ilgili hook IzinSuresiDakika (agent-kit.config.psd1) boyunca gecer.
# Isaretler ANA worktree'nin Temp\ klasorunde durur (Temp\agent-kit-izin-<ad>), oturum hangi worktree'den
# calisirsa calissin ayni yere bakilir.
#
# Bilinen izinler:
#   unity-test     hook-guard-mcp: commit'siz prefab/sahne/asset varken run_tests
#   unity-refresh  hook-guard-mcp: commit'siz prefab/sahne/asset varken refresh_unity
#
# Kullanim (proje kokunden):
#   powershell -NoProfile -File scripts/kit-izin.ps1 unity-test      # izin ver
#   powershell -NoProfile -File scripts/kit-izin.ps1 -Liste          # izinleri ve kalan sureyi goster
#   powershell -NoProfile -File scripts/kit-izin.ps1 -Sil unity-test # izni geri al

[CmdletBinding()]
param(
    [Parameter(Position = 0)][string]$Ad,
    [switch]$Liste,
    [string]$Sil,
    [string]$Root
)

$ErrorActionPreference = 'Continue'
. (Join-Path $PSScriptRoot 'kit-lib.ps1')

$AdDeseni = '^[A-Za-z0-9][A-Za-z0-9_-]*$'
$Onek = 'agent-kit-izin-'

if (-not $Root) { $Root = Get-KitRoot }
$cfg = Get-KitConfig -Root $Root
$sure = [int]$cfg.IzinSuresiDakika

if ($Liste) {
    $temp = Join-Path (Get-AnaWorktree -Root $Root) 'Temp'
    $dosyalar = @(Get-ChildItem -LiteralPath $temp -Filter "$Onek*" -File -ErrorAction SilentlyContinue | Sort-Object Name)
    if ($dosyalar.Count -eq 0) { Write-Host "Izin yok ($temp)."; exit 0 }
    foreach ($f in $dosyalar) {
        $yas = ((Get-Date) - $f.LastWriteTime).TotalMinutes
        $kalan = [Math]::Round($sure - $yas, 1)
        $durum = 'SURESI DOLDU'
        if ($kalan -ge 0) { $durum = "gecerli, $kalan dk kaldi" }
        Write-Host ("{0,-16} {1:yyyy-MM-dd HH:mm:ss}  {2}" -f $f.Name.Substring($Onek.Length), $f.LastWriteTime, $durum)
    }
    exit 0
}

if ($Sil) {
    if ($Sil -cnotmatch $AdDeseni) { Write-Host "Gecersiz izin adi: '$Sil' (harf, rakam, - ve _ olabilir)."; exit 2 }
    $yol = Get-KitIzinYolu -Ad $Sil -Root $Root
    if (Test-Path -LiteralPath $yol) {
        Remove-Item -LiteralPath $yol -Force
        Write-Host "Izin geri alindi: $Sil"
    } else {
        Write-Host "Izin zaten yok: $Sil"
    }
    exit 0
}

if (-not $Ad) {
    Write-Host 'Kullanim: kit-izin.ps1 <ad> | -Liste | -Sil <ad>    (ad: unity-test, unity-refresh)'
    Write-Host 'Izni yalniz kullanici onay verdikten sonra ver.'
    exit 2
}
if ($Ad -cnotmatch $AdDeseni) { Write-Host "Gecersiz izin adi: '$Ad' (harf, rakam, - ve _ olabilir)."; exit 2 }

$isaret = New-KitIzin -Ad $Ad -Root $Root
Write-Host "Izin verildi: $Ad ($sure dk gecerli) -> $isaret"
exit 0
