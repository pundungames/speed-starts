<#
tur-sonu.ps1 - tur sonu kapisi: task dosyalari + git + kopyalar tek komutla.

Kullanim:
  scripts\tur-sonu.ps1 [-Root <proje>] [-Since <git ref>] [-DurumYaz] [-Kisa]
    -Root     proje koku (vars. bulunulan klasorun git koku)
    -Since    bu ref'ten beri degisen task dosyalari kapsam; verilmezse status in-progress/review/merged olanlar.
              (Acik WIRING/SCENE kapisi HER ZAMAN tum task'lara bakar, cancelled haric.)
    -DurumYaz <DurumDosyasi> icinde <!-- OTOMATIK:BASLA --> ile <!-- OTOMATIK:BITIR --> arasini kisa raporla degistirir;
              isaretler yoksa dosyaya dokunmaz, uyarir. Isaretler disi AYNEN kalir.
    -Kisa     her bolumde ilk birkac satir.
Cikis: 0 = kapi temiz (format hatasi yok, open WIRING/SCENE yok), 1 = kapi kapali, 2 = script hatasi.
Cikti duz metin, ASCII (PS 5.1 konsolunda kodlama sorunu olmasin).
#>
[CmdletBinding()]
param(
    [string]$Root,
    [string]$Since,
    [switch]$DurumYaz,
    [switch]$Kisa
)

$ErrorActionPreference = 'Stop'
trap {
    [Console]::Error.WriteLine("tur-sonu HATA: $($_.Exception.Message) [satir $($_.InvocationInfo.ScriptLineNumber)]")
    exit 2
}

. (Join-Path $PSScriptRoot 'task-lib.ps1')

function Get-TsKok {
    param([string]$Verilen)
    if ($Verilen) { return (Resolve-Path -LiteralPath $Verilen).Path }
    $eap = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try { $k = Get-KitRoot } finally { $ErrorActionPreference = $eap }
    return $k
}

function Format-Ts {
    # ASCII + tek satir + kirpma
    param([AllowNull()][string]$Metin, [int]$En = 150)
    $a = (ConvertTo-KitAscii $Metin) -replace '\s+', ' '
    $a = $a.Trim()
    if ($a.Length -gt $En) { $a = $a.Substring(0, $En - 3) + '...' }
    return $a
}

function Get-TsGoreli {
    param([string]$Yol)
    if ($Yol.StartsWith($script:Kok, [StringComparison]::OrdinalIgnoreCase)) { return $Yol.Substring($script:Kok.Length).TrimStart('\', '/') }
    return $Yol
}

function New-TsBolum {
    param([string]$No, [string]$Baslik, [int]$KisaSinir = 5)
    return [pscustomobject]@{ No = $No; Baslik = $Baslik; Satirlar = (New-Object System.Collections.Generic.List[string]); KisaSinir = $KisaSinir }
}

# ------------------------------------------------------------------------------------------------------------------
$script:Kok = Get-TsKok $Root
$cfg = Get-KitConfig -Root $script:Kok
$simdi = Get-Date
$tasks = @(Get-Tasks -Config $cfg)
# Not: task-lib'in dizi donduren fonksiyonlari diziyi acar; burada her cagri @(...) ile sarilir.
$fmli = @($tasks | Where-Object { $_.FrontmatterVar })
$durumOf = @{}
foreach ($t in $fmli) { $durumOf[$t.Path] = Get-TaskSkaler $t 'status' }

# --- kapsam ---
$kapsamAciklama = 'status in-progress/review/merged'
if ($Since) {
    # '--' tirnakli: tirnaksiz -- PowerShell'in kendi "parametre sonu" isaretidir ve git'e ulasmaz
    $d = Invoke-TaskGit $script:Kok diff --name-only --relative $Since '--' $cfg.TaskKlasoru
    if ($d.Kod -ne 0) { throw "git diff $Since basarisiz (ref gecerli mi?)" }
    $u = Invoke-TaskGit $script:Kok ls-files --others --exclude-standard '--' $cfg.TaskKlasoru
    $degisen = @{}
    foreach ($y in @($d.Cikti + $u.Cikti)) {
        if (-not $y) { continue }
        $tam = [System.IO.Path]::GetFullPath((Join-Path $script:Kok ($y -replace '/', '\')))
        $degisen[$tam.ToLowerInvariant()] = $true
    }
    $kapsam = @($tasks | Where-Object { $degisen.ContainsKey($_.Path.ToLowerInvariant()) })
    $kapsamAciklama = "$Since'ten beri degisen task dosyalari"
} else {
    $kapsam = @($fmli | Where-Object { $durumOf[$_.Path] -in @('in-progress', 'review', 'merged') })
}

$bolumler = New-Object System.Collections.Generic.List[object]

# [1] format hatalari
$b1 = New-TsBolum '1' 'FORMAT HATALARI (KAPI)' 10
foreach ($t in $tasks) {
    $ad = $t.Id
    if (-not $ad) { $ad = Get-TsGoreli $t.Path }
    if (-not $t.FrontmatterVar) {
        $b1.Satirlar.Add("$(Get-TsGoreli $t.Path): frontmatter yok (task degilse adini T-*.md disina al, task ise frontmatter ekle)")
        continue
    }
    foreach ($h in $t.Hatalar) { $b1.Satirlar.Add("${ad}: $h") }
    $st = $durumOf[$t.Path]
    if (-not $st) { $b1.Satirlar.Add("${ad}: status yok") }
    elseif (-not (Test-TaskStatus $st)) { $b1.Satirlar.Add("${ad}: gecersiz status '$st' (gecerli: $((Get-TaskStatusKumesi) -join ', '))") }
    if ($st -in @('blocked', 'cancelled') -and -not (Get-TaskSkaler $t 'blocked_reason').Trim()) { $b1.Satirlar.Add("${ad}: $st ama blocked_reason bos") }
    foreach ($o in (Get-TaskListe $t 'needs')) {
        if (-not ($o -is [System.Collections.IDictionary])) { $b1.Satirlar.Add("${ad}: needs maddesi harita degil: '$o'"); continue }
        $tip = [string]$o['tip']; $du = [string]$o['durum']
        if ($script:TaskNeedTipleri -notcontains $tip) { $b1.Satirlar.Add("${ad}: needs tip gecersiz '$tip'") }
        if ($script:TaskNeedDurumlari -notcontains $du) { $b1.Satirlar.Add("${ad}: needs durum gecersiz '$du'") }
        if (-not ([string]$o['not']).Trim()) { $b1.Satirlar.Add("${ad}: needs not bos ($tip)") }
    }
}
$formatSayisi = $b1.Satirlar.Count
$bolumler.Add($b1)

# [2] acik needs
$acik = New-Object System.Collections.Generic.List[object]
foreach ($t in $fmli) {
    foreach ($o in (Get-TaskNeeds $t)) {
        if (([string]$o['durum']) -eq 'open') { $acik.Add([pscustomobject]@{ Id = $t.Id; Status = $durumOf[$t.Path]; Tip = [string]$o['tip']; Not = [string]$o['not'] }) }
    }
}
$kapiAcik = @($acik | Where-Object { $script:TaskKapiTipleri -contains $_.Tip -and $_.Status -ne 'cancelled' } | Sort-Object @{ Expression = { Get-TaskIdSayisi $_.Id } }, Tip)
$digerAcik = @($acik | Where-Object { -not ($script:TaskKapiTipleri -contains $_.Tip -and $_.Status -ne 'cancelled') } | Sort-Object @{ Expression = { Get-TaskIdSayisi $_.Id } }, Tip)
$kapaliTaskAcik = @($acik | Where-Object { $_.Status -in @('done', 'cancelled') })
$tipOzet = @($acik | Group-Object Tip | Sort-Object Name | ForEach-Object { "$($_.Name) $($_.Count)" }) -join ', '
if (-not $tipOzet) { $tipOzet = '-' }
$b2 = New-TsBolum '2' "ACIK NEEDS: $($acik.Count) ($tipOzet) | KAPI (WIRING/SCENE, cancelled haric): $($kapiAcik.Count)" 10
foreach ($a in $kapiAcik) { $b2.Satirlar.Add("KAPI  $($a.Id) [$($a.Status)] $($a.Tip): $($a.Not)") }
foreach ($a in $digerAcik) { $b2.Satirlar.Add("      $($a.Id) [$($a.Status)] $($a.Tip): $($a.Not)") }
if ($kapaliTaskAcik.Count -gt 0) {
    $b2.Satirlar.Add("UYARI done/cancelled task'ta open need: $($kapaliTaskAcik.Count) ($((@($kapaliTaskAcik | ForEach-Object { $_.Id } | Select-Object -Unique)) -join ', '))")
}
$bolumler.Add($b2)

# [3] govdede NEEDS_ var, frontmatter needs'te karsiligi yok (in-progress/review)
$b3 = New-TsBolum '3' "GOVDEDE NEEDS_ AMA FRONTMATTER needs'TE YOK (in-progress/review, uyari)" 5
foreach ($t in @($fmli | Where-Object { $durumOf[$_.Path] -in @('in-progress', 'review') })) {
    $fmTipler = @(Get-TaskNeeds $t | ForEach-Object { [string]$_['tip'] })
    $govde = @(Get-TaskGovdeNeeds -Govde $t.Body | Where-Object { -not $_.Kapali })
    foreach ($g in @($govde | Group-Object Tip)) {
        if ($fmTipler -notcontains $g.Name) {
            $ilk = $g.Group[0]
            $b3.Satirlar.Add("$($t.Id) [$($durumOf[$t.Path])] govdede $($g.Count) NEEDS_$($g.Name), needs'te $($g.Name) yok (govde satir $($ilk.Satir): $($ilk.Not))")
        }
    }
}
$bolumler.Add($b3)

# [4] review kuyrugu
$review = @($fmli | Where-Object { $durumOf[$_.Path] -eq 'review' } | Sort-Object @{ Expression = { Get-TaskIdSayisi $_.Id } })
$uyari4 = ''
if ($review.Count -gt 10) { $uyari4 = '  UYARI: 10 ustu, supervizor kuyrugu birikti' }
$b4 = New-TsBolum '4' "REVIEW KUYRUGU (agent bitirdi, supervizor bekliyor): $($review.Count)$uyari4" 5
foreach ($t in $review) { $b4.Satirlar.Add("$($t.Id)  $(Get-TaskSkaler $t 'title')") }
$bolumler.Add($b4)

# [5] goz bekleyenler (merged): en yeni 15
$merged = @($fmli | Where-Object { $durumOf[$_.Path] -eq 'merged' })
$hashZaman = @{}
$ilkHashler = @($merged | ForEach-Object { @(Get-TaskListe $_ 'commits') | Select-Object -First 1 } | Where-Object { $_ })
if ($ilkHashler.Count -gt 0) {
    $lg = Invoke-TaskGit $script:Kok log --no-walk '--format=%H %ct' @ilkHashler
    $satirlarLg = @()
    if ($lg.Kod -eq 0) { $satirlarLg = $lg.Cikti }
    else {
        foreach ($h in $ilkHashler) { $tek = Invoke-TaskGit $script:Kok log -1 '--format=%H %ct' $h; if ($tek.Kod -eq 0) { $satirlarLg += $tek.Cikti } }
    }
    foreach ($s in $satirlarLg) {
        $p = ([string]$s).Trim() -split ' '
        if ($p.Count -ge 2) { foreach ($h in $ilkHashler) { if ($p[0].StartsWith([string]$h, [StringComparison]::OrdinalIgnoreCase)) { $hashZaman[[string]$h] = [long]$p[1] } } }
    }
}
$mergedSirali = @($merged | Sort-Object @{ Expression = { $h = @(Get-TaskListe $_ 'commits') | Select-Object -First 1; if ($h -and $hashZaman.ContainsKey([string]$h)) { $hashZaman[[string]$h] } else { 0 } }; Descending = $true }, @{ Expression = { Get-TaskIdSayisi $_.Id }; Descending = $true })
$b5 = New-TsBolum '5' "GOZ BEKLEYENLER (merged, kullanicinin bakmasi bekleniyor): $($merged.Count) (en yeni 15)" 15
foreach ($t in @($mergedSirali | Select-Object -First 15)) {
    $h = @(Get-TaskListe $t 'commits') | Select-Object -First 1
    if (-not $h) { $h = '(commit yok)' }
    $b5.Satirlar.Add("$($t.Id)  $h  $(Get-TaskSkaler $t 'title')")
}
$bolumler.Add($b5)

# [6] in-progress / blocked
$ip = @($fmli | Where-Object { $durumOf[$_.Path] -eq 'in-progress' } | Sort-Object @{ Expression = { Get-TaskIdSayisi $_.Id } })
$bl = @($fmli | Where-Object { $durumOf[$_.Path] -eq 'blocked' } | Sort-Object @{ Expression = { Get-TaskIdSayisi $_.Id } })
$b6 = New-TsBolum '6' "IN-PROGRESS: $($ip.Count) / BLOCKED: $($bl.Count)" 10
foreach ($t in $ip) { $b6.Satirlar.Add("in-progress  $($t.Id) (started $(Get-TaskSkaler $t 'started'))  $(Get-TaskSkaler $t 'title')") }
foreach ($t in $bl) { $b6.Satirlar.Add("blocked      $($t.Id)  $(Get-TaskSkaler $t 'title') -- neden: $(Get-TaskSkaler $t 'blocked_reason')") }
$bolumler.Add($b6)

# [7] test
$b7 = New-TsBolum '7' "TEST (kapsamdaki review/merged/done: tests bos ya da dosya yok, uyari)" 5
foreach ($t in @($kapsam | Where-Object { $_.FrontmatterVar -and ($durumOf[$_.Path] -in @('review', 'merged', 'done')) })) {
    $ts = @(Get-TaskListe $t 'tests')
    if ($ts.Count -eq 0) { $b7.Satirlar.Add("$($t.Id) [$($durumOf[$t.Path])]: tests bos (dosya ya da ""gerekmiyor: <gerekce>"")"); continue }
    foreach ($y in $ts) {
        $y = [string]$y
        if ([regex]::IsMatch($y, '^\s*gerekmiyor\s*:\s*\S', 'IgnoreCase, CultureInvariant')) { continue }
        $tam = $y
        if (-not [System.IO.Path]::IsPathRooted($y)) { $tam = Join-Path $script:Kok ($y -replace '/', '\') }
        $var = $false
        if ($tam -match '[*?\[]') { $var = [bool](Test-Path -Path $tam) } else { $var = Test-Path -LiteralPath $tam }
        if (-not $var) { $b7.Satirlar.Add("$($t.Id) [$($durumOf[$t.Path])]: test dosyasi yok: $y") }
    }
}
$bolumler.Add($b7)

# [8] analytics
$b8 = New-TsBolum '8' 'ANALYTICS (kapsamdaki task: analytics bos / olay katalogda ya da GA eslemesinde yok, uyari)' 5
if ($cfg.AnalyticsKatalog -and $cfg.AnalyticsGaEslemesi) {
    $katYol = Join-Path $script:Kok $cfg.AnalyticsKatalog
    $gaYol = Join-Path $script:Kok $cfg.AnalyticsGaEslemesi
    $katMetin = $null; $gaMetin = $null
    if (Test-Path -LiteralPath $katYol) { $katMetin = [System.IO.File]::ReadAllText($katYol) } else { $b8.Satirlar.Add("katalog dosyasi yok: $($cfg.AnalyticsKatalog)") }
    if (Test-Path -LiteralPath $gaYol) { $gaMetin = [System.IO.File]::ReadAllText($gaYol) } else { $b8.Satirlar.Add("GA esleme dosyasi yok: $($cfg.AnalyticsGaEslemesi)") }
    foreach ($t in @($kapsam | Where-Object { $_.FrontmatterVar -and ($durumOf[$_.Path] -notin @('todo', 'cancelled')) })) {
        $an = @(Get-TaskListe $t 'analytics')
        if ($an.Count -eq 0) { $b8.Satirlar.Add("$($t.Id) [$($durumOf[$t.Path])]: analytics bos (olay adi ya da ""yok: <gerekce>"")"); continue }
        foreach ($e in $an) {
            $e = ([string]$e).Trim()
            if ([regex]::IsMatch($e, '^yok\s*:\s*\S', 'IgnoreCase, CultureInvariant')) { continue }
            if ($null -ne $katMetin -and -not $katMetin.Contains($e)) { $b8.Satirlar.Add("$($t.Id): '$e' katalogda yok ($($cfg.AnalyticsKatalog))") }
            if ($null -ne $gaMetin -and -not $gaMetin.Contains($e)) { $b8.Satirlar.Add("$($t.Id): '$e' GA eslemesinde yok ($($cfg.AnalyticsGaEslemesi))") }
        }
    }
} else {
    $b8.Baslik = 'ANALYTICS: config AnalyticsKatalog/AnalyticsGaEslemesi bos, denetim yok'
}
$bolumler.Add($b8)

# [9] git
$b9 = New-TsBolum '9' 'GIT (worktree: dal | commit''siz degisiklik | ana dala gore ahead/behind)' 20
$wl = Invoke-TaskGit $script:Kok worktree list --porcelain
$agaclar = New-Object System.Collections.Generic.List[object]
$aktifAgac = $null
foreach ($s in $wl.Cikti) {
    if ($s -like 'worktree *') { $aktifAgac = [pscustomobject]@{ Yol = ($s.Substring(9) -replace '/', '\'); Head = ''; Dal = ''; Notlar = '' }; $agaclar.Add($aktifAgac) }
    elseif ($null -ne $aktifAgac -and $s -like 'HEAD *') { $aktifAgac.Head = $s.Substring(5) }
    elseif ($null -ne $aktifAgac -and $s -like 'branch *') { $aktifAgac.Dal = ($s.Substring(7) -replace '^refs/heads/', '') }
    elseif ($null -ne $aktifAgac -and $s -eq 'detached') { $aktifAgac.Dal = '(detached)' }
    elseif ($null -ne $aktifAgac -and ($s -like 'locked*' -or $s -like 'prunable*')) { $aktifAgac.Notlar = ($aktifAgac.Notlar + ' ' + $s).Trim() }
}
$anaRef = ''
if ($agaclar.Count -gt 0) {
    $anaRef = $agaclar[0].Dal
    if (-not $anaRef -or $anaRef -eq '(detached)') { $anaRef = $agaclar[0].Head }
}
$korunanAna = 0
for ($i = 0; $i -lt $agaclar.Count; $i++) {
    $a = $agaclar[$i]
    $etiket = 'agac'
    if ($i -eq 0) { $etiket = 'ANA ' }
    if (-not (Test-Path -LiteralPath $a.Yol)) { $b9.Satirlar.Add("$etiket $($a.Yol) | $($a.Dal) | klasor YOK $($a.Notlar)"); continue }
    $st = Invoke-TaskGit $a.Yol --no-optional-locks status --porcelain
    $satirlarSt = @($st.Cikti | Where-Object { $_ -and $_.Length -gt 3 })
    $deg = "$($satirlarSt.Count) commit'siz"
    if ($st.Kod -ne 0) { $deg = 'git status basarisiz' }
    $ab = ''
    if ($i -gt 0 -and $anaRef) {
        $hedefRef = $a.Dal
        if (-not $hedefRef -or $hedefRef -eq '(detached)') { $hedefRef = $a.Head }
        $rl = Invoke-TaskGit $script:Kok rev-list --left-right --count "$anaRef...$hedefRef"
        if ($rl.Kod -eq 0 -and $rl.Cikti.Count -gt 0) {
            $p = ($rl.Cikti[0].Trim() -split '\s+')
            if ($p.Count -ge 2) { $ab = " | ana ($anaRef) gore: ahead $($p[1]) / behind $($p[0])" }
        }
    }
    $ek = ''
    if ($i -eq 0) {
        # izlenmeyen klasor "?? Assets/Yeni/" diye tek satira katlanir: korunan uzantilari dosya bazinda ayrica say
        $pf = Invoke-TaskGit $a.Yol --no-optional-locks status --porcelain --untracked-files=all '--' '*.prefab' '*.unity' '*.asset'
        foreach ($s in @($pf.Cikti | Where-Object { $_ -and $_.Length -gt 3 })) {
            $yol = $s.Substring(3)
            if ($yol.Contains(' -> ')) { $yol = $yol.Substring($yol.IndexOf(' -> ') + 4) }
            $yol = $yol.Trim('"')
            if ($yol -match '\.(prefab|unity|asset)$') { $korunanAna++ }
        }
        $ek = " | commit'siz .prefab/.unity/.asset: $korunanAna"
        if ($korunanAna -gt 0) { $ek += ' (UYARI: ana Editor''de test kosusu bunlari geri alabilir; once commit/yedek)' }
    }
    $b9.Satirlar.Add("$etiket $($a.Yol) | $($a.Dal) | $deg$ab$ek $($a.Notlar)".TrimEnd())
}
$b9.Baslik = "GIT: $($agaclar.Count) calisma agaci (ana: $anaRef)"
$bolumler.Add($b9)

# [10] kopyalar
$b10 = New-TsBolum '10' 'KOPYALAR' 10
$kopya = $null
foreach ($aday in @((Join-Path $PSScriptRoot 'kopya.ps1'), (Join-Path $script:Kok 'scripts\kopya.ps1'))) { if (-not $kopya -and (Test-Path -LiteralPath $aday)) { $kopya = $aday } }
if (-not $kopya) {
    $b10.Baslik = 'KOPYALAR: kopya.ps1 kurulu degil'
} else {
    $eap = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    $kArg = @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', $kopya, '-Liste', '-Json')
    try { if ((Get-Command -Name $kopya -ErrorAction Stop).Parameters.ContainsKey('Root')) { $kArg += @('-Root', $script:Kok) } } catch { }
    Push-Location -LiteralPath $script:Kok
    try { $kc = & powershell.exe @kArg 2>$null; $kk = $LASTEXITCODE }
    finally { Pop-Location; $ErrorActionPreference = $eap }
    $json = (@($kc) -join "`n")
    $bas = $json.IndexOf('[')
    if ($bas -lt 0) { $bas = $json.IndexOf('{') }
    $liste = $null
    if ($bas -ge 0) { try { $liste = @($json.Substring($bas) | ConvertFrom-Json) } catch { $liste = $null } }
    if ($null -eq $liste) {
        $b10.Baslik = "KOPYALAR: kopya.ps1 -Liste -Json ciktisi cozulemedi (cikis $kk)"
        foreach ($s in @(@($kc) | Where-Object { $_ } | Select-Object -First 3)) { $b10.Satirlar.Add("kopya.ps1: $s") }
    } else {
        if ($liste.Count -eq 1 -and $liste[0] -is [array]) { $liste = @($liste[0]) }
        $b10.Baslik = "KOPYALAR: $($liste.Count) (kopya.ps1 -Liste -Json)"
        foreach ($k in $liste) {
            $b10.Satirlar.Add(('{0,-22} {1,-12} sahip={2,-18} yas={3,6} dk  {4}' -f $k.Ad, $k.Durum, $k.Sahip, $k.YasDakika, $k.Yol))
        }
    }
}
$bolumler.Add($b10)

# --- sonuc ---
$kapiKapali = ($formatSayisi -gt 0 -or $kapiAcik.Count -gt 0)
$nedenler = New-Object System.Collections.Generic.List[string]
if ($formatSayisi -gt 0) { $nedenler.Add("format hatasi $formatSayisi") }
if ($kapiAcik.Count -gt 0) { $nedenler.Add("acik WIRING/SCENE $($kapiAcik.Count)") }
if ($kapiKapali) { $sonuc = "KAPI KAPALI (cikis 1): $($nedenler -join ', ')" } else { $sonuc = 'KAPI TEMIZ (cikis 0)' }

$cikti = New-Object System.Collections.Generic.List[string]
$cikti.Add("=== TUR SONU KAPISI  $($simdi.ToString('yyyy-MM-dd HH:mm')) ===")
$cikti.Add("Kok: $($script:Kok) | task dosyasi: $($tasks.Count) (frontmatter'li $($fmli.Count)) | kapsam: $kapsamAciklama ($($kapsam.Count) task)")
foreach ($b in $bolumler) {
    $cikti.Add('')
    $sayi = ''
    if ($b.No -in @('1', '3', '7', '8')) { $sayi = ": $($b.Satirlar.Count)" }
    $cikti.Add("[$($b.No)] $($b.Baslik)$sayi")
    $sinir = $b.Satirlar.Count
    if ($Kisa) { $sinir = [Math]::Min($b.KisaSinir, $b.Satirlar.Count) }
    for ($i = 0; $i -lt $sinir; $i++) { $cikti.Add('    ' + (Format-Ts $b.Satirlar[$i] 160)) }
    if ($sinir -lt $b.Satirlar.Count) { $cikti.Add("    ... +$($b.Satirlar.Count - $sinir) satir (tam liste icin -Kisa olmadan)") }
}
$cikti.Add('')
$cikti.Add("SONUC: $sonuc")
foreach ($s in $cikti) { Write-Output (ConvertTo-KitAscii $s) }

# --- DURUM dosyasi ---
if ($DurumYaz) {
    $durumYol = Join-Path $script:Kok $cfg.DurumDosyasi
    if (-not (Test-Path -LiteralPath $durumYol)) {
        Write-Output "UYARI: -DurumYaz: $($cfg.DurumDosyasi) yok, yazilmadi."
    } else {
        $ok = Read-TaskMetni -Path $durumYol
        $metin = $ok.Metin
        $basIsaret = '<!-- OTOMATIK:BASLA -->'
        $sonIsaret = '<!-- OTOMATIK:BITIR -->'
        $ib = -1; $is = -1
        if ($null -ne $metin) { $ib = $metin.IndexOf($basIsaret); if ($ib -ge 0) { $is = $metin.IndexOf($sonIsaret, $ib + $basIsaret.Length) } }
        if ($ib -lt 0 -or $is -lt 0) {
            Write-Output "UYARI: -DurumYaz: $($cfg.DurumDosyasi) icinde $basIsaret / $sonIsaret isaretleri yok, dosyaya dokunulmadi."
        } else {
            $nl = "`n"
            if ($metin -match "`r`n") { $nl = "`r`n" }
            $k = New-Object System.Collections.Generic.List[string]
            $k.Add("_tur-sonu.ps1 $($simdi.ToString('yyyy-MM-dd HH:mm')) - otomatik blok, elle yazilan bu blokta kaybolur._")
            $k.Add('')
            $k.Add("- **$sonuc**")
            $k.Add("- Acik needs: $($acik.Count) ($tipOzet)")
            foreach ($a in @($kapiAcik | Select-Object -First 10)) { $k.Add("  - KAPI $($a.Id) [$($a.Status)] $($a.Tip): $(Format-Ts $a.Not 110)") }
            if ($kapiAcik.Count -gt 10) { $k.Add("  - ... +$($kapiAcik.Count - 10)") }
            if ($formatSayisi -gt 0) { $k.Add("- Format hatasi: $formatSayisi (ilk: $(Format-Ts $b1.Satirlar[0] 110))") }
            $k.Add("- Review kuyrugu: $($review.Count)$uyari4")
            $k.Add("- Goz bekleyen (merged): $($merged.Count)$(if ($merged.Count -gt 0) { ' - ' + ((@($mergedSirali | Select-Object -First 8 | ForEach-Object { $_.Id })) -join ', ') })")
            $k.Add("- in-progress: $($ip.Count)$(if ($ip.Count -gt 0) { ' (' + ((@($ip | ForEach-Object { $_.Id })) -join ', ') + ')' }) / blocked: $($bl.Count)$(if ($bl.Count -gt 0) { ' (' + ((@($bl | ForEach-Object { $_.Id })) -join ', ') + ')' })")
            $k.Add("- Uyari: govde-needs $($b3.Satirlar.Count), test $($b7.Satirlar.Count), analytics $($b8.Satirlar.Count) (kapsam: $kapsamAciklama)")
            $k.Add("- $(Format-Ts $b9.Baslik 120); ana agacta commit'siz .prefab/.unity/.asset: $korunanAna")
            $k.Add("- $(Format-Ts $b10.Baslik 120)")
            $blok = $nl + ((@($k | ForEach-Object { ConvertTo-KitAscii $_ })) -join $nl) + $nl
            $yeni = $metin.Substring(0, $ib + $basIsaret.Length) + $blok + $metin.Substring($is)
            if (-not [string]::Equals($yeni, $metin, [StringComparison]::Ordinal)) { Write-Utf8NoBom -Path $durumYol -Text $yeni }
            Write-Output "DURUM: $($cfg.DurumDosyasi) otomatik blok guncellendi."
        }
    }
}

if ($kapiKapali) { exit 1 }
exit 0
