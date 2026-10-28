<#
.SYNOPSIS
    Dogrulama kapisi (agent-kit v2): Unity projesini batchmode'da derler, istenirse EditMode testlerini kosar.

.DESCRIPTION
    Sozlesme: agent-kit\scripts\verify.ps1.ORNEK. Kapinin UC sonucu vardir:
      0       YESIL          her sey tamam
      1..9    KIRMIZI        gercek kod/test hatasi -> turu BLOKLAR
      10..19  DOGRULANAMADI  ortam sorunu -> turu bloklamaz ama SAYILIR
    Cagiranlar: kopya.ps1 -Dogrula (-ProjectPath <kopya> [-CompileOnly] [-TestFilter], depo kokunden) ve
    hook-post-agent.ps1 (yalniz -CompileOnly, ProjectPath yok -> calisma klasoru; 10..19 = ortam).

    CIKIS KODLARI
      kod  sinif          anlam
      ---  -------------  --------------------------------------------------------------------------
      0    YESIL          derleme temiz; testler gecti / -CompileOnly / test framework kurulu degil
      1    KIRMIZI        derleme hatasi: log'da "error CS####" ya da "Scripts have compiler errors",
                          ya da Unity cikisi != 0 ve log'da bilinen bir ortam imzasi yok
      2    KIRMIZI        testler calismadi: sonuc XML'i yok / okunamadi (ortam imzasi yokken)
      3    KIRMIZI        test kaldi: failed > 0 ya da Unity test cikisi != 0 (kalan testler listelenir)
      10   DOGRULANAMADI  proje surumu icin TAM Unity kurulumu yok
      11   DOGRULANAMADI  proje klasoru / Assets / ProjectSettings\ProjectVersion.txt yok
      12   DOGRULANAMADI  Editor projeyi acik tutuyor (kilit + surec, ya da log'da "Multiple Unity instances")
      13   DOGRULANAMADI  Unity calisti ama sonuc guvenilmez: UPM yerel sunucusu yok, lisans hatasi,
                          paket cozumleme ag hatasi, Unity baslatilamadi ya da hic log yazmadi
      19   DOGRULANAMADI  bu script'in beklenmeyen ic hatasi (kod hakkinda bir sey soylemez)

    NEDENLER
    - Ortam ve kod hatasi ayri: 25-28.08.2026'da 21 ardisik ortam hatasi "DERLEME BASARISIZ" diye
      kaydedildi, agent'lar saglam kodu duzeltmeye calisti.
    - Unity.exe'nin varligi yetmez: bu makinede 2022.3.62f2 klasorunde Editor\Data\Resources\
      PackageManager\Server yok; batchmode "Could not find Unity Package Manager local server" deyip
      1 ile cikar. Tam kurulum 2022.3.62f2-x86_64. Baska surume DUSULMEZ (proje yukseltilir, Library
      bastan import edilir) -> 10. $env:UNITY_EXE (Unity.exe yolu) her seyin onundedir.
    - Editor acik kontrolu Unity'den ONCE: Editor acikken batchmode ayni projeyi acamaz. Kilit dosyasi
      tek basina yetmez (yarida kesilen batchmode bayat kilit birakir): -projectPath'i bu proje olan
      Unity.exe sureci aranir (buyuk/kucuk harf ve / \ farki yok). Kilit dururken komut satiri okunamayan
      ya da -projectPath'siz bir Unity varsa temkinli: acik sayilir. Bayat kilit SILINMEZ (acilmakta olan
      bir Editor'un olabilir); uyarilir, Unity onu kendisi devralir.
    - -executeMethod yok: projede CompileCheck editor script'i yok. -quit ile acip kapamak derlemeyi
      zaten yapar; derleme hatasinda Unity "Scripts have compiler errors" yazar ve != 0 ile cikar.
    - Paket cozumleme ag hatasi CS hatalarindan once gelir: git paketi (unity-mcp) inmezse ona bagli kod
      CS0246 verir; bu kod hatasi degil. Lisans imzasina yalniz Unity != 0 ile cikip CS hatasi yokken
      bakilir: saglikli log'larda da zararsiz "LicensingClient ... failed" turu satirlar olur.
    - Log'lar CALISMA KLASORUNUN pipeline-logs\ altina (kopya.ps1 depo kokunden cagirir: kopyanin log'u da
      ana depoda toplanir). Zaman damgali, her turden son N tutulur (28.08.2026: tek log siliniyordu,
      "son 3 derleme" sorusu cevaplanamiyordu). Son basarisizin kopyasi unity-verify-last.log
      (hook-post-agent.ps1 agent'a onu okutur).
    - 10/11/12'de Unity baslatilmaz ve hicbir dosya yazilmaz.
    - Dosya YALNIZ ASCII: Windows PowerShell 5.1 BOM'suz dosyayi ANSI kod sayfasiyla okur.

.PARAMETER ProjectPath
    Dogrulanacak Unity projesi. Bos = calisma klasoru.
.PARAMETER CompileOnly
    Yalniz derleme; testler kosulmaz (hook icin, hizli).
.PARAMETER TestFilter
    Yalniz eslesen EditMode testleri (Unity -testFilter: ';' ile ayrilmis adlar ya da regex). Bos = tam suite.
.PARAMETER TutulacakLogSayisi
    pipeline-logs altinda her turden (compile / test / test-results) tutulacak dosya sayisi.
.EXAMPLE
    powershell -NoProfile -ExecutionPolicy Bypass -File scripts\unity-verify.ps1 -CompileOnly
    powershell -NoProfile -ExecutionPolicy Bypass -File scripts\unity-verify.ps1 -ProjectPath <kopya> -TestFilter "Runner"
    . .\scripts\unity-verify.ps1; Resolve-UnityExe -Surum (Get-ProjeSurumu (Get-Location).Path)
        (dot-source: yalniz fonksiyonlar yuklenir, kapi kosmaz)
#>

[CmdletBinding()]
param(
    [string]$ProjectPath,
    [switch]$CompileOnly,
    [string]$TestFilter,
    [int]$TutulacakLogSayisi = 20
)

$ErrorActionPreference = 'Stop'

$script:HubKoku = 'C:\Program Files\Unity\Hub\Editor'
$script:LogDir = $null

# ------------------------------------------------------------------ log signatures
# Lines that mean "the environment failed", not "the code is broken".
$script:ImzaCoklu   = '(?i)Multiple Unity instances cannot open the same project|another Unity instance is running with this project open'
$script:ImzaUpm     = '(?i)Could not find Unity Package Manager local server|Cannot connect to Unity Package Manager local server'
$script:AgBelirtisi = 'ENOTFOUND|EAI_AGAIN|ETIMEDOUT|ECONNREFUSED|ECONNRESET|ENETUNREACH|getaddrinfo|Could not resolve host|unable to access|Failed to connect to|Connection timed out|network is unreachable'
# A network token alone is not enough (healthy logs have analytics/licensing timeouts): it must follow a package resolution failure.
$script:ImzaPaketAg = '(?i)(?:An error occurred while resolving packages|Project has invalid dependencies)[\s\S]{0,4000}?(?:' + $script:AgBelirtisi + ')'
$script:SatirPaketAg = '(?i)resolving packages|invalid dependencies|' + $script:AgBelirtisi
$script:ImzaLisans  = '(?i)No valid Unity Editor license|No ULF license found|LicensingClient[^\r\n]*fail|fail[^\r\n]*LicensingClient|Failed to (?:activate|update|validate)[^\r\n]*licen[cs]e'
$script:ImzaCs      = 'error CS\d+'
$script:ImzaDerleme = 'Scripts have compiler errors'
$script:ImzaGenel   = '(?i)\berror\b|exception|aborting|crash|fatal'

# ------------------------------------------------------------------ helpers

function Get-TamYol {
    param([string]$Yol, [string]$Taban)
    $y = $Yol.Trim().Trim('"') -replace '/', '\'
    if (-not [System.IO.Path]::IsPathRooted($y)) { $y = Join-Path $Taban $y }
    $y = [System.IO.Path]::GetFullPath($y)
    if ($y.Length -gt 3) { $y = $y.TrimEnd('\') }
    return $y
}

function ConvertTo-KarsilastirmaYolu {
    # Comparable form: lower case, '\' only, no doubled or trailing '\'. Leading '\\' (UNC) kept.
    param([string]$Yol)
    $y = $Yol.Trim().Trim('"') -replace '/', '\'
    $bas = ''
    if ($y.StartsWith('\\')) { $bas = '\\'; $y = $y.Substring(2) }
    $y = $bas + ($y -replace '\\{2,}', '\')
    try { if ([System.IO.Path]::IsPathRooted($y)) { $y = [System.IO.Path]::GetFullPath($y) } } catch { }
    if ($y.Length -gt 3) { $y = $y.TrimEnd('\') }
    return $y.ToLowerInvariant()
}

function ConvertTo-KomutArg {
    # Windows command line rules (CommandLineToArgvW). PS 5.1 Start-Process joins an array WITHOUT quoting,
    # so a path with a space would split; we build the line ourselves.
    param([string]$A)
    if ($A -eq '') { return '""' }
    if ($A -notmatch '[\s"]') { return $A }
    $sb = New-Object System.Text.StringBuilder
    [void]$sb.Append('"')
    $ters = 0
    foreach ($ch in $A.ToCharArray()) {
        if ($ch -eq [char]'\') { $ters++; continue }
        if ($ch -eq [char]'"') { [void]$sb.Append('\' * ($ters * 2 + 1)); [void]$sb.Append('"'); $ters = 0; continue }
        if ($ters -gt 0) { [void]$sb.Append('\' * $ters); $ters = 0 }
        [void]$sb.Append($ch)
    }
    if ($ters -gt 0) { [void]$sb.Append('\' * ($ters * 2)) }
    [void]$sb.Append('"')
    return $sb.ToString()
}

function Get-ProjeSurumu {
    # m_EditorVersion from ProjectSettings\ProjectVersion.txt, or $null.
    param([string]$Proje)
    $vf = Join-Path $Proje 'ProjectSettings\ProjectVersion.txt'
    if (-not (Test-Path -LiteralPath $vf -PathType Leaf)) { return $null }
    $m = [regex]::Match([System.IO.File]::ReadAllText($vf), '(?m)^\s*m_EditorVersion:\s*(\S+)')
    if ($m.Success) { return $m.Groups[1].Value }
    return $null
}

function Get-UnityKurulumEksigi {
    # '' = complete install. Unity.exe alone is not enough: without the UPM local server batchmode exits 1.
    param([string]$Exe)
    if (-not (Test-Path -LiteralPath $Exe -PathType Leaf)) { return 'Unity.exe yok' }
    $server = Join-Path (Split-Path -Parent $Exe) 'Data\Resources\PackageManager\Server'
    if (-not (Test-Path -LiteralPath $server -PathType Container)) { return 'Editor\Data\Resources\PackageManager\Server yok' }
    return ''
}

function Resolve-UnityExe {
    # Order: $env:UNITY_EXE (if complete) -> Hub install whose name is the project version or starts with
    # it ("2022.3.62f2", "2022.3.62f2-x86_64"), exact name first, first COMPLETE one wins. Else $null.
    param([string]$Surum, [string]$HubKoku = $script:HubKoku)
    if (-not [string]::IsNullOrWhiteSpace($env:UNITY_EXE)) {
        $e = $env:UNITY_EXE.Trim().Trim('"')
        $eksik = Get-UnityKurulumEksigi $e
        if (-not $eksik) {
            Write-Host "  Unity secimi: UNITY_EXE ortam degiskeni (surum kontrolu yapilmadi)"
            return $e
        }
        Write-Host "UYARI: UNITY_EXE kullanilamiyor ($eksik): $e ; Hub kurulumlarina bakiliyor."
    }
    if ([string]::IsNullOrWhiteSpace($Surum)) { return $null }
    if (-not (Test-Path -LiteralPath $HubKoku -PathType Container)) {
        Write-Host "  Unity Hub editor klasoru yok: $HubKoku"
        return $null
    }
    $hepsi = @(Get-ChildItem -LiteralPath $HubKoku -Directory -ErrorAction SilentlyContinue)
    $desen = '^' + [regex]::Escape($Surum) + '(?:$|[^0-9A-Za-z])'
    $adaylar = @($hepsi | Where-Object { $_.Name -match $desen } |
        Sort-Object -Property @{ Expression = { if ($_.Name -eq $Surum) { 0 } else { 1 } } }, Name)
    foreach ($d in $adaylar) {
        $exe = Join-Path $d.FullName 'Editor\Unity.exe'
        $eksik = Get-UnityKurulumEksigi $exe
        if (-not $eksik) { return $exe }
        Write-Host "  atlandi: $($d.Name) (eksik kurulum: $eksik)"
    }
    Write-Host "  '$Surum' ile baslayan TAM kurulum yok. $HubKoku altinda: $((@($hepsi | ForEach-Object { $_.Name })) -join ', ')"
    return $null
}

function Get-KomutSatiriProjeYollari {
    # Values of every -projectPath argument (flag may itself be quoted, as in AssetImportWorker lines).
    param([string]$KomutSatiri)
    $liste = @()
    foreach ($m in [regex]::Matches($KomutSatiri, '(?i)(?:^|\s)"?-projectpath"?\s+(?:"([^"]*)"|(\S+))')) {
        $deger = $m.Groups[2].Value
        if ($m.Groups[1].Success) { $deger = $m.Groups[1].Value }
        if ($deger) { $liste += (ConvertTo-KarsilastirmaYolu $deger) }
    }
    return $liste
}

function Find-ProjeyiAcanUnity {
    # Unity.exe processes that hold this project. Cautious only while the lockfile exists: then an
    # unreadable command line, or one without -projectPath, counts as "open".
    param([string]$Proje, [bool]$KilitVar)
    $hedef = ConvertTo-KarsilastirmaYolu $Proje
    $bulunan = New-Object System.Collections.Generic.List[object]
    try {
        $surecler = @(Get-CimInstance -ClassName Win32_Process -Filter "Name='Unity.exe'" -ErrorAction Stop)
    } catch {
        if ($KilitVar) {
            foreach ($gp in @(Get-Process -Name 'Unity' -ErrorAction SilentlyContinue)) {
                $bulunan.Add([pscustomobject]@{ Pid = $gp.Id; Neden = 'surec komut satirlari okunamadi (WMI); kilit duruyor: temkinli, acik sayildi' })
            }
        }
        return $bulunan.ToArray()
    }
    foreach ($s in $surecler) {
        $cl = [string]$s.CommandLine
        if ([string]::IsNullOrWhiteSpace($cl)) {
            if ($KilitVar) { $bulunan.Add([pscustomobject]@{ Pid = $s.ProcessId; Neden = 'komut satiri okunamadi (baska kullanici / yonetici?); kilit duruyor: temkinli, acik sayildi' }) }
            continue
        }
        $yollar = @(Get-KomutSatiriProjeYollari $cl)
        if ($yollar.Count -gt 0) {
            if ($yollar -contains $hedef) { $bulunan.Add([pscustomobject]@{ Pid = $s.ProcessId; Neden = '-projectPath bu proje' }) }
            continue
        }
        $clNorm = ($cl -replace '/', '\').ToLowerInvariant()
        if ([regex]::IsMatch($clNorm, [regex]::Escape($hedef) + '(?=["''\s\\]|$)')) {
            $bulunan.Add([pscustomobject]@{ Pid = $s.ProcessId; Neden = 'komut satiri bu proje yolunu iceriyor' })
        } elseif ($KilitVar) {
            $bulunan.Add([pscustomobject]@{ Pid = $s.ProcessId; Neden = '-projectPath yok, hangi projeyi actigi bilinmiyor; kilit duruyor: temkinli, acik sayildi' })
        }
    }
    return $bulunan.ToArray()
}

function Read-LogMetni {
    # Whole log as text (UTF-8), $null if missing. Shared read: a lingering child may still hold it.
    param([string]$Yol)
    if (-not (Test-Path -LiteralPath $Yol -PathType Leaf)) { return $null }
    $fs = [System.IO.File]::Open($Yol, [System.IO.FileMode]::Open, [System.IO.FileAccess]::Read, ([System.IO.FileShare]'ReadWrite, Delete'))
    try {
        $sr = New-Object System.IO.StreamReader($fs, [System.Text.Encoding]::UTF8, $true)
        return $sr.ReadToEnd()
    } finally { $fs.Dispose() }
}

function Get-LogTeshisi {
    param([string]$Metin)
    if ($null -eq $Metin) { $Metin = '' }
    $cs = @([regex]::Matches($Metin, '(?m)^.*' + $script:ImzaCs + '.*$') | ForEach-Object { $_.Value.Trim() } | Select-Object -Unique)
    return [pscustomobject]@{
        Coklu         = [regex]::IsMatch($Metin, $script:ImzaCoklu)
        Upm           = [regex]::IsMatch($Metin, $script:ImzaUpm)
        PaketAg       = [regex]::IsMatch($Metin, $script:ImzaPaketAg)
        Lisans        = [regex]::IsMatch($Metin, $script:ImzaLisans)
        CsSayisi      = $cs.Count
        DerlemeHatasi = (($cs.Count -gt 0) -or [regex]::IsMatch($Metin, $script:ImzaDerleme))
    }
}

function Write-EslesenSatirlar {
    # First N distinct matching lines (the "first 15 errors" block).
    param([string]$Metin, [string]$Desen, [int]$Adet = 15)
    if ([string]::IsNullOrEmpty($Metin)) { return }
    $satirlar = @($Metin -split "\r?\n" | Where-Object { $_ -match $Desen } | ForEach-Object { $_.Trim() } |
        Where-Object { $_ } | Select-Object -Unique | Select-Object -First $Adet)
    foreach ($s in $satirlar) {
        if ($s.Length -gt 300) { $s = $s.Substring(0, 300) + ' ...' }
        Write-Host "    $s"
    }
}

function Save-SonBasarisiz {
    param([string]$Log)
    if (-not $script:LogDir -or -not $Log -or -not (Test-Path -LiteralPath $Log -PathType Leaf)) { return }
    try { Copy-Item -LiteralPath $Log -Destination (Join-Path $script:LogDir 'unity-verify-last.log') -Force }
    catch { Write-Host "UYARI: unity-verify-last.log yazilamadi: $($_.Exception.Message)" }
}

function Invoke-LogBudama {
    # Keep the newest N of each kind (the current run included).
    if (-not $script:LogDir -or -not (Test-Path -LiteralPath $script:LogDir -PathType Container)) { return }
    $tut = [Math]::Max(1, $TutulacakLogSayisi)
    $dosyalar = @(Get-ChildItem -LiteralPath $script:LogDir -File -ErrorAction SilentlyContinue)
    foreach ($desen in @('compile-*.log', 'test-*.log', 'test-results-*.xml')) {
        $dosyalar | Where-Object { $_.Name -like $desen } | Sort-Object LastWriteTime -Descending |
            Select-Object -Skip $tut | Remove-Item -Force -ErrorAction SilentlyContinue
    }
}

function Stop-Verify {
    param([int]$Kod, [string]$Ozet)
    if ($Ozet) { Write-Host $Ozet }
    try { Invoke-LogBudama } catch { }
    exit $Kod
}

function Invoke-Unity {
    # Runs Unity and waits. Returns its exit code ($null if unknown). Throws if it cannot start.
    param([string]$Exe, [string[]]$Argumanlar)
    $satir = (@($Argumanlar | ForEach-Object { ConvertTo-KomutArg $_ }) -join ' ')
    Write-Host "  > $(ConvertTo-KomutArg $Exe) $satir"
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    $p = Start-Process -FilePath $Exe -ArgumentList $satir -Wait -PassThru -NoNewWindow
    $sw.Stop()
    $kod = $null
    if ($p) { $kod = $p.ExitCode }
    Write-Host ("  Unity cikis kodu: {0}  ({1:N0} sn)" -f $kod, $sw.Elapsed.TotalSeconds)
    return $kod
}

# Dot-sourced: load the functions only (lets Resolve-UnityExe / Find-ProjeyiAcanUnity be checked alone).
if ($MyInvocation.InvocationName -eq '.') { return }

# ------------------------------------------------------------------ gate

try {
    $calismaKlasoru = (Get-Location -PSProvider FileSystem).ProviderPath
    if ([string]::IsNullOrWhiteSpace($ProjectPath)) { $ProjectPath = $calismaKlasoru }
    $proje = $null
    try { $proje = Get-TamYol $ProjectPath $calismaKlasoru } catch { }

    # --- 1. ENVIRONMENT (no Unity started, no file written) ------------------------------------
    if (-not $proje -or -not (Test-Path -LiteralPath $proje -PathType Container)) {
        Stop-Verify 11 "SONUC: DOGRULANAMADI (exit 11) - ORTAM: proje klasoru yok: $ProjectPath"
    }
    if (-not (Test-Path -LiteralPath (Join-Path $proje 'Assets') -PathType Container)) {
        Stop-Verify 11 "SONUC: DOGRULANAMADI (exit 11) - ORTAM: Unity projesi degil (Assets\ yok): $proje"
    }
    $surum = Get-ProjeSurumu $proje
    if (-not $surum) {
        Stop-Verify 11 "SONUC: DOGRULANAMADI (exit 11) - ORTAM: ProjectSettings\ProjectVersion.txt yok ya da m_EditorVersion okunamadi: $proje"
    }

    $unityExe = Resolve-UnityExe -Surum $surum
    if (-not $unityExe) {
        Stop-Verify 10 "SONUC: DOGRULANAMADI (exit 10) - ORTAM: proje surumu $surum icin TAM Unity kurulumu yok (Editor\Unity.exe + Editor\Data\Resources\PackageManager\Server). Gerekirse `$env:UNITY_EXE."
    }
    Write-Host "Unity: $unityExe"
    Write-Host "Proje: $proje  (surum $surum)"

    $kilit = Join-Path $proje 'Temp\UnityLockfile'
    $kilitVar = Test-Path -LiteralPath $kilit
    $acanlar = @(Find-ProjeyiAcanUnity -Proje $proje -KilitVar $kilitVar)
    if ($acanlar.Count -gt 0) {
        Write-Host "HATA (ORTAM): Unity bu projeyi acik tutuyor; batchmode ayni projeyi ikinci kez acamaz. Unity baslatilmadi."
        foreach ($a in $acanlar) { Write-Host "  pid $($a.Pid): $($a.Neden)" }
        if ($kilitVar) { Write-Host "  kilit: $kilit" }
        Write-Host "  COZUM: Editor'u kapat ya da dogrulama kopyasinda calistir (scripts\kopya.ps1 -Al / -Senkron / -Dogrula)."
        Stop-Verify 12 "SONUC: DOGRULANAMADI (exit 12) - ORTAM: Editor projeyi acik tutuyor (kod hakkinda bir sey soylemez)"
    }
    if ($kilitVar) {
        Write-Host "UYARI: bayat Temp\UnityLockfile (bu projeyi acan Unity sureci yok); silinmedi, devam ediliyor."
    }

    # --- logs ---------------------------------------------------------------------------------
    $script:LogDir = Join-Path $calismaKlasoru 'pipeline-logs'
    New-Item -ItemType Directory -Force -Path $script:LogDir | Out-Null
    $stamp      = Get-Date -Format 'yyyy-MM-dd_HHmmss_fff'
    $derlemeLog = Join-Path $script:LogDir "compile-$stamp.log"
    $testLog    = Join-Path $script:LogDir "test-$stamp.log"
    $sonucXml   = Join-Path $script:LogDir "test-results-$stamp.xml"

    # --- 2. COMPILE ---------------------------------------------------------------------------
    Write-Host "[1/2] Derleme (batchmode -quit)..."
    try {
        $cikis = Invoke-Unity $unityExe @('-batchmode', '-quit', '-nographics', '-projectPath', $proje, '-logFile', $derlemeLog)
    } catch {
        Write-Host "HATA (ORTAM): Unity baslatilamadi: $($_.Exception.Message)"
        Stop-Verify 13 "SONUC: DOGRULANAMADI (exit 13) - ORTAM: Unity baslatilamadi"
    }
    $metin = Read-LogMetni $derlemeLog
    if ([string]::IsNullOrEmpty($metin)) {
        Stop-Verify 13 "SONUC: DOGRULANAMADI (exit 13) - ORTAM: Unity log yazmadi (cikis $cikis): $derlemeLog"
    }
    if ($null -eq $cikis) {
        # Exit code lost (rare Start-Process quirk): fall back to Unity's own success line.
        if ($metin -match 'Exiting batchmode successfully') { $cikis = 0 } else { $cikis = -1 }
    }
    $t = Get-LogTeshisi $metin
    $ortamNeden = $null; $ortamKod = 13; $ortamDesen = $null
    if ($t.Coklu) {
        $ortamNeden = 'baska bir Unity bu projeyi acik tutuyor (Multiple Unity instances)'; $ortamKod = 12; $ortamDesen = $script:ImzaCoklu
    } elseif ($t.Upm) {
        $ortamNeden = 'Unity Package Manager yerel sunucusu yok / baglanilamadi (eksik kurulum?)'; $ortamDesen = $script:ImzaUpm
    } elseif ($t.DerlemeHatasi -and $t.PaketAg) {
        $ortamNeden = 'paketler agdan cozumlenemedi; CS hatalari eksik paketten olabilir'; $ortamDesen = $script:SatirPaketAg
    } elseif (-not $t.DerlemeHatasi -and $cikis -ne 0 -and $t.PaketAg) {
        $ortamNeden = 'paketler agdan cozumlenemedi'; $ortamDesen = $script:SatirPaketAg
    } elseif (-not $t.DerlemeHatasi -and $cikis -ne 0 -and $t.Lisans) {
        $ortamNeden = 'Unity lisans hatasi'; $ortamDesen = '(?i)licen[cs]'
    }
    if ($ortamNeden) {
        Save-SonBasarisiz $derlemeLog
        Write-Host "HATA (ORTAM): $ortamNeden. Log: $derlemeLog"
        Write-EslesenSatirlar $metin $ortamDesen 15
        Stop-Verify $ortamKod "SONUC: DOGRULANAMADI (exit $ortamKod) - ORTAM: $ortamNeden (kod hakkinda bir sey soylemez)"
    }
    if ($t.DerlemeHatasi -or $cikis -ne 0) {
        Save-SonBasarisiz $derlemeLog
        Write-Host "DERLEME BASARISIZ. C# hata satiri: $($t.CsSayisi), Unity cikis kodu: $cikis"
        Write-Host "Log: $derlemeLog"
        Write-Host "--- ilk hatalar (en fazla 15) ---"
        if ($t.CsSayisi -gt 0) { Write-EslesenSatirlar $metin $script:ImzaCs 15 }
        else { Write-EslesenSatirlar $metin $script:ImzaGenel 15 }
        Stop-Verify 1 "SONUC: KIRMIZI (exit 1) - derleme hatasi"
    }
    Write-Host "  derleme OK  ($derlemeLog)"

    if ($CompileOnly) { Stop-Verify 0 "SONUC: YESIL (exit 0) - derleme temiz (-CompileOnly, testler kosulmadi)" }

    # --- 3. TESTS -----------------------------------------------------------------------------
    $manifest = Join-Path $proje 'Packages\manifest.json'
    $tfVar = $false
    if (Test-Path -LiteralPath $manifest -PathType Leaf) {
        $tfVar = [regex]::IsMatch([System.IO.File]::ReadAllText($manifest), '"com\.unity\.test-framework"\s*:')
    }
    if (-not $tfVar) {
        Stop-Verify 0 "SONUC: YESIL (exit 0) - derleme temiz; test framework kurulu degil, testler kosulmadi (Packages\manifest.json'da com.unity.test-framework yok)"
    }

    Write-Host "[2/2] EditMode testleri..."
    $testArg = @('-batchmode', '-nographics', '-projectPath', $proje, '-runTests', '-testPlatform', 'EditMode', '-testResults', $sonucXml, '-logFile', $testLog)
    if (-not [string]::IsNullOrWhiteSpace($TestFilter)) {
        $testArg += @('-testFilter', $TestFilter)
        Write-Host "  filtre: $TestFilter (tam suite DEGIL)"
    }
    try {
        $tCikis = Invoke-Unity $unityExe $testArg
    } catch {
        Write-Host "HATA (ORTAM): Unity baslatilamadi: $($_.Exception.Message)"
        Stop-Verify 13 "SONUC: DOGRULANAMADI (exit 13) - ORTAM: Unity (test) baslatilamadi"
    }

    if (-not (Test-Path -LiteralPath $sonucXml -PathType Leaf) -or (Get-Item -LiteralPath $sonucXml).Length -eq 0) {
        $tMetin = Read-LogMetni $testLog
        Save-SonBasarisiz $testLog
        if ([string]::IsNullOrEmpty($tMetin)) {
            Stop-Verify 13 "SONUC: DOGRULANAMADI (exit 13) - ORTAM: Unity test kosusunda log da yazmadi (cikis $tCikis): $testLog"
        }
        $tt = Get-LogTeshisi $tMetin
        if ($tt.Coklu) {
            Write-EslesenSatirlar $tMetin $script:ImzaCoklu 5
            Stop-Verify 12 "SONUC: DOGRULANAMADI (exit 12) - ORTAM: baska bir Unity projeyi acti (Multiple Unity instances); testler kosulmadi"
        }
        if ($tt.Upm -or $tt.PaketAg -or $tt.Lisans) {
            Write-EslesenSatirlar $tMetin ($script:ImzaUpm + '|' + $script:SatirPaketAg + '|(?i)licen[cs]') 15
            Stop-Verify 13 "SONUC: DOGRULANAMADI (exit 13) - ORTAM: UPM / paket / lisans hatasi; testler kosulmadi. Log: $testLog"
        }
        if ($tt.DerlemeHatasi) {
            Write-EslesenSatirlar $tMetin $script:ImzaCs 15
            Stop-Verify 1 "SONUC: KIRMIZI (exit 1) - test kosusunda derleme hatasi. Log: $testLog"
        }
        Write-Host "TESTLER CALISMADI: sonuc XML'i yok (Unity cikis $tCikis). Log: $testLog"
        Write-EslesenSatirlar $tMetin $script:ImzaGenel 15
        Stop-Verify 2 "SONUC: KIRMIZI (exit 2) - testler calismadi"
    }

    $xml = New-Object System.Xml.XmlDocument
    try { $xml.Load($sonucXml) } catch {
        Save-SonBasarisiz $testLog
        Stop-Verify 2 "SONUC: KIRMIZI (exit 2) - test sonuc XML'i okunamadi ($($_.Exception.Message)): $sonucXml"
    }
    $kok = $xml.DocumentElement
    if ($null -eq $kok -or $kok.Name -ne 'test-run') {
        Save-SonBasarisiz $testLog
        Stop-Verify 2 "SONUC: KIRMIZI (exit 2) - test sonuc XML'inde test-run yok: $sonucXml"
    }
    $toplam = 0; $gecen = 0; $kalan = 0; $atlanan = 0
    [void][int]::TryParse($kok.GetAttribute('total'), [ref]$toplam)
    [void][int]::TryParse($kok.GetAttribute('passed'), [ref]$gecen)
    [void][int]::TryParse($kok.GetAttribute('failed'), [ref]$kalan)
    [void][int]::TryParse($kok.GetAttribute('skipped'), [ref]$atlanan)
    Write-Host "  toplam: $toplam  gecen: $gecen  kalan: $kalan  atlanan: $atlanan"
    Write-Host "  sonuc dosyasi: $sonucXml"
    if ($null -eq $tCikis) { $tCikis = 0 }   # exit code lost: trust the XML

    $kalanlar = @($xml.SelectNodes("//test-case[@result='Failed']"))
    if ($kalan -gt 0 -or $tCikis -ne 0 -or $kalanlar.Count -gt 0) {
        Save-SonBasarisiz $testLog
        Write-Host "TESTLER BASARISIZ (kalan $kalan, Unity cikis $tCikis). Log: $testLog"
        foreach ($n in @($kalanlar | Select-Object -First 50)) {
            $mesaj = ''
            $mn = $n.SelectSingleNode('failure/message')
            if ($mn) { $mesaj = (($mn.InnerText -split "\r?\n") | Where-Object { $_.Trim() } | Select-Object -First 1) }
            if ($mesaj) { $mesaj = $mesaj.Trim(); if ($mesaj.Length -gt 200) { $mesaj = $mesaj.Substring(0, 200) + ' ...' }; $mesaj = "  -- $mesaj" }
            Write-Host "  X $($n.GetAttribute('fullname'))$mesaj"
        }
        if ($kalanlar.Count -gt 50) { Write-Host "  ... ve $($kalanlar.Count - 50) test daha (XML'e bak)" }
        if ($kalanlar.Count -eq 0) {
            Write-Host "  (XML'de kalan test yok; Unity cikisi $tCikis) log'daki hata satirlari:"
            Write-EslesenSatirlar (Read-LogMetni $testLog) $script:ImzaGenel 15
        }
        Stop-Verify 3 "SONUC: KIRMIZI (exit 3) - test kaldi"
    }
    if ($toplam -eq 0) {
        if ($TestFilter) { Write-Host "  UYARI: hic test kosulmadi - filtre '$TestFilter' hicbir testle eslesmedi mi?" }
        else { Write-Host "  UYARI: hic test kosulmadi (projede EditMode testi yok mu?)" }
    }
    Stop-Verify 0 "SONUC: YESIL (exit 0) - derleme temiz, $gecen test gecti"
} catch {
    Write-Host "HATA (beklenmeyen, dogrulama script'inin ic hatasi): $($_.Exception.Message)"
    Write-Host "  $($_.InvocationInfo.PositionMessage)"
    Write-Host "SONUC: DOGRULANAMADI (exit 19) - script ic hatasi; kod hakkinda bir sey soylemez"
    exit 19
}
