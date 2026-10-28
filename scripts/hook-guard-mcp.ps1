# hook-guard-mcp.ps1 - agent-kit v2
# PreToolUse hook (matcher: mcp__UnityMCP__run_tests | mcp__unityMCP__run_tests |
#                           mcp__UnityMCP__refresh_unity | mcp__unityMCP__refresh_unity).
# Cikis 2 = engel, 0 = serbest. Ic hata = FAIL-OPEN (logla, cikis 0).
#
# Neden:
#   - EditMode test kosusu bitince Unity "Undo after editor test run" yapip kullanicinin commit'siz
#     (hatta commit'li ama oturumda duzenlenmis) prefab'ini diskte geri aldi: 4+ kez.
#   - refresh_unity diskten yeniden yukleyip / domain reload yapip kaydedilmemis prefab isini sildi.
#   - Ana Editor'de run_tests kullanicinin dev save'ini (Editor PlayerPrefs) iki kez sildi.
# Kural:
#   - run_tests: once prefs-yedek.ps1 -Al -Sessiz (engellense de engellenmese de).
#   - Ana worktree'de 'git status --porcelain -- Assets' icinde commit'siz (M/A/??) .prefab/.unity/.asset
#     varsa ve bilincli gecis izni yoksa (kit-izin.ps1 unity-test / unity-refresh) engelle.
# Hiz: temiz yolda 2 git cagrisi (worktree list + status); izin bakilan riskli yolda +1 (Test-KitIzin).

$ErrorActionPreference = 'Continue'

$raw = @($input) -join "`n"
if ([string]::IsNullOrWhiteSpace($raw)) { try { $raw = [Console]::In.ReadToEnd() } catch { $raw = '' } }

$HookAdi = 'guard-mcp'
$script:LogKok = (Get-Location).Path
$GosterilecekDosya = 10

function Stop-HookArac([string]$Mesaj) {
    try { Write-KitLog -Ad 'hook-engellenen' -Mesaj "${HookAdi}: $Mesaj" -Root $script:LogKok } catch { }
    Block-Tool -Mesaj $Mesaj
}

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

try {
    $lib = Join-Path $PSScriptRoot 'kit-lib.ps1'
    if (-not (Test-Path -LiteralPath $lib)) { throw "kit-lib.ps1 bulunamadi: $lib" }
    . $lib

    $h = ConvertFrom-HookInput $raw $HookAdi
    if (-not $h) { exit 0 }
    $arac = [string]$h.tool_name
    if ($arac -cmatch '__run_tests$') { $testKosusu = $true; $izinAdi = 'unity-test' }
    elseif ($arac -cmatch '__refresh_unity$') { $testKosusu = $false; $izinAdi = 'unity-refresh' }
    else { exit 0 }

    $cwd = (Get-Location).Path
    $wtListe = Get-HookWorktreeListesi $cwd                       # git 1
    $ana = $cwd
    if ($wtListe.Count -gt 0) { $ana = $wtListe[0] }              # Unity Editor'un actigi klasor
    $oturumKoku = Find-HookWorktree $cwd $wtListe
    if ($oturumKoku) { $script:LogKok = $oturumKoku }

    # run_tests: once PlayerPrefs yedegi (kosu dev save'i silebilir). Yedek hatasi testi engellemez.
    if ($testKosusu) {
        $prefs = Join-Path $PSScriptRoot 'prefs-yedek.ps1'
        if (Test-Path -LiteralPath $prefs) {
            try { & $prefs -Al -Sessiz -Root $ana | Out-Null } catch {
                try { Write-KitLog -Ad 'hook-sorunlari' -Mesaj "${HookAdi}: prefs-yedek hatasi: $($_.Exception.Message)" -Root $script:LogKok } catch { }
            }
        } else {
            try { Write-KitLog -Ad 'hook-sorunlari' -Mesaj "${HookAdi}: prefs-yedek.ps1 yok ($prefs), yedek alinmadi." -Root $script:LogKok } catch { }
        }
    }

    # Ana worktree'de commit'siz sahne/prefab/asset
    $git = Get-GitExe
    $satirlar = @(& $git -C $ana -c core.quotePath=false status --porcelain --untracked-files=all -- Assets 2>$null)   # git 2
    $riskli = New-Object 'System.Collections.Generic.List[string]'
    foreach ($l in $satirlar) {
        $s = [string]$l
        if ($s.Length -lt 4) { continue }
        $xy = $s.Substring(0, 2)
        if (-not ($xy -eq '??' -or $xy -cmatch '[MA]')) { continue }
        $p = $s.Substring(3)
        $ok = $p.IndexOf(' -> ')
        if ($ok -ge 0) { $p = $p.Substring($ok + 4) }
        $p = $p.Trim().Trim([char]34)
        $pl = $p.ToLowerInvariant()
        if ($pl.EndsWith('.prefab') -or $pl.EndsWith('.unity') -or $pl.EndsWith('.asset')) { $riskli.Add($p) }
    }
    if ($riskli.Count -eq 0) { exit 0 }

    if (Test-KitIzin -Ad $izinAdi -Root $ana) {                    # git 3 (yalniz riskli yolda)
        try { Write-KitLog -Ad 'hook-izin' -Mesaj "${HookAdi}: $arac izinle gecti ($izinAdi), commit'siz $($riskli.Count) dosya." -Root $script:LogKok } catch { }
        exit 0
    }

    $cfg = Get-KitConfig -Root $ana
    $liste = (@($riskli | Select-Object -First $GosterilecekDosya) -join ', ')
    if ($riskli.Count -gt $GosterilecekDosya) { $liste += " ve $($riskli.Count - $GosterilecekDosya) dosya daha" }
    $basi = "Ana calisma agacinda ($ana) commit'siz $($riskli.Count) sahne/prefab/asset var: $liste."
    if ($testKosusu) {
        Stop-HookArac ("$basi run_tests kosu bitince Unity 'Undo after editor test run' yapip bunlari diskte sessizce geri alabilir (4+ kez oldu)." +
            " Yapilacak: testi kopyada kos (scripts/kopya.ps1) ya da dosyalarin sahibine commit'let." +
            " Bilerek gecmek icin once KULLANICIYA SOR; onay gelirse: powershell -NoProfile -File scripts/kit-izin.ps1 unity-test ($($cfg.IzinSuresiDakika) dk gecerli).")
    } else {
        Stop-HookArac ("$basi refresh_unity diskten yeniden yukler / domain reload yapar; kullanicinin kaydedilmemis prefab/sahne duzenlemesini sessizce siler." +
            " Yapilacak: once prefab stage ve isDirty kontrol et (PrefabStageUtility.GetCurrentPrefabStage(), acik sahnelerin isDirty'si); kaydedilmemis is varsa kullaniciya kaydettir." +
            " Sonra kullaniciya sorup: powershell -NoProfile -File scripts/kit-izin.ps1 unity-refresh ($($cfg.IzinSuresiDakika) dk gecerli).")
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
