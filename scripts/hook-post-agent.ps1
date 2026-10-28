# hook-post-agent.ps1 - agent-kit v2
# SubagentStop hook: agent isini bitirince HIZLI derleme kontrolu (DogrulamaScripti -CompileOnly).
# Tam test kosusu pahalidir, onu supervisor yapar. Amac: derlenmeyen kodun bir sonraki agent'in
# uzerine yigilmasini engellemek.
#
# Uc sonuc (verify.ps1.ORNEK sozlesmesi):
#   0       YESIL          -> "derleme OK", cikis 0
#   10..19  DOGRULANAMADI  -> ortam sorunu (Unity yok / proje yok / Editor acik ...), turu BLOKLAMAZ, sayilir
#   digeri  KIRMIZI        -> gercek derleme/test hatasi, cikis 2 (agent durdurulur)
# 28.08.2026: 21 ardisik ortam hatasi "DERLEME BASARISIZ" diye kaydedildi, agent'lar saglam kodu duzeltmeye
# calisti. Bu yuzden ortam ile kod hatasi AYRI tutulur.
#
# Batchmode ACILMAYAN durumlar (hepsi cikis 0):
#   - git worktree icinde (.claude\worktrees\ ya da ana olmayan herhangi bir bagli worktree): taze Library'yi
#     sifirdan kurar (dakikalar, agir CPU); supervizor birlestirince tek sefer dogrular. (19.10.2026)
#   - Ana projede Temp\UnityLockfile var: Unity.exe sureci varsa "Editor acik", yoksa "bayat kilit".
#     Ikisinde de batchmode acilmaz (Editor kapaliyken acilan batchmode projeyi ~2 dk kilitleyip kullanicinin
#     Unity'yi acmasini engelledi; bayat kilidi hook silmez, kullanicinin acilmakta olan Editor'u olabilir).
# Ic hata = FAIL-OPEN (logla, cikis 0). Hiz: tek git cagrisi (worktree list).

$ErrorActionPreference = 'Continue'
$null = @($input)                  # girdiyi tuket, kullanmasak da

$HookAdi = 'post-agent'
$script:LogKok = (Get-Location).Path

function ConvertTo-HookTamYol([string]$Yol, [string]$Taban) {
    $y = $Yol.Trim() -replace '/', '\'
    if (-not [System.IO.Path]::IsPathRooted($y)) { $y = Join-Path $Taban $y }
    $y = [System.IO.Path]::GetFullPath($y)
    if ($y.Length -gt 3) { $y = $y.TrimEnd('\') }
    return $y
}

function Get-HookWorktreeListesi([string]$Baslangic) {
    $liste = New-Object 'System.Collections.Generic.List[string]'
    $git = Get-GitExe
    foreach ($l in @(& $git -C $Baslangic worktree list --porcelain 2>$null)) {
        if ($l -like 'worktree *') { $liste.Add((ConvertTo-HookTamYol $l.Substring(9) $Baslangic)) }
    }
    return ,$liste
}

function Find-HookWorktree([string]$Yol, $Liste) {
    $y = $Yol.ToLowerInvariant()
    $en = $null
    foreach ($w in $Liste) {
        $wl = $w.ToLowerInvariant().TrimEnd('\')
        if ($y -eq $wl -or $y.StartsWith($wl + '\')) {
            if (-not $en -or $w.Length -gt $en.Length) { $en = $w }
        }
    }
    return $en
}

function Test-HookUnitySureci([string]$Proje) {
    # Bu projeyi acan Unity.exe var mi? Makinede baska projeler de acik olabilir: -projectPath ile eslestir.
    $p = $Proje.ToLowerInvariant().TrimEnd('\')
    $desen = [regex]::Escape($p) + '(?=["''\s\\]|$)'
    foreach ($s in @(Get-CimInstance Win32_Process -Filter "Name='Unity.exe'" -ErrorAction SilentlyContinue)) {
        $cl = [string]$s.CommandLine
        if (-not $cl) { return $true }                 # komut satiri okunamadi: acik say (temkinli)
        if ([regex]::IsMatch($cl.Replace('/', '\').ToLowerInvariant(), $desen)) { return $true }
    }
    return $false
}

function Write-HookDogrulanamadi([string]$Log, [string]$Etiket, [string]$Sebep) {
    Add-Content -Path $Log -Value "$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss') DOGRULANAMADI - ORTAM ($Etiket): $Sebep"
    # Ardisik kac turdur dogrulama yapilamadi: bu sayi buyuyorsa derleme kapisi fiilen kapalidir.
    $ardisik = 0
    $son = @(Get-Content -Path $Log -Tail 200)
    for ($i = $son.Count - 1; $i -ge 0; $i--) {
        if ([string]$son[$i] -cmatch 'DOGRULANAMADI') { $ardisik++ } else { break }
    }
    [Console]::Error.WriteLine("UYARI: Derleme DOGRULANAMADI - $Sebep ($Etiket). Bu bir KOD HATASI DEGIL, ortam sorunu. Yazdigin kodu 'bozuk' varsayip duzeltmeye CALISMA. Ardisik dogrulanamayan tur: $ardisik.")
    if ($ardisik -ge 5) {
        [Console]::Error.WriteLine("DIKKAT: $ardisik turdur hicbir derleme dogrulanmadi. Derleme kapisi fiilen kapali. Kopyada dogrula: powershell -NoProfile -File scripts/kopya.ps1")
    }
}

try {
    $lib = Join-Path $PSScriptRoot 'kit-lib.ps1'
    if (-not (Test-Path -LiteralPath $lib)) { throw "kit-lib.ps1 bulunamadi: $lib" }
    . $lib

    $cwd = (Get-Location).Path
    if ($cwd -match '\\\.claude\\worktrees\\') { exit 0 }         # v1: agent worktree'sinde batchmode ACMA

    $wtListe = Get-HookWorktreeListesi $cwd                       # git 1
    $kok = Find-HookWorktree $cwd $wtListe
    if (-not $kok) { $kok = $cwd }
    $script:LogKok = $kok
    if ($wtListe.Count -gt 0 -and ($kok.ToLowerInvariant() -ne $wtListe[0].ToLowerInvariant())) { exit 0 }   # bagli worktree

    $cfg = Get-KitConfig -Root $kok
    $logKlasor = Join-Path $kok $cfg.LogKlasoru
    New-Item -ItemType Directory -Force -Path $logKlasor | Out-Null
    $log = Join-Path $logKlasor 'hook-post-agent.log'

    # Editor acik / bayat kilit: batchmode ACMA
    if (Test-Path -LiteralPath (Join-Path $kok 'Temp\UnityLockfile')) {
        if (Test-HookUnitySureci $kok) {
            Write-HookDogrulanamadi $log 'Editor acik' 'Unity Editor projeyi acik tutuyor (Temp\UnityLockfile + Unity.exe sureci); batchmode acilmadi'
        } else {
            Write-HookDogrulanamadi $log 'bayat kilit' "Temp\UnityLockfile var ama bu projeyi acan Unity.exe sureci yok; batchmode acilmadi. Kilidi silmeden HEMEN once surec listesine tekrar bak (Unity o an aciliyor olabilir); surec yoksa Temp\UnityLockfile'i sil"
        }
        exit 0
    }

    $dogrulamaAdi = [string]$cfg.DogrulamaScripti
    $dogrulama = $null
    if ($dogrulamaAdi) { $dogrulama = Join-Path $kok ($dogrulamaAdi -replace '/', '\') }
    if (-not $dogrulama -or -not (Test-Path -LiteralPath $dogrulama)) {
        try { Write-KitLog -Ad 'hook-sorunlari' -Mesaj "${HookAdi}: DogrulamaScripti bulunamadi: '$dogrulamaAdi'" -Root $kok } catch { }
        Write-HookDogrulanamadi $log 'script yok' "dogrulama script'i bulunamadi ('$dogrulamaAdi', agent-kit.config.psd1 DogrulamaScripti)"
        exit 0
    }

    # PowerShell 7 varsa onu, yoksa Windows PowerShell 5.1'i kullan (v1 ile ayni).
    $psExe = (Get-Command pwsh -ErrorAction SilentlyContinue).Source
    if (-not $psExe) { $psExe = (Get-Command powershell -ErrorAction SilentlyContinue).Source }

    # Hizli mod: yalniz derleme. Dogrulama script'i depo kokunden calisir (log'lari oraya yazar).
    Push-Location -LiteralPath $kok
    try {
        & $psExe -NoProfile -ExecutionPolicy Bypass -File $dogrulama -CompileOnly
        $code = $LASTEXITCODE
    } finally { Pop-Location }

    if ($code -eq 0) {
        Add-Content -Path $log -Value "$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss') derleme OK"
        exit 0
    }

    if ($code -ge 10 -and $code -le 19) {
        $sebep = switch ($code) {
            10 { 'calisir Unity kurulumu bulunamadi' }
            11 { 'Unity projesi bulunamadi (Assets/ yok)' }
            12 { 'Unity Editor projeyi acik tutuyor (batchmode ayni projeyi acamaz)' }
            default { "ortam sorunu (dogrulama script'inin aciklamasina bak)" }
        }
        Write-HookDogrulanamadi $log "exit $code" $sebep
        exit 0        # turu bloklama: agent'in kodu hakkinda hicbir sey bilmiyoruz
    }

    # Buraya dusen her sey GERCEK bir kod/test hatasidir.
    Add-Content -Path $log -Value "$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss') DERLEME BASARISIZ (exit $code)"
    [Console]::Error.WriteLine("DERLEME BASARISIZ (exit $code). Yazdigin kod derlenmiyor. $($cfg.LogKlasoru)/ altindaki son dogrulama log'unu (orn. unity-verify-last.log) oku ve DURUMU DUZELT. Task'i tamamlanmis sayma.")
    exit 2
} catch {
    $hata = $_.Exception.Message
    try { Write-KitLog -Ad 'hook-sorunlari' -Mesaj "${HookAdi}: ic hata, fail-open: $hata" -Root $script:LogKok } catch {
        try {
            $d = Join-Path (Get-Location).Path 'pipeline-logs'
            New-Item -ItemType Directory -Force -Path $d | Out-Null
            Add-Content -Path (Join-Path $d 'hook-sorunlari.log') -Value "$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss') ${HookAdi}: ic hata, fail-open: $hata"
        } catch { }
    }
    exit 0
}

exit 0
