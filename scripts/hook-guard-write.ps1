# hook-guard-write.ps1 - agent-kit v2
# PreToolUse hook (matcher: Write|Edit): hedef dosya yolunu denetler.
# Cikis 2 = engel, 0 = serbest. Ic hata = FAIL-OPEN (logla, cikis 0).
#
# Kurallar agent-kit.config.psd1'den okunur (yoksa kit-lib varsayilanlari):
#   KorunanUzantilar    -> yalniz scene-integrator        (gecis: AGENT_PIPELINE_ALLOW_SCENE=1)
#   KompozisyonKokleri  -> yalniz supervizor              (gecis: AGENT_PIPELINE_ALLOW_COMPOSITION=1)
#   KorunanBelgeler     -> emekli/korunan belge klasoru   (gecis: AGENT_PIPELINE_ALLOW_GDD=1 ya da _DOCS=1)
#   GuvenlikDosyalari   -> CLAUDE.md, settings.json, scripts/hook-, config  (gecis YOK)
# Her env gecisi YALNIZ kendi kuralini gevsetir.
#
# Denetim yalniz yol bu deponun bir worktree'si altindaysa yapilir ('git worktree list'); scratchpad,
# %TEMP%, baska depo serbesttir. Yollar buyuk/kucuk harf duyarsiz, / ve \ normalize karsilastirilir.
# Desenler worktree kokune gore goreli yol onekleridir; herhangi bir klasor sinirinda da eslesir
# (v1'deki gibi alt klasordeki CLAUDE.md de korunur).
# Hiz: tek git cagrisi (worktree list), ag erisimi yok.

$ErrorActionPreference = 'Continue'

$raw = @($input) -join "`n"
if ([string]::IsNullOrWhiteSpace($raw)) { try { $raw = [Console]::In.ReadToEnd() } catch { $raw = '' } }

$HookAdi = 'guard-write'
$script:LogKok = (Get-Location).Path

function Stop-HookArac([string]$Mesaj) {
    try { Write-KitLog -Ad 'hook-engellenen' -Mesaj "${HookAdi}: $Mesaj" -Root $script:LogKok } catch { }
    Block-Tool -Mesaj $Mesaj
}

function ConvertTo-HookTamYol([string]$Yol, [string]$Taban) {
    $y = $Yol.Trim()
    if ($y -match '^/([A-Za-z])(/.*)?$') {
        $kuyruk = '\'; if ($Matches[2]) { $kuyruk = $Matches[2] }
        $y = $Matches[1] + ':' + $kuyruk
    }
    $y = $y -replace '/', '\'
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

function Test-HookYolEslesir([string]$Rel, [string]$Desen) {
    # Rel ve desen '/' ayracli, kucuk harf. Desen ya rel'in basinda ya bir klasor sinirindan sonra.
    $d = ([string]$Desen -replace '\\', '/').Trim().TrimStart('/').ToLowerInvariant()
    if (-not $d) { return $false }
    $r = $Rel.ToLowerInvariant()
    return ($r.StartsWith($d) -or $r.Contains('/' + $d))
}

try {
    $lib = Join-Path $PSScriptRoot 'kit-lib.ps1'
    if (-not (Test-Path -LiteralPath $lib)) { throw "kit-lib.ps1 bulunamadi: $lib" }
    . $lib

    $h = ConvertFrom-HookInput $raw $HookAdi
    if (-not $h) { exit 0 }
    $yol = [string]$h.tool_input.file_path
    if ([string]::IsNullOrWhiteSpace($yol)) { $yol = [string]$h.tool_input.notebook_path }
    if ([string]::IsNullOrWhiteSpace($yol)) { exit 0 }

    $cwd = (Get-Location).Path
    $taban = $cwd
    if ($h.cwd) { $taban = [string]$h.cwd }
    $tam = ConvertTo-HookTamYol $yol $taban

    $wtListe = Get-HookWorktreeListesi $cwd
    if ($wtListe.Count -eq 0) { exit 0 }                 # oturum bir git deposunda degil: denetlenecek worktree yok
    $oturumKoku = Find-HookWorktree $cwd $wtListe
    if ($oturumKoku) { $script:LogKok = $oturumKoku }
    $wt = Find-HookWorktree $tam $wtListe
    if (-not $wt) { exit 0 }                              # scratchpad, %TEMP%, baska depo: serbest

    $cfg = Get-KitConfig -Root $script:LogKok
    $rel = ($tam.Substring($wt.Length).TrimStart('\')) -replace '\\', '/'
    $ext = [System.IO.Path]::GetExtension($tam).ToLowerInvariant()

    # 1. Sahne / prefab / asset uzantilari: yalniz scene-integrator
    if ($env:AGENT_PIPELINE_ALLOW_SCENE -ne '1') {
        foreach ($u in @($cfg.KorunanUzantilar)) {
            if ($u -and ($ext -ceq ([string]$u).ToLowerInvariant())) {
                Stop-HookArac "'$ext' uzantili dosyalara sadece scene-integrator dokunabilir ($rel). Bu degisikligi task dosyasina 'NEEDS_SCENE: <ne gerekiyor>' olarak yaz ve status'u blocked yap."
            }
        }
    }

    # 2. Kompozisyon kokleri: yalniz supervizor
    if ($env:AGENT_PIPELINE_ALLOW_COMPOSITION -ne '1') {
        foreach ($k in @($cfg.KompozisyonKokleri)) {
            if ($k -and (Test-HookYolEslesir $rel ([string]$k))) {
                Stop-HookArac "$rel kompozisyon kokudur, alt agent yazamaz. Kendi sistemine bir seam ac (Func<>/arayuz/Initialize parametresi) ve task dosyasina 'NEEDS_WIRING: <sistem> -> <seam adi>' yaz. Baglamayi supervisor yapar."
            }
        }
    }

    # 3. Korunan / emekli belgeler
    if ($env:AGENT_PIPELINE_ALLOW_GDD -ne '1' -and $env:AGENT_PIPELINE_ALLOW_DOCS -ne '1') {
        foreach ($b in @($cfg.KorunanBelgeler)) {
            if ($b -and (Test-HookYolEslesir $rel ([string]$b))) {
                Stop-HookArac "$rel korunan belge klasorunde ($b): yalniz supervisor talimatiyla degisir. Sorunu docs/decisions/ altina OPEN- dosyasi olarak yaz."
            }
        }
    }

    # 4. Pipeline guvenlik dosyalari: gecis yok
    foreach ($g in @($cfg.GuvenlikDosyalari)) {
        if ($g -and (Test-HookYolEslesir $rel ([string]$g))) {
            Stop-HookArac "Pipeline guvenlik dosyalari agent tarafindan degistirilemez ($rel). Gereken degisikligi task dosyasina 'BLOCKED: $rel yazma yetkim yok. Gereken degisiklik: ...' olarak yaz; supervizor yapar."
        }
    }
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
