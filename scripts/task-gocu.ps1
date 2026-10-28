<#
task-gocu.ps1 - task dosyalarini v1 bicimden v2 bicime (templates/task.md) toplu tasir.

Kullanim:
  scripts\task-gocu.ps1 -Root <proje>                       # KURU: hicbir dosyaya yazmaz, ozeti basar
  scripts\task-gocu.ps1 -Root <proje> -Rapor <yol.md>       # KURU + yalniz rapor dosyasi (acikca istendi)
  scripts\task-gocu.ps1 -Root <proje> -Uygula               # task dosyalarini yazar + rapor (vars. <RaporKlasoru>/task-gocu-<Bugun>.md)
Parametreler: -AnaDal (vars. HEAD), -GunEsigi 7, -Bugun yyyy-MM-dd (vars. bugun), -Haric @('T-452',...).

Ne yapar (govdeye yalniz status notu eklenir; baska govde degisikligi YOK):
  1. status normalizasyonu: yorum/serbest metin ayiklanir ("review (verify: ...)" -> review); ayiklanan metin
     "## Calisma notu" sonuna "- (goc dd.MM.yyyy) eski status satiri: <metin>" olarak eklenir (yalniz status
     listesi olan sablon yorumu "# todo | in-progress | ..." not edilmez).
  2. review -> commit eslemesi: AnaDal'dan erisilebilir, mesajinda \bT-123\b gecen commit'ler. En yeni commit
     GunEsigi'nden eski -> done, daha yeni -> merged; commits'e en fazla 10 kisa hash. Commit yoksa review kalir.
  3. needs cikarimi: govdedeki somut NEEDS_(SCENE|WIRING|ASSET) satirlari frontmatter needs'e eklenir
     (task-lib Get-TaskGovdeNeeds). durum: task done/cancelled ya da kapanis isareti -> closed, aksi halde open.
  4. Eksik v2 anahtarlari (branch, commits, tests, analytics, needs) bos degerle eklenir; hicbir anahtar silinmez.
     cancelled + bos blocked_reason + dolu cancelled_reason (v1) -> blocked_reason'a kopyalanir.
  Idempotent: ikinci kosu sifir degisiklik uretir. Frontmatter'i cozulemeyen dosyaya DOKUNULMAZ (raporda listelenir).
Cikis: 0 tamam, 2 script hatasi.
#>
[CmdletBinding()]
param(
    [string]$Root,
    [switch]$Uygula,
    [string]$AnaDal = 'HEAD',
    [int]$GunEsigi = 7,
    [string]$Bugun,
    [string[]]$Haric = @(),
    [string]$Rapor
)

$ErrorActionPreference = 'Stop'
trap {
    [Console]::Error.WriteLine("task-gocu HATA: $($_.Exception.Message) [$($_.InvocationInfo.ScriptLineNumber)]")
    exit 2
}

. (Join-Path $PSScriptRoot 'task-lib.ps1')

function Get-GocKok {
    param([string]$Verilen)
    if ($Verilen) { return (Resolve-Path -LiteralPath $Verilen).Path }
    $eap = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try { $k = Get-KitRoot } finally { $ErrorActionPreference = $eap }
    return $k
}

function Get-GocHamStatus {
    # status satirinin 'status:' sonrasi ham metni (yorum dahil).
    param($Task)
    foreach ($s in $Task.Segmentler) {
        if ($s.Anahtar -ceq 'status') {
            $m = [regex]::Match($s.Satirlar[0], '^status[ \t]*:(.*)$')
            if ($m.Success) { return $m.Groups[1].Value.Trim() }
        }
    }
    return $null
}

function ConvertTo-GocStatus {
    # Doner: @{ Status (gecerli ya da $null); Ek (ayiklanan metin); NotGerekli }
    param([string]$Ham)
    $s = $Ham.Trim()
    if ($s.Length -ge 2 -and (($s[0] -ceq [char]'"') -or ($s[0] -ceq [char]"'"))) {
        $r = Read-TaskTirnakli -Metin $s -Bas 0
        $kalan = ''
        if ($r.Son -lt $s.Length) { $kalan = $s.Substring($r.Son).Trim() }
        $s = ($r.Deger + ' ' + $kalan).Trim()
    }
    # CultureInvariant: tr-TR'de IgnoreCase "REVIEW" ~ "review" eslesmez ('I' -> U+0131 noktasiz i)
    $m = [regex]::Match($s, '^(todo|in-progress|review|blocked|merged|done|cancelled)\b(.*)$', 'IgnoreCase, CultureInvariant')
    if (-not $m.Success) { return @{ Status = $null; Ek = $s; NotGerekli = $false } }
    $st = $m.Groups[1].Value.ToLowerInvariant()
    $ek = $m.Groups[2].Value.Trim()
    $sablon = '^[#\s(]*(?:(?:todo|in-progress|review|blocked|merged|done|cancelled)\s*[|/,]?\s*)+[)\s]*$'
    $notGerekli = ($ek.Length -gt 0) -and -not [regex]::IsMatch($ek, $sablon, 'IgnoreCase, CultureInvariant')
    return @{ Status = $st; Ek = $ek; NotGerekli = $notGerekli }
}

function Add-GocCalismaNotu {
    # Govdede SON "Calisma notu" bolumunun son dolu satirindan sonra $Satir'i ekler; bolum yoksa dosya sonuna
    # "## Calisma notu" + satir. Baska hicbir bayt degismez.
    param([string]$Govde, [string]$Satir, [string]$Nl)
    $satirlar = [regex]::Split($Govde, '\r?\n')
    $baslar = New-Object System.Collections.Generic.List[int]
    $baslar.Add(0)
    for ($i = 0; $i -lt $Govde.Length; $i++) { if ($Govde[$i] -ceq [char]10) { $baslar.Add($i + 1) } }
    $hi = -1
    $hsev = 0
    for ($i = 0; $i -lt $satirlar.Length; $i++) {
        $m = $script:TaskBaslikRegex.Match($satirlar[$i])
        if ($m.Success -and ((ConvertTo-KitAscii $m.Groups[2].Value).ToLowerInvariant() -match '^calisma not')) { $hi = $i; $hsev = $m.Groups[1].Value.Length }
    }
    if ($hi -lt 0) {
        $on = ''
        if ($Govde.Length -gt 0) {
            if (-not $Govde.EndsWith("`n")) { $on = $Nl }
            $on = $on + $Nl
        }
        return ($Govde + $on + '## Calisma notu' + $Nl + $Satir + $Nl)
    }
    $son = $satirlar.Length - 1
    for ($j = $hi + 1; $j -lt $satirlar.Length; $j++) {
        $m = $script:TaskBaslikRegex.Match($satirlar[$j])
        if ($m.Success -and $m.Groups[1].Value.Length -le $hsev) { $son = $j - 1; break }
    }
    $k = $son
    while ($k -gt $hi -and $satirlar[$k].Trim() -eq '') { $k-- }
    $ofs = $baslar[$k] + $satirlar[$k].Length
    return $Govde.Insert($ofs, $Nl + $Satir)
}

function Format-GocHucre {
    param([AllowNull()][string]$Metin)
    if ($null -eq $Metin) { return '' }
    return (($Metin -replace '\r?\n', ' ') -replace '\|', '\|')
}

# ------------------------------------------------------------------------------------------------------------------
$Root = Get-GocKok $Root
$cfg = Get-KitConfig -Root $Root
# powershell -File ile "-Haric T-1,T-2" tek dize gelir: virgul/bosluktan bol
$Haric = @($Haric | ForEach-Object { ([string]$_) -split '[,;\s]+' } | Where-Object { $_ })
if (-not $Bugun) { $Bugun = Get-Date -Format 'yyyy-MM-dd' }
$inv = [System.Globalization.CultureInfo]::InvariantCulture
$bugunTarih = [datetime]::ParseExact($Bugun, 'yyyy-MM-dd', $inv)
$esik = $bugunTarih.AddDays(-$GunEsigi)
$gocEtiketi = $bugunTarih.ToString('dd.MM.yyyy', $inv)
if (-not $Rapor -and $Uygula) { $Rapor = Join-Path (Join-Path $Root $cfg.RaporKlasoru) "task-gocu-$Bugun.md" }

# --- commit dizini: AnaDal'dan erisilebilir commit'lerde gecen T-id'ler ---
$anaHash = ''
$rv = Invoke-TaskGit $Root rev-parse --short $AnaDal
if ($rv.Kod -ne 0) { throw "AnaDal cozulemedi: $AnaDal" }
$anaHash = ($rv.Cikti | Select-Object -First 1)
$lg = Invoke-TaskGit $Root log $AnaDal '--format=%h%x1f%ct%x1f%s%x1f%B%x1e'
if ($lg.Kod -ne 0) { throw "git log $AnaDal basarisiz" }
$commitDizini = @{}
$kayitlar = ($lg.Cikti -join "`n") -split [string][char]0x1e
foreach ($kayit in $kayitlar) {
    $kayit = $kayit.Trim([char[]]@([char]13, [char]10))
    if (-not $kayit) { continue }
    $p = $kayit -split [string][char]0x1f
    if ($p.Count -lt 4) { continue }
    $hash = $p[0].Trim()
    $zaman = [DateTimeOffset]::FromUnixTimeSeconds([long]$p[1]).LocalDateTime
    $baslik = $p[2]
    $mesaj = $p[3]
    $gorulen = @{}
    foreach ($m in [regex]::Matches($mesaj + "`n" + $baslik, '\bT-\d+[A-Za-z]?\b')) {
        $id = $m.Value
        if ($gorulen.ContainsKey($id)) { continue }
        $gorulen[$id] = $true
        if (-not $commitDizini.ContainsKey($id)) { $commitDizini[$id] = New-Object System.Collections.Generic.List[object] }
        $commitDizini[$id].Add([pscustomobject]@{ Hash = $hash; Zaman = $zaman; Baslikta = [regex]::IsMatch($baslik, '\b' + [regex]::Escape($id) + '\b') })
    }
}

# --- task'lar ---
$tumu = @(Get-Tasks -Config $cfg)
$kayitListesi = New-Object System.Collections.Generic.List[object]
$parseHatalari = New-Object System.Collections.Generic.List[object]
$elleBakilacak = New-Object System.Collections.Generic.List[object]
$bilgiCommitli = New-Object System.Collections.Generic.List[object]
$yalnizAnma = New-Object System.Collections.Generic.List[object]
$dogrulamaHatalari = New-Object System.Collections.Generic.List[string]
$onceDagilim = @{}
$sonraDagilim = @{}
$acikNeeds = New-Object System.Collections.Generic.List[object]
$haricSayisi = 0
$yazilan = 0

foreach ($t in $tumu) {
    if ($Haric -and ($Haric -contains $t.Id)) { $haricSayisi++; continue }
    $goreli = $t.Path
    if ($goreli.StartsWith($Root, [StringComparison]::OrdinalIgnoreCase)) { $goreli = $goreli.Substring($Root.Length).TrimStart('\', '/') }
    if (-not $t.FrontmatterVar -or $t.Hatalar.Count -gt 0) {
        $parseHatalari.Add([pscustomobject]@{ Id = $t.Id; Dosya = $goreli; Hatalar = ($t.Hatalar -join '; ') })
        $anahtar = '(frontmatter yok/bozuk)'
        $onceDagilim[$anahtar] = 1 + [int]$onceDagilim[$anahtar]
        $sonraDagilim[$anahtar] = 1 + [int]$sonraDagilim[$anahtar]
        continue
    }
    $k = [pscustomobject]@{
        Id = $t.Id; Dosya = $goreli; EskiStatus = (Get-TaskSkaler $t 'status'); YeniStatus = ''; Commitler = @()
        NeedsOpen = 0; NeedsClosed = 0; EklenenAnahtarlar = @(); Notlar = (New-Object System.Collections.Generic.List[string]); Degisti = $false
    }
    $degerler = [ordered]@{}
    $zorla = @()
    $govde = $null

    # 1) status: ham satir (yorum dahil) gecerli bir status'un kendisi degilse normalize et
    $ham = Get-GocHamStatus $t
    $status = $k.EskiStatus
    if (Test-TaskStatus $status) { $onceAnahtar = $status }
    elseif ($status) { $onceAnahtar = '(gecersiz) ' + $(if ($status.Length -gt 40) { $status.Substring(0, 40) + '...' } else { $status }) }
    else { $onceAnahtar = '(bos)' }
    if ($null -eq $ham) {
        $k.Notlar.Add('status satiri YOK - elle eklenmeli')
    } elseif (($ham -cne $status) -or -not (Test-TaskStatus $status)) {
        $n = ConvertTo-GocStatus $ham
        if ($n.Status) {
            $status = $n.Status
            $degerler['status'] = $status
            $zorla += 'status'
            if ($n.NotGerekli) {
                $govde = Add-GocCalismaNotu -Govde $t.Body -Satir ("- (goc $gocEtiketi) eski status satiri: " + $ham) -Nl $t.SatirSonu
                $k.Notlar.Add('status normalize + calisma notu')
            } else {
                $k.Notlar.Add('status sablon yorumu atildi')
            }
        } else {
            $k.Notlar.Add("GECERSIZ status elle duzeltilmeli: '$ham'")
        }
    }
    $onceDagilim[$onceAnahtar] = 1 + [int]$onceDagilim[$onceAnahtar]

    # 2) review -> commit
    $commitListesi = @()
    if ($commitDizini.ContainsKey($t.Id)) { $commitListesi = @($commitDizini[$t.Id] | Sort-Object Zaman -Descending) }
    if ($status -eq 'review') {
        if ($commitListesi.Count -gt 0) {
            $enYeni = $commitListesi[0]
            if ($enYeni.Zaman -lt $esik) { $status = 'done' } else { $status = 'merged' }
            $degerler['status'] = $status
            $hashler = @($commitListesi | Select-Object -First 10 | ForEach-Object { $_.Hash })
            if (@(Get-TaskListe $t 'commits').Count -eq 0) { $degerler['commits'] = $hashler; $k.Commitler = $hashler }
            else { $k.Commitler = @(Get-TaskListe $t 'commits') }
            $k.Notlar.Add("review -> $status (en yeni commit $($enYeni.Hash) $($enYeni.Zaman.ToString('yyyy-MM-dd')))")
            if (-not (@($commitListesi | Where-Object { $_.Baslikta }).Count -gt 0)) {
                $yalnizAnma.Add([pscustomobject]@{ Id = $t.Id; Status = $status; Hashler = ($hashler -join ', ') })
            }
        } else {
            $elleBakilacak.Add([pscustomobject]@{ Id = $t.Id; Baslik = (Get-TaskSkaler $t 'title'); Dosya = $goreli; Started = (Get-TaskSkaler $t 'started') })
        }
    } elseif ($status -in @('in-progress', 'blocked', 'todo') -and $commitListesi.Count -gt 0) {
        $bilgiCommitli.Add([pscustomobject]@{ Id = $t.Id; Status = $status; EnYeni = $commitListesi[0].Hash; Tarih = $commitListesi[0].Zaman.ToString('yyyy-MM-dd'); Sayi = $commitListesi.Count })
    }
    $k.YeniStatus = $status

    # 3) needs
    $kapaliTask = ($status -eq 'done' -or $status -eq 'cancelled')
    $mevcut = @(Get-TaskNeeds $t)
    $mevcutAnahtarlar = @{}
    foreach ($o in $mevcut) { $mevcutAnahtarlar[(Get-TaskNotAnahtari -Tip ([string]$o['tip']) -Not ([string]$o['not']))] = $true }
    $eklenecek = New-Object System.Collections.Generic.List[object]
    foreach ($gn in @(Get-TaskGovdeNeeds -Govde $t.Body)) {
        $an = Get-TaskNotAnahtari -Tip $gn.Tip -Not $gn.Not
        if ($mevcutAnahtarlar.ContainsKey($an)) { continue }
        # frontmatter'da ayni tipte ayni ihtiyac varsa (durumu ne olursa olsun) EKLENMEZ: kapatan kisi not'un sonuna
        # " -> <commit/aciklama>" ekler; ilk goc notu 160 karakterde "..." ile kesmis ve YAML'da kacislamis olabilir.
        $ayni = $false
        foreach ($o in $mevcut) {
            if ([string]::Equals([string]$o['tip'], [string]$gn.Tip, [StringComparison]::OrdinalIgnoreCase) -and (Test-TaskAyniNot -Yeni $gn.Not -Mevcut ([string]$o['not']))) { $ayni = $true; break }
        }
        if ($ayni) { continue }
        $mevcutAnahtarlar[$an] = $true
        $durum = 'open'
        if ($kapaliTask -or $gn.Kapali) { $durum = 'closed' }
        $h = [ordered]@{ tip = $gn.Tip; durum = $durum; not = $gn.Not }
        $eklenecek.Add($h)
        if ($durum -eq 'open') { $k.NeedsOpen++ } else { $k.NeedsClosed++ }
    }
    if ($eklenecek.Count -gt 0) {
        $yeniNeeds = New-Object System.Collections.Generic.List[object]
        foreach ($o in $mevcut) { $yeniNeeds.Add($o) }
        foreach ($o in $eklenecek) { $yeniNeeds.Add($o) }
        $degerler['needs'] = $yeniNeeds.ToArray()
    }

    # 4) eksik v2 anahtarlari + v1 cancelled_reason
    $eklenen = New-Object System.Collections.Generic.List[string]
    foreach ($a in $script:TaskV2Anahtarlari) {
        if (-not $t.Frontmatter.Contains($a)) {
            $eklenen.Add($a)
            if (-not $degerler.Contains($a)) { if ($a -eq 'branch') { $degerler[$a] = '' } else { $degerler[$a] = @() } }
        }
    }
    $k.EklenenAnahtarlar = $eklenen.ToArray()
    if ($status -eq 'cancelled' -and -not (Get-TaskSkaler $t 'blocked_reason') -and (Get-TaskSkaler $t 'cancelled_reason')) {
        $degerler['blocked_reason'] = Get-TaskSkaler $t 'cancelled_reason'
        $k.Notlar.Add('cancelled_reason -> blocked_reason')
    }

    # sirali degerler: templates/task.md sirasi (yeni anahtarlar dogru yere otursun)
    $sirali = [ordered]@{}
    foreach ($a in $script:TaskAnahtarSirasi) { if ($degerler.Contains($a)) { $sirali[$a] = $degerler[$a] } }
    foreach ($a in @($degerler.Keys)) { if (-not $sirali.Contains($a)) { $sirali[$a] = $degerler[$a] } }

    if ($null -ne $govde) { $yeniMetin = Get-TaskYeniMetin -Task $t -Degerler $sirali -Govde $govde -Zorla $zorla }
    else { $yeniMetin = Get-TaskYeniMetin -Task $t -Degerler $sirali -Zorla $zorla }
    $k.Degisti = -not [string]::Equals($yeniMetin, $t.Metin, [StringComparison]::Ordinal)
    if ($k.Degisti -and $Uygula) {
        Write-TaskMetni -Path $t.Path -Metin $yeniMetin
        $yazilan++
        # yazdiktan sonra dogrula: yeniden cozulur, govde (status notu haric) aynen kalir
        $kontrol = Read-TaskFile -Path $t.Path
        if ($kontrol.Hatalar.Count -gt 0) { $dogrulamaHatalari.Add("$($t.Id): yazim sonrasi parse hatasi: $($kontrol.Hatalar -join '; ')") }
        $beklenenGovde = $t.Body
        if ($null -ne $govde) { $beklenenGovde = $govde }
        if (-not [string]::Equals($kontrol.Body, $beklenenGovde, [StringComparison]::Ordinal)) { $dogrulamaHatalari.Add("$($t.Id): govde degisti") }
        if ($kontrol.Frontmatter['status'] -cne $status) { $dogrulamaHatalari.Add("$($t.Id): status yazilamadi") }
        foreach ($a in @($t.Frontmatter.Keys)) { if (-not $kontrol.Frontmatter.Contains($a)) { $dogrulamaHatalari.Add("$($t.Id): anahtar kayboldu: $a") } }
    }
    $sonraAnahtar = $status
    if (-not $sonraAnahtar) { $sonraAnahtar = '(bos)' }
    if (-not (Test-TaskStatus $status)) { $sonraAnahtar = '(gecersiz) ' + $status }
    $sonraDagilim[$sonraAnahtar] = 1 + [int]$sonraDagilim[$sonraAnahtar]

    # goc sonrasi acik needs (mevcut + eklenen)
    foreach ($o in @($mevcut + $eklenecek.ToArray())) {
        if (([string]$o['durum']) -eq 'open') { $acikNeeds.Add([pscustomobject]@{ Id = $t.Id; Status = $status; Tip = [string]$o['tip']; Not = [string]$o['not'] }) }
    }
    $kayitListesi.Add($k)
}

# --- rapor ---
$degisenler = @($kayitListesi | Where-Object { $_.Degisti })
$reviewDone = @($kayitListesi | Where-Object { $_.EskiStatus -eq 'review' -and $_.YeniStatus -eq 'done' }).Count
$reviewMerged = @($kayitListesi | Where-Object { $_.EskiStatus -eq 'review' -and $_.YeniStatus -eq 'merged' }).Count
$normalize = @($kayitListesi | Where-Object { ($_.Notlar -contains 'status normalize + calisma notu') -or ($_.Notlar -contains 'status sablon yorumu atildi') }).Count
$notEklenen = @($kayitListesi | Where-Object { $_.Notlar -contains 'status normalize + calisma notu' }).Count
$needsOpen = ($kayitListesi | Measure-Object -Property NeedsOpen -Sum).Sum
$needsClosed = ($kayitListesi | Measure-Object -Property NeedsClosed -Sum).Sum
$anahtarEklenen = @($kayitListesi | Where-Object { $_.EklenenAnahtarlar.Count -gt 0 }).Count
$cancelKopya = @($kayitListesi | Where-Object { $_.Notlar -contains 'cancelled_reason -> blocked_reason' }).Count
$mod = 'KURU (hicbir task dosyasina yazilmadi)'
if ($Uygula) { $mod = 'UYGULA' }

$r = New-Object System.Collections.Generic.List[string]
$r.Add("# Task gocu v1 -> v2 - $Bugun")
$r.Add('')
$r.Add("- Kok: ``$Root`` - Mod: **$mod** - AnaDal: ``$AnaDal`` ($anaHash) - GunEsigi: $GunEsigi gun (esik $($esik.ToString('yyyy-MM-dd')): daha eski en yeni commit -> done)")
$r.Add("- Haric: $(if ($Haric) { $Haric -join ', ' } else { '-' }) ($haricSayisi dosya atlandi)")
$r.Add("- T-*.md: $($tumu.Count) dosya; islenen $($kayitListesi.Count); parse hatasi/frontmatter'siz $($parseHatalari.Count); degisen $($degisenler.Count); yazilan $yazilan")
$r.Add('- Script: `scripts/task-gocu.ps1` (kit). Ikinci kosu sifir degisiklik uretir (idempotent).')
$r.Add('')
$r.Add('## 1. Status dagilimi (once -> sonra)')
$r.Add('')
$r.Add('| status | once | sonra |')
$r.Add('|---|---:|---:|')
$tumAnahtarlar = @($onceDagilim.Keys + $sonraDagilim.Keys | Select-Object -Unique)
$siraliAnahtarlar = @($script:TaskStatusleri | Where-Object { $tumAnahtarlar -contains $_ }) + @($tumAnahtarlar | Where-Object { $script:TaskStatusleri -notcontains $_ } | Sort-Object)
foreach ($a in $siraliAnahtarlar) { $r.Add("| $(Format-GocHucre $a) | $([int]$onceDagilim[$a]) | $([int]$sonraDagilim[$a]) |") }
$r.Add('')
$r.Add('## 2. Ozet')
$r.Add('')
$r.Add("- review -> done: **$reviewDone**; review -> merged: **$reviewMerged**; commit'i bulunamayan review (review kaldi): **$($elleBakilacak.Count)**")
$r.Add("- status satiri normalize edildi: $normalize (calisma notuna eski satir eklenen: $notEklenen)")
$r.Add("- needs eklendi: $($needsOpen + $needsClosed) (open $needsOpen, closed $needsClosed)")
$tipeGore = @($acikNeeds | Group-Object Tip | Sort-Object Name | ForEach-Object { "$($_.Name) $($_.Count)" })
$r.Add("- goc sonrasi ACIK needs: $($acikNeeds.Count) ($(if ($tipeGore) { $tipeGore -join ', ' } else { '-' })); WIRING/SCENE kapisi: $(@($acikNeeds | Where-Object { $_.Tip -in $script:TaskKapiTipleri -and $_.Status -ne 'cancelled' }).Count)")
$r.Add("- eksik v2 anahtari eklenen dosya: $anahtarEklenen; cancelled_reason -> blocked_reason: $cancelKopya")
if ($dogrulamaHatalari.Count -gt 0) { $r.Add("- **YAZIM DOGRULAMA HATASI: $($dogrulamaHatalari.Count)** (asagida)") }
$r.Add('')
$r.Add('## 3. Goc sonrasi ACIK needs')
$r.Add('')
if ($acikNeeds.Count -eq 0) { $r.Add('(yok)') } else {
    $r.Add('| id | status | tip | not |')
    $r.Add('|---|---|---|---|')
    foreach ($a in ($acikNeeds | Sort-Object @{ Expression = { Get-TaskIdSayisi $_.Id } }, Tip)) { $r.Add("| $($a.Id) | $($a.Status) | $($a.Tip) | $(Format-GocHucre $a.Not) |") }
}
$r.Add('')
$r.Add("## 4. Commit'i bulunamayan review'lar (elle bakilacak)")
$r.Add('')
if ($elleBakilacak.Count -eq 0) { $r.Add('(yok)') } else {
    $r.Add('| id | started | baslik |')
    $r.Add('|---|---|---|')
    foreach ($a in $elleBakilacak) { $r.Add("| $($a.Id) | $($a.Started) | $(Format-GocHucre $a.Baslik) |") }
}
$r.Add('')
$r.Add("## 5. Parse hatalari / frontmatter'siz dosyalar (DOKUNULMADI)")
$r.Add('')
if ($parseHatalari.Count -eq 0) { $r.Add('(yok)') } else {
    foreach ($a in $parseHatalari) { $r.Add("- ``$($a.Dosya)``: $(Format-GocHucre $a.Hatalar)") }
}
$r.Add('')
$r.Add("## 6. Bilgi: commit'i olan ama in-progress / blocked / todo kalan task'lar (goc kurali yalniz review'i esler)")
$r.Add('')
if ($bilgiCommitli.Count -eq 0) { $r.Add('(yok)') } else {
    foreach ($a in $bilgiCommitli) { $r.Add("- $($a.Id) [$($a.Status)]: $($a.Sayi) commit, en yeni $($a.EnYeni) ($($a.Tarih))") }
}
$r.Add('')
$r.Add("## 7. Bilgi: esleme yalniz anmadan (hicbir commit basliginda id yok; govdede geciyor)")
$r.Add('')
if ($yalnizAnma.Count -eq 0) { $r.Add('(yok)') } else {
    foreach ($a in $yalnizAnma) { $r.Add("- $($a.Id) -> $($a.Status): $($a.Hashler)") }
}
if ($dogrulamaHatalari.Count -gt 0) {
    $r.Add('')
    $r.Add('## YAZIM DOGRULAMA HATALARI')
    $r.Add('')
    foreach ($a in $dogrulamaHatalari) { $r.Add("- $a") }
}
$r.Add('')
$r.Add('## 8. Task basina degisiklik tablosu')
$r.Add('')
if ($degisenler.Count -eq 0) { $r.Add('(degisiklik yok)') } else {
    $r.Add('| id | status | commits | needs + (open/closed) | eklenen anahtar | not |')
    $r.Add('|---|---|---|---|---|---|')
    foreach ($a in $degisenler) {
        $st = $a.EskiStatus
        if ($a.EskiStatus -cne $a.YeniStatus) { $st = "$($a.EskiStatus) -> $($a.YeniStatus)" }
        $cm = ''
        if ($a.Commitler.Count -gt 0) { $cm = "$($a.Commitler.Count): $($a.Commitler[0])" }
        $nd = ''
        if (($a.NeedsOpen + $a.NeedsClosed) -gt 0) { $nd = "$($a.NeedsOpen)/$($a.NeedsClosed)" }
        $r.Add("| $($a.Id) | $(Format-GocHucre $st) | $cm | $nd | $($a.EklenenAnahtarlar -join ', ') | $(Format-GocHucre ($a.Notlar -join '; ')) |")
    }
}
$raporMetni = ($r.ToArray() -join "`n") + "`n"

if ($Rapor) {
    $dizin = Split-Path -Parent $Rapor
    if ($dizin -and -not (Test-Path -LiteralPath $dizin)) { New-Item -ItemType Directory -Force -Path $dizin | Out-Null }
    Write-Utf8NoBom -Path $Rapor -Text $raporMetni
}

# --- konsol ozeti (ASCII) ---
Write-Output (ConvertTo-KitAscii "task-gocu [$mod] kok=$Root AnaDal=$AnaDal ($anaHash) esik=$($esik.ToString('yyyy-MM-dd'))")
Write-Output (ConvertTo-KitAscii "  dosya $($tumu.Count) | islenen $($kayitListesi.Count) | haric $haricSayisi | parse hatasi $($parseHatalari.Count) | degisen $($degisenler.Count) | yazilan $yazilan")
Write-Output "  review -> done $reviewDone | review -> merged $reviewMerged | review kaldi (commit yok) $($elleBakilacak.Count)"
Write-Output "  status normalize $normalize (not eklenen $notEklenen) | needs eklendi open $needsOpen / closed $needsClosed | acik needs $($acikNeeds.Count) ($($tipeGore -join ', '))"
$onceY = @($siraliAnahtarlar | ForEach-Object { "$_=$([int]$onceDagilim[$_])" }) -join ' '
$sonraY = @($siraliAnahtarlar | ForEach-Object { "$_=$([int]$sonraDagilim[$_])" }) -join ' '
Write-Output (ConvertTo-KitAscii "  once : $onceY")
Write-Output (ConvertTo-KitAscii "  sonra: $sonraY")
if ($dogrulamaHatalari.Count -gt 0) { Write-Output "  YAZIM DOGRULAMA HATASI: $($dogrulamaHatalari.Count)"; foreach ($a in $dogrulamaHatalari) { Write-Output (ConvertTo-KitAscii "    $a") } }
if ($Rapor) { Write-Output (ConvertTo-KitAscii "  rapor: $Rapor") }
if ($dogrulamaHatalari.Count -gt 0) { exit 2 }
exit 0
