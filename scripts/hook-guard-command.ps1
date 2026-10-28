# hook-guard-command.ps1 - agent-kit v2
# PreToolUse hook (matcher: Bash|PowerShell): komutu calistirmadan once denetler.
# Cikis 2 = engel (stderr mesaji agent'a doner), 0 = serbest. Ic hata = FAIL-OPEN (logla, cikis 0).
# Prompt unutulur, hook unutulmaz: buradaki her kural en az bir kez gercek kaza olarak yasandi.
#
# v1 kurallari (aynen korunur): git push, remote add/set-url, main'e gecis/commit, kok silme,
#   clean -fdx, reset --hard, Set-ExecutionPolicy, internetten indirme, sistem paketi.
# v2 ekleri:
#   - git add -A | --all | .         paylasilan checkout'ta baskasinin stage'ini/degisikligini alir (3 kez)
#   - git commit -a | --all | -am    ayni sebep; commit mesaji (tirnak/here-string/heredoc ici) denetlenmez
#   - Bash + heredoc (<<) + satir > HeredocSatirSiniri   "unexpected EOF" ile HIC calismaz (2+ kez)
#   - robocopy hedefi bir git worktree'si ve /L yok   /XO + /XF *.cs sart (kopyadan geri alma .cs ezdi)
# Hiz: git cagrisi en fazla 2 (kok + worktree list, ikisi de yalniz gerektiginde), ag erisimi yok.
# Windows PowerShell 5.1 uyumlu. Makine tr-TR: buyuk/kucuk harf duyarsiz regex'ler CultureInvariant.

$ErrorActionPreference = 'Continue'

# stdin script kapsaminda okunur ($input fonksiyona gecmez).
$raw = @($input) -join "`n"
if ([string]::IsNullOrWhiteSpace($raw)) { try { $raw = [Console]::In.ReadToEnd() } catch { $raw = '' } }

$HookAdi = 'guard-command'
$script:HookKok = $null
$script:HookWorktreeler = $null
$RxSecenek = [System.Text.RegularExpressions.RegexOptions]'IgnoreCase, CultureInvariant'
$TEK = [char]39; $CIFT = [char]34; $LF = [char]10; $CR = [char]13; $TAB = [char]9

$v1Kurallar = @(
    @{ p = 'git\s+push';                     m = 'git push yasak. Uzak depoya hicbir sey gonderilmez.' },
    @{ p = 'git\s+remote\s+(add|set-url)';   m = 'Uzak depo ayari degistirilemez.' },
    @{ p = 'git\s+checkout\s+main|git\s+switch\s+main'; m = 'main branchine gecilemez. Kendi agent/T-XXX branchinde kal.' },
    @{ p = 'git\s+commit[^|]*\bmain\b';      m = 'main branchine commit yasak.' },
    @{ p = 'Remove-Item[^|]*-Recurse[^|]*(C:\\|\\\\)'; m = 'Kok/surucu seviyesinde silme yasak.' },
    @{ p = 'rm\s+-rf\s+/';                   m = 'Kok seviyesinde silme yasak.' },
    @{ p = 'git\s+clean\s+-fdx';             m = 'git clean -fdx sadece supervisor scriptinin isidir.' },
    @{ p = 'git\s+reset\s+--hard';           m = 'git reset --hard sadece supervisor scriptinin isidir.' },
    @{ p = 'Set-ExecutionPolicy';            m = 'Sistem ayari degistirilemez.' },
    @{ p = 'Invoke-WebRequest|curl\s+http|wget\s+http'; m = 'Internetten dosya indirme yasak. Gerekli paketi task dosyasina yaz.' },
    @{ p = 'Uninstall-|choco\s+install|winget\s+install'; m = 'Sistem paketi kurulamaz/kaldirilamaz.' }
)

$AddMesaji = "git add -A / --all / . bu checkout'taki TUM degisiklikleri (paralel oturumlarinkini de) stage'ler. Yollari acikca yaz: git add -- <yol1> <yol2>. Commit'i de yollarla sinirla: git commit -F <mesaj.txt> -- <yollar>."
$CommitMesaji = "git commit -a / --all / -am izlenen TUM degisiklikleri (paralel oturumlarinkini de) commit'ler. Yollari acikca ver: git add -- <yol>; git commit -F <mesaj.txt> -- <yollar> (ya da -m '...' -- <yollar>). Commit'ten hemen once git diff --cached --name-only ile index'te yabanci dosya var mi bak."

function Get-HookKok {
    if (-not $script:HookKok) { $script:HookKok = Get-KitRoot -Baslangic (Get-Location).Path }
    return $script:HookKok
}

function Stop-HookArac([string]$Mesaj) {
    try { Write-KitLog -Ad 'hook-engellenen' -Mesaj "${HookAdi}: $Mesaj" -Root (Get-HookKok) } catch { }
    Block-Tool -Mesaj $Mesaj
}

function ConvertTo-HookTamYol([string]$Yol, [string]$Taban) {
    $y = $Yol.Trim()
    # Git Bash yolu: /d/Pundun/x -> D:\Pundun\x
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
    # En uzun eslesen worktree (.claude\worktrees\x ana klasorun icinde durur).
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

# --- Komut ayristirici --------------------------------------------------------
# Amac kusursuz kabuk ayristirmasi degil: tirnak icini (commit mesaji) tek token yapmak ve komutu
# ; && || | & ve satir sonundan parcalara bolmek. Token = @{ V = deger; Q = tirnakli mi }.

function Remove-HookHereString([string]$Metin) {
    # PowerShell here-string govdesi denetlenmez (commit mesaji): @' ... '@  /  @" ... "@
    $m = [regex]::Replace($Metin, "@'[ \t]*\r?\n[\s\S]*?\r?\n'@", "'H'")
    $m = [regex]::Replace($m, '@"[ \t]*\r?\n[\s\S]*?\r?\n"@', '"H"')
    return $m
}

function Remove-HookBashHeredoc([string]$Metin) {
    # Bash heredoc govdesi (cat <<'EOF' ... EOF) komut degil veridir; satirlari atilir.
    $satirlar = $Metin -split "`n"
    $sb = New-Object System.Text.StringBuilder
    $i = 0
    $desen = '(?<!<)<<(?!<)-?\s*([''"]?)([A-Za-z_][A-Za-z0-9_]*)\1'
    while ($i -lt $satirlar.Count) {
        $s = $satirlar[$i]
        $eslesmeler = [regex]::Matches($s, $desen)
        [void]$sb.Append([regex]::Replace($s, $desen, ' ')).Append("`n")
        $i++
        foreach ($m in $eslesmeler) {
            $son = $m.Groups[2].Value
            while ($i -lt $satirlar.Count -and ($satirlar[$i].Trim() -cne $son)) { $i++ }
            $i++
        }
    }
    return $sb.ToString()
}

function Complete-HookToken($Seg, $Sb, $St) {
    if ($St.Var) {
        $Seg.Add(@{ V = $Sb.ToString(); Q = $St.Q })
        [void]$Sb.Clear()
        $St.Var = $false
        $St.Q = $false
    }
}

function Split-HookKomut([string]$Metin, [bool]$Bash) {
    $parcalar = New-Object 'System.Collections.Generic.List[object]'
    $seg = New-Object 'System.Collections.Generic.List[object]'
    $sb = New-Object System.Text.StringBuilder
    $st = @{ Var = $false; Q = $false }
    $kacis = [char]'`'
    if ($Bash) { $kacis = [char]'\' }
    $n = $Metin.Length
    $i = 0
    while ($i -lt $n) {
        $c = $Metin[$i]
        if ($c -eq $TEK) {
            $st.Var = $true; $st.Q = $true; $i++
            while ($i -lt $n) {
                $d = $Metin[$i]
                if ($d -eq $TEK) {
                    if ((-not $Bash) -and ($i + 1 -lt $n) -and ($Metin[$i + 1] -eq $TEK)) { [void]$sb.Append($TEK); $i += 2; continue }
                    break
                }
                [void]$sb.Append($d); $i++
            }
            $i++
            continue
        }
        if ($c -eq $CIFT) {
            $st.Var = $true; $st.Q = $true; $i++
            while ($i -lt $n) {
                $d = $Metin[$i]
                if (($d -eq $kacis) -and ($i + 1 -lt $n)) { [void]$sb.Append($Metin[$i + 1]); $i += 2; continue }
                if ($d -eq $CIFT) {
                    if ((-not $Bash) -and ($i + 1 -lt $n) -and ($Metin[$i + 1] -eq $CIFT)) { [void]$sb.Append($CIFT); $i += 2; continue }
                    break
                }
                [void]$sb.Append($d); $i++
            }
            $i++
            continue
        }
        if (($c -eq $kacis) -and ($i + 1 -lt $n)) {
            $e = $Metin[$i + 1]
            if ($e -eq $LF -or $e -eq $CR) { Complete-HookToken $seg $sb $st; $i += 2; continue }   # satir devami
            [void]$sb.Append($e); $st.Var = $true; $i += 2
            continue
        }
        if ($c -eq [char]' ' -or $c -eq $TAB -or $c -eq $CR) { Complete-HookToken $seg $sb $st; $i++; continue }
        if ($c -eq $LF -or $c -eq [char]';' -or $c -eq [char]'|' -or $c -eq [char]'&') {
            Complete-HookToken $seg $sb $st
            if ($seg.Count -gt 0) { $parcalar.Add($seg); $seg = New-Object 'System.Collections.Generic.List[object]' }
            $i++
            continue
        }
        if ($c -eq [char]'#' -and -not $st.Var) {
            while ($i -lt $n -and $Metin[$i] -ne $LF) { $i++ }
            continue
        }
        [void]$sb.Append($c); $st.Var = $true; $i++
    }
    Complete-HookToken $seg $sb $st
    if ($seg.Count -gt 0) { $parcalar.Add($seg) }
    return ,$parcalar
}

function Get-HookKomutAdi($T) {
    $v = [string]$T.V
    if (-not $T.Q) { $v = $v.TrimStart([char]'(', [char]'{', [char]'@').TrimEnd([char]')', [char]'}') }
    return $v
}

function Test-HookGitAdi([string]$Ad) {
    return [regex]::IsMatch($Ad, '^(?:.*[\\/])?git(?:\.exe)?$|^\$\{?git\w*\}?$', $RxSecenek)
}

function Test-HookRobocopyAdi([string]$Ad) {
    return [regex]::IsMatch($Ad, '^(?:.*[\\/])?robocopy(?:\.exe)?$', $RxSecenek)
}

# --- git add / git commit -----------------------------------------------------

function Test-HookGitAdd($Seg, [int]$Bas, [int]$Son) {
    $yollar = $false
    for ($k = $Bas; $k -lt $Son; $k++) {
        $v = [string]$Seg[$k].V
        if (-not $yollar) {
            if ($v -ceq '--') { $yollar = $true; continue }
            if ($v -ceq '--all') { Stop-HookArac $AddMesaji }
            if ($v -cmatch '^-[A-Za-z]+$' -and $v.Contains('A')) { Stop-HookArac $AddMesaji }
        }
        if ($v -ceq '.') { Stop-HookArac $AddMesaji }
    }
}

function Test-HookGitCommit($Seg, [int]$Bas, [int]$Son) {
    # Deger alan uzun secenekler (ayri token olarak verildiginde sonraki token deger sayilir).
    $degerli = @('--message', '--file', '--reuse-message', '--reedit-message', '--template', '--author',
                 '--date', '--fixup', '--squash', '--cleanup', '--trailer', '--pathspec-from-file')
    $atla = $false
    for ($k = $Bas; $k -lt $Son; $k++) {
        $t = $Seg[$k]
        if ($atla) { $atla = $false; continue }
        if ($t.Q) { continue }                      # tirnak ici: mesaj ya da yol, bayrak degil
        $v = [string]$t.V
        if ($v -ceq '--') { return }                 # sonrasi yollar
        if ($v -ceq '--all') { Stop-HookArac $CommitMesaji }
        if ($v.StartsWith('--')) {
            if ((-not $v.Contains('=')) -and ($degerli -ccontains $v)) { $atla = $true }
            continue
        }
        if ($v -cmatch '^-[A-Za-z]') {
            # Kisa bayrak kumesi (-am, -qa ...): m F C c t deger alir -> kumenin kalani deger, dur.
            $harfler = $v.Substring(1)
            for ($x = 0; $x -lt $harfler.Length; $x++) {
                $ch = $harfler[$x]
                if ($ch -ceq [char]'a') { Stop-HookArac $CommitMesaji }
                if ('mFCct'.IndexOf($ch) -ge 0) { if ($x -eq $harfler.Length - 1) { $atla = $true }; break }
                if ('uS'.IndexOf($ch) -ge 0) { break }
            }
        }
    }
}

function Test-HookGit($Seg, [int]$Bas) {
    $j = $Bas
    while ($j -lt $Seg.Count) {
        $v = [string]$Seg[$j].V
        if ($v -ceq '-C' -or $v -ceq '-c' -or $v -ceq '--git-dir' -or $v -ceq '--work-tree' -or $v -ceq '--namespace') { $j += 2; continue }
        if ($v.StartsWith('-')) { $j++; continue }
        break
    }
    if ($j -ge $Seg.Count) { return }
    $alt = [string]$Seg[$j].V
    $son = $Seg.Count
    for ($k = $j + 1; $k -lt $Seg.Count; $k++) {
        if (Test-HookGitAdi (Get-HookKomutAdi $Seg[$k])) { $son = $k; break }
    }
    if ($alt -ceq 'add') { Test-HookGitAdd $Seg ($j + 1) $son }
    elseif ($alt -ceq 'commit') { Test-HookGitCommit $Seg ($j + 1) $son }
}

# --- robocopy -------------------------------------------------------------------

function Test-HookRobocopy($Seg, [int]$Bas, $H) {
    $konum = New-Object 'System.Collections.Generic.List[string]'
    $L = $false; $XO = $false; $xfCs = $false; $xfIcinde = $false; $yonAtla = $false
    for ($k = $Bas; $k -lt $Seg.Count; $k++) {
        $t = $Seg[$k]
        $v = [string]$t.V
        if ($yonAtla) { $yonAtla = $false; continue }
        if ((-not $t.Q) -and $v -match '^(\d|\*)?(>>?|<)') {       # yonlendirme: > log.txt, 2>$null
            if ($v -match '^(\d|\*)?(>>?|<)$') { $yonAtla = $true }
            continue
        }
        if ((-not $t.Q) -and $v -cmatch '^/{1,2}[A-Za-z][A-Za-z0-9]*(:.*)?$') {   # anahtar: /XO, //XO (Git Bash), /LEV:2
            $ad = ($v.TrimStart('/') -split ':', 2)[0].ToUpperInvariant()
            $xfIcinde = $false
            if ($ad -ceq 'L') { $L = $true }
            elseif ($ad -ceq 'XO') { $XO = $true }
            elseif ($ad -ceq 'XF') { $xfIcinde = $true }
            continue
        }
        if ($xfIcinde) { if ($v -eq '*.cs') { $xfCs = $true }; continue }
        $konum.Add($v)
    }
    if ($L) { return }                     # yalniz listeleme
    if ($konum.Count -lt 2) { return }
    if ($XO -and $xfCs) { return }
    $hedef = $konum[1]
    if ($hedef -match '[\$%]') {
        try { Write-KitLog -Ad 'hook-sorunlari' -Mesaj "${HookAdi}: robocopy hedefi degisken, denetlenemedi: $hedef" -Root (Get-HookKok) } catch { }
        return
    }
    $taban = (Get-Location).Path
    if ($H.cwd) { $taban = [string]$H.cwd }
    $tam = $null
    try { $tam = ConvertTo-HookTamYol $hedef $taban } catch {
        try { Write-KitLog -Ad 'hook-sorunlari' -Mesaj "${HookAdi}: robocopy hedefi cozulemedi: $hedef" -Root (Get-HookKok) } catch { }
        return
    }
    if ($null -eq $script:HookWorktreeler) { $script:HookWorktreeler = Get-HookWorktreeListesi (Get-HookKok) }
    $wt = Find-HookWorktree $tam $script:HookWorktreeler
    if (-not $wt) { return }               # kopyaya dogru senkron: serbest
    $eksik = @()
    if (-not $XO) { $eksik += '/XO' }
    if (-not $xfCs) { $eksik += '/XF *.cs' }
    Stop-HookArac ("robocopy hedefi bir git worktree'si ($wt) ve komutta /L yok; eksik: " + ($eksik -join ' + ') +
        ". Kopyadan geri alma, sync'ten sonra degisen .cs'leri ESKI haliyle ezer (23.10 kaybi). Once listele: robocopy <kaynak> <hedef> ... /L" +
        " (listede .cs varsa DUR); sonra: robocopy <kaynak> <hedef> ... /XO /XF *.cs - ya da yalniz builder ciktisini tek tek kopyala.")
}

# --- Ana akis -------------------------------------------------------------------

try {
    $lib = Join-Path $PSScriptRoot 'kit-lib.ps1'
    if (-not (Test-Path -LiteralPath $lib)) { throw "kit-lib.ps1 bulunamadi: $lib" }
    . $lib

    $h = ConvertFrom-HookInput $raw $HookAdi
    if (-not $h) { exit 0 }
    $cmd = [string]$h.tool_input.command
    if ([string]::IsNullOrWhiteSpace($cmd)) { exit 0 }
    $bash = ([string]$h.tool_name -eq 'Bash')

    # 1. v1 kurallari (ham metin uzerinde, aynen)
    foreach ($b in $v1Kurallar) {
        if ([regex]::IsMatch($cmd, $b.p, $RxSecenek)) { Stop-HookArac "$($b.m)  [kural: $($b.p)]" }
    }

    # 2. Bash + heredoc + uzun komut
    if ($bash -and $cmd.Contains('<<')) {
        $satir = ($cmd -split "`n").Count
        $cfg = Get-KitConfig -Root (Get-HookKok)
        $sinir = [int]$cfg.HeredocSatirSiniri
        if ($sinir -gt 0 -and $satir -gt $sinir) {
            Stop-HookArac "Bash komutu $satir satir ve heredoc (<<) iceriyor; $sinir satir ustu Bash araci 'unexpected EOF' ile HIC calismadan duser. Dosyayi Write araciyla yaz; Bash'i $sinir satirdan kisa parcalara bol."
        }
    }

    # 3. git add / git commit / robocopy (once ucuz on-filtre)
    $gitAday = [regex]::IsMatch($cmd, 'git', $RxSecenek) -and [regex]::IsMatch($cmd, '\b(add|commit)\b', $RxSecenek)
    $roboAday = [regex]::IsMatch($cmd, 'robocopy', $RxSecenek)
    if ($gitAday -or $roboAday) {
        if ($bash) { $metin = Remove-HookBashHeredoc $cmd } else { $metin = Remove-HookHereString $cmd }
        $parcalar = Split-HookKomut $metin $bash
        foreach ($seg in $parcalar) {
            for ($i = 0; $i -lt $seg.Count; $i++) {
                $ad = Get-HookKomutAdi $seg[$i]
                if ($gitAday -and (Test-HookGitAdi $ad)) { Test-HookGit $seg ($i + 1) }
                elseif ($roboAday -and (Test-HookRobocopyAdi $ad)) { Test-HookRobocopy $seg ($i + 1) $h }
            }
        }
    }
} catch {
    $hata = $_.Exception.Message
    try { Write-KitLog -Ad 'hook-sorunlari' -Mesaj "${HookAdi}: ic hata, fail-open: $hata" -Root (Get-Location).Path } catch {
        try {
            $d = Join-Path (Get-Location).Path 'pipeline-logs'
            New-Item -ItemType Directory -Force -Path $d | Out-Null
            Add-Content -Path (Join-Path $d 'hook-sorunlari.log') -Value "$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss') ${HookAdi}: ic hata, fail-open: $hata"
        } catch { }
    }
    exit 0
}

exit 0
