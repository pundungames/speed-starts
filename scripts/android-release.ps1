<#
.SYNOPSIS
    Magaza build'i: kopyada IL2CPP + ARMv7/ARM64 AAB alir (batchmode), ANDROID_* ve error CS satirlarini basar.

.DESCRIPTION
    Metot: SpeedStars.Tools.Build.ReleaseBuild.Run (Assets/Speed-Staes/Tools/Editor/Packaging). Ayarlar yalniz build
    suresince uygulanir, metot bitince geri yazilir. Imza parolalari ortam degiskenlerinden okunur
    (SPEEDSTARS_KEYSTORE_PASS, SPEEDSTARS_KEY_PASS) ve bu script'te yazilmaz/basilmaz; yoksa debug imza + ANDROID_RELEASE_UNSIGNED.
    Ana klasorde Unity aciksa calismaz: kopyada kos (scripts\kopya.ps1 -Al / -Senkron).
    Cikis kodlari: 0 build tamam; 1 build/derleme basarisiz; 10 Unity kurulumu yok; 11 proje yok; 12 Editor projeyi
    acik tutuyor; 13 zaman asimi / log yok; 19 script ic hatasi.
    Dosya YALNIZ ASCII (Windows PowerShell 5.1).

.PARAMETER ProjectPath
    Build alinacak Unity projesi (dogrulama kopyasi), ornek D:\Pundun\speed-starts-verify-1.
.PARAMETER OutFile
    Cikti yolu. Bos = <depo>\pipeline-logs\android\SpeedStars-release.aab (config apk derse .apk icin yol ver).
.PARAMETER ConfigAsset
    ReleaseBuildConfig asset yolu (proje icinde). Bos = Assets/Speed-Staes/Data/Build/ReleaseBuildConfig.asset.
.PARAMETER TimeoutMinutes
    Unity bu surede bitmezse sonlandirilir (varsayilan 60; IL2CPP ~10 dk).
.EXAMPLE
    powershell -NoProfile -ExecutionPolicy Bypass -File scripts\android-release.ps1 -ProjectPath D:\Pundun\speed-starts-verify-1
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$ProjectPath,
    [string]$OutFile,
    [string]$ConfigAsset,
    [int]$TimeoutMinutes = 60
)

$ErrorActionPreference = 'Stop'

try {
    $depo = Split-Path -Parent $PSScriptRoot
    $proje = [System.IO.Path]::GetFullPath($ProjectPath.Trim().Trim('"')).TrimEnd('\')
    if (-not (Test-Path -LiteralPath (Join-Path $proje 'Assets') -PathType Container)) {
        Write-Host "SONUC: DOGRULANAMADI (exit 11) - proje yok / Assets\ yok: $proje"; exit 11
    }
    # Reuse the verify gate's helpers (dot-source loads functions only).
    . (Join-Path $PSScriptRoot 'unity-verify.ps1')
    $surum = Get-ProjeSurumu $proje
    $unityExe = Resolve-UnityExe -Surum $surum
    if (-not $unityExe) {
        Write-Host "SONUC: DOGRULANAMADI (exit 10) - surum $surum icin TAM Unity kurulumu yok"; exit 10
    }
    $kilit = Test-Path -LiteralPath (Join-Path $proje 'Temp\UnityLockfile')
    $acanlar = @(Find-ProjeyiAcanUnity -Proje $proje -KilitVar $kilit)
    if ($acanlar.Count -gt 0) {
        Write-Host "SONUC: DOGRULANAMADI (exit 12) - Editor projeyi acik tutuyor; kopyada calistir"; exit 12
    }

    $logDir = Join-Path $depo 'pipeline-logs'
    New-Item -ItemType Directory -Force -Path $logDir | Out-Null
    if ([string]::IsNullOrWhiteSpace($OutFile)) { $OutFile = Join-Path $logDir 'android\SpeedStars-release.aab' }
    $OutFile = [System.IO.Path]::GetFullPath($OutFile)
    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $OutFile) | Out-Null
    $log = Join-Path $logDir ("android-release-{0}.log" -f (Get-Date -Format 'yyyy-MM-dd_HHmmss'))

    $argumanlar = @('-batchmode', '-nographics', '-projectPath', $proje, '-buildTarget', 'Android',
        '-executeMethod', 'SpeedStars.Tools.Build.ReleaseBuild.Run', '-buildOut', $OutFile, '-logFile', $log)
    if (-not [string]::IsNullOrWhiteSpace($ConfigAsset)) { $argumanlar += @('-config', $ConfigAsset) }
    $satir = (@($argumanlar | ForEach-Object { ConvertTo-KomutArg $_ }) -join ' ')
    Write-Host "Unity: $unityExe"
    Write-Host "Proje: $proje  (surum $surum)"
    Write-Host "Cikti: $OutFile"
    Write-Host "Log:   $log"
    # Report only whether the signing variables exist; never their values.
    $ks = if ([string]::IsNullOrEmpty($env:SPEEDSTARS_KEYSTORE_PASS)) { 'YOK' } else { 'var' }
    $ky = if ([string]::IsNullOrEmpty($env:SPEEDSTARS_KEY_PASS)) { 'YOK' } else { 'var' }
    Write-Host "Imza ortam degiskenleri: SPEEDSTARS_KEYSTORE_PASS=$ks SPEEDSTARS_KEY_PASS=$ky"
    Write-Host "Build basliyor (zaman asimi $TimeoutMinutes dk)..."

    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    $p = Start-Process -FilePath $unityExe -ArgumentList $satir -PassThru -NoNewWindow
    $null = $p.Handle  # PowerShell 5.1: without the cached handle ExitCode stays empty after the process ends
    $bitti = $p.WaitForExit($TimeoutMinutes * 60 * 1000)
    if (-not $bitti) {
        try { Stop-Process -Id $p.Id -Force } catch { }
        Write-Host "SONUC: DOGRULANAMADI (exit 13) - zaman asimi ($TimeoutMinutes dk); Unity sonlandirildi. Log: $log"
        exit 13
    }
    $p.WaitForExit()
    $cikis = $p.ExitCode
    Write-Host ("Unity cikis kodu: {0}  ({1:N0} sn)" -f $cikis, $sw.Elapsed.TotalSeconds)

    $metin = Read-LogMetni $log
    if ([string]::IsNullOrEmpty($metin)) {
        Write-Host "SONUC: DOGRULANAMADI (exit 13) - Unity log yazmadi: $log"; exit 13
    }

    Write-Host "--- ANDROID satirlari ---"
    Write-EslesenSatirlar $metin '^ANDROID_[A-Z_]+' 40
    $csDesen = 'error CS\d+'
    if ($metin -match $csDesen) {
        Write-Host "--- error CS satirlari (en fazla 20) ---"
        Write-EslesenSatirlar $metin $csDesen 20
    }

    $tani = Get-LogTeshisi $metin
    if ($tani.Upm -or $tani.PaketAg -or $tani.Lisans -or $tani.Coklu) {
        Write-Host "UYARI: log'da ortam imzasi var (UPM/paket/lisans/coklu Unity); sonuc guvenilmez olabilir. Log: $log"
    }
    if ([regex]::IsMatch($metin, '(?m)^ANDROID_RELEASE_UNSIGNED')) {
        Write-Host "UYARI: debug imza kullanildi (ANDROID_RELEASE_UNSIGNED); magazaya yuklenemez."
    }
    if ([regex]::IsMatch($metin, '(?m)^ANDROID_RELEASE_RESTORE_FAIL')) {
        Write-Host "UYARI: bir ayar geri yazilamadi (ANDROID_RELEASE_RESTORE_FAIL); kopyanin ProjectSettings'ini kontrol et."
    }
    $ok = [regex]::IsMatch($metin, '(?m)^ANDROID_RELEASE_OK')
    # The method exits 0/1 itself; an empty exit code (handle lost) is not a failure when the OK line and the file are there.
    if ($ok -and ($cikis -eq 0 -or $null -eq $cikis) -and (Test-Path -LiteralPath $OutFile -PathType Leaf)) {
        Write-Host "SONUC: YESIL (exit 0) - cikti: $OutFile"; exit 0
    }
    Write-Host "SONUC: KIRMIZI (exit 1) - build basarisiz (Unity cikis $cikis). Log: $log"
    exit 1
} catch {
    Write-Host "HATA (script ic hatasi): $($_.Exception.Message)"
    Write-Host "  $($_.InvocationInfo.PositionMessage)"
    exit 19
}
