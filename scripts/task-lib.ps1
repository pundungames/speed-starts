# task-lib.ps1 - task dosyalarini (<TaskKlasoru>/**/T-*.md) makineyle okuma ve yazma.
# Kullanim (script basinda):  . (Join-Path $PSScriptRoot 'task-lib.ps1')
#
# Format sozlesmesi: templates/task.md (frontmatter kurallari HTML yorumunda). Parser YALNIZ su bicimleri bilir:
#   anahtar: deger | anahtar: [a, b] | anahtar: [] | blok liste ("  - deger") | needs harita listesi (tip/durum/not)
# Tirnakli degerler ("..." ve '...') acilir; v1 dosyalarindaki satir sonu yorumlari ("status: todo   # ...") tolere edilir.
#
# Windows PowerShell 5.1 uyumlu: ?? / ?: / ?. YOK. Bu dosyada ASCII disi karakter YOK (PS 5.1 BOM'suz .ps1'i ANSI okur);
# Turkce harfler regex'te \uXXXX, metinde [char] koduyla yazilir.

if (-not (Get-Command Get-KitConfig -ErrorAction SilentlyContinue)) { . (Join-Path $PSScriptRoot 'kit-lib.ps1') }

# --- Sozlesme sabitleri (tek yer) -------------------------------------------------------------------------------
$script:TaskStatusleri      = @('todo', 'in-progress', 'review', 'blocked', 'merged', 'done', 'cancelled')
$script:TaskAnahtarSirasi   = @('id', 'title', 'status', 'owner', 'depends_on', 'owns_paths', 'touches_scene', 'branch',
                                'commits', 'tests', 'analytics', 'needs', 'started', 'finished', 'blocked_reason')
$script:TaskV2Anahtarlari   = @('branch', 'commits', 'tests', 'analytics', 'needs')
$script:TaskListeAnahtarlari = @('commits', 'tests', 'analytics', 'needs', 'depends_on', 'owns_paths')
$script:TaskHaritaListeleri = @('needs')
$script:TaskNeedTipleri     = @('SCENE', 'WIRING', 'ASSET', 'DECISION')
$script:TaskNeedDurumlari   = @('open', 'closed')
$script:TaskKapiTipleri     = @('WIRING', 'SCENE')
$script:TaskNotSiniri       = 160

function Get-TaskStatusKumesi { return $script:TaskStatusleri }

function Test-TaskStatus {
    param([AllowNull()][string]$Deger)
    if ([string]::IsNullOrEmpty($Deger)) { return $false }
    foreach ($s in $script:TaskStatusleri) { if ([string]::Equals($s, $Deger, [StringComparison]::Ordinal)) { return $true } }
    return $false
}

# --- ASCII ---------------------------------------------------------------------------------------------------------
$script:KitAsciiHarita = New-Object 'System.Collections.Generic.Dictionary[int,string]'
foreach ($cift in @(
        @(0x131, 'i'), @(0x130, 'I'), @(0x11F, 'g'), @(0x11E, 'G'), @(0x15F, 's'), @(0x15E, 'S'),
        @(0xE7, 'c'), @(0xC7, 'C'), @(0xF6, 'o'), @(0xD6, 'O'), @(0xFC, 'u'), @(0xDC, 'U'),
        @(0xE2, 'a'), @(0xC2, 'A'), @(0xEE, 'i'), @(0xCE, 'I'), @(0xFB, 'u'), @(0xDB, 'U'), @(0xE9, 'e'), @(0xE1, 'a'),
        @(0x2014, '-'), @(0x2013, '-'), @(0x2212, '-'), @(0x2192, '->'), @(0x2190, '<-'), @(0x2194, '<->'),
        @(0x21D2, '=>'), @(0x2026, '...'), @(0xD7, 'x'), @(0x2018, "'"), @(0x2019, "'"), @(0x201C, '"'), @(0x201D, '"'),
        @(0x2713, 'OK'), @(0x2714, 'OK'), @(0x2705, 'OK'), @(0x274C, 'X'), @(0xA0, ' '), @(0x2265, '>='), @(0x2264, '<='),
        @(0x2022, '*'), @(0xA7, 'S'), @(0xB0, 'deg'), @(0x2248, '~'))) {
    $script:KitAsciiHarita[[int]$cift[0]] = [string]$cift[1]
}

function ConvertTo-KitAscii {
    # PS 5.1 konsolunda kodlama sorunu olmasin diye cikti metnini ASCII'ye indirir (Turkce harf -> latin karsiligi).
    param([AllowNull()][string]$Metin)
    if ([string]::IsNullOrEmpty($Metin)) { return '' }
    $sb = New-Object System.Text.StringBuilder ($Metin.Length)
    foreach ($c in $Metin.ToCharArray()) {
        $k = [int]$c
        if ($k -lt 128) { [void]$sb.Append($c) }
        elseif ($script:KitAsciiHarita.ContainsKey($k)) { [void]$sb.Append($script:KitAsciiHarita[$k]) }
        elseif ($k -ge 0xDC00 -and $k -le 0xDFFF) { }   # vekil ciftin ikinci yarisi: ilk yari zaten '?' oldu
        else { [void]$sb.Append('?') }
    }
    return $sb.ToString()
}

# --- git ----------------------------------------------------------------------------------------------------------
function Invoke-TaskGit {
    # git'i -C <Dizin> ile cagirir; stderr atilir (PS 5.1'de EAP=Stop iken 2>$null bile hata firlatir, burada Continue).
    param([Parameter(Mandatory)][string]$Dizin, [Parameter(ValueFromRemainingArguments = $true)][string[]]$Arglar)
    $git = Get-GitExe
    $eskiEap = $ErrorActionPreference
    $eskiKod = $null
    try { $eskiKod = [Console]::OutputEncoding } catch { }
    try {
        $ErrorActionPreference = 'Continue'
        try { [Console]::OutputEncoding = New-Object System.Text.UTF8Encoding($false) } catch { }
        $cikti = & $git -C $Dizin @Arglar 2>$null
        $kod = $LASTEXITCODE
    } finally {
        $ErrorActionPreference = $eskiEap
        if ($eskiKod) { try { [Console]::OutputEncoding = $eskiKod } catch { } }
    }
    return [pscustomobject]@{ Cikti = @($cikti | ForEach-Object { [string]$_ }); Kod = $kod }
}

# --- Okuma --------------------------------------------------------------------------------------------------------
function Read-TaskMetni {
    # Dosyayi kati UTF-8 olarak okur. BOM ayiklanir ve kaydedilir.
    param([Parameter(Mandatory)][string]$Path)
    $b = [System.IO.File]::ReadAllBytes($Path)
    $bom = ($b.Length -ge 3 -and $b[0] -eq 0xEF -and $b[1] -eq 0xBB -and $b[2] -eq 0xBF)
    $bas = 0
    if ($bom) { $bas = 3 }
    $enc = New-Object System.Text.UTF8Encoding($false, $true)
    try { $t = $enc.GetString($b, $bas, $b.Length - $bas) }
    catch { return @{ Metin = $null; Bom = $bom; Hata = 'dosya gecerli UTF-8 degil' } }
    return @{ Metin = $t; Bom = $bom; Hata = $null }
}

function Read-TaskTirnakli {
    # $Metin[$Bas] bir tirnak (" ya da '). Doner: @{ Deger; Son (kapanistan sonraki indeks); Hata }
    param([string]$Metin, [int]$Bas)
    $q = $Metin[$Bas]
    $sb = New-Object System.Text.StringBuilder
    $i = $Bas + 1
    $n = $Metin.Length
    while ($i -lt $n) {
        $c = $Metin[$i]
        if ($q -ceq [char]'"') {
            if ($c -ceq [char]'\' -and ($i + 1) -lt $n) {
                $e = [string]$Metin[$i + 1]
                switch -CaseSensitive ($e) {
                    '"'     { [void]$sb.Append('"') }
                    '\'     { [void]$sb.Append('\') }
                    'n'     { [void]$sb.Append("`n") }
                    't'     { [void]$sb.Append("`t") }
                    default { [void]$sb.Append('\').Append($e) }
                }
                $i += 2
                continue
            }
            if ($c -ceq [char]'"') { return @{ Deger = $sb.ToString(); Son = $i + 1; Hata = $null } }
        } else {
            if ($c -ceq [char]"'") {
                if (($i + 1) -lt $n -and $Metin[$i + 1] -ceq [char]"'") { [void]$sb.Append("'"); $i += 2; continue }
                return @{ Deger = $sb.ToString(); Son = $i + 1; Hata = $null }
            }
        }
        [void]$sb.Append($c)
        $i++
    }
    return @{ Deger = $sb.ToString(); Son = $n; Hata = 'tirnak kapanmiyor' }
}

function ConvertFrom-TaskSkaler {
    # 'anahtar:' sonrasindaki ham metin -> @{ Deger; Yorum; Hata }. Satir sonu yorumu (bosluk + #) ayiklanir.
    param([AllowNull()][string]$Ham)
    if ($null -eq $Ham) { $Ham = '' }
    $s = $Ham.Trim()
    if ($s.Length -eq 0) { return @{ Deger = ''; Yorum = ''; Hata = $null } }
    if ($s[0] -ceq [char]'"' -or $s[0] -ceq [char]"'") {
        $r = Read-TaskTirnakli -Metin $s -Bas 0
        $kalan = ''
        if ($r.Son -lt $s.Length) { $kalan = $s.Substring($r.Son).Trim() }
        $hata = $r.Hata
        $yorum = ''
        if ($kalan -and -not $kalan.StartsWith('#') -and -not $hata) {
            # v1 hosgorusu: 'title: "Jackpot" yazisi kaliyor' gecerli YAML degil ama duz metin olarak okunur
            $m = [regex]::Match($s, '\s#')
            if ($m.Success) { return @{ Deger = $s.Substring(0, $m.Index).TrimEnd(); Yorum = $s.Substring($m.Index).Trim(); Hata = $null } }
            return @{ Deger = $s; Yorum = ''; Hata = $null }
        }
        if ($kalan) { $yorum = $kalan }
        return @{ Deger = $r.Deger; Yorum = $yorum; Hata = $hata }
    }
    if ($s.StartsWith('#')) { return @{ Deger = ''; Yorum = $s; Hata = $null } }
    $m = [regex]::Match($s, '\s#')
    if ($m.Success) { return @{ Deger = $s.Substring(0, $m.Index).TrimEnd(); Yorum = $s.Substring($m.Index).Trim(); Hata = $null } }
    return @{ Deger = $s; Yorum = ''; Hata = $null }
}

function ConvertFrom-TaskSatirIciListe {
    # '[a, "b", c]' (+ istege bagli yorum) -> @{ Deger (object[]); Hata }
    param([string]$Metin)
    $ogeler = New-Object System.Collections.Generic.List[object]
    $hata = $null
    $n = $Metin.Length
    $i = 1
    $kapandi = $false
    while ($i -lt $n) {
        while ($i -lt $n -and [char]::IsWhiteSpace($Metin[$i])) { $i++ }
        if ($i -ge $n) { break }
        $c = $Metin[$i]
        if ($c -ceq [char]']') { $i++; $kapandi = $true; break }
        if ($c -ceq [char]'"' -or $c -ceq [char]"'") {
            $r = Read-TaskTirnakli -Metin $Metin -Bas $i
            if ($r.Hata) { $hata = $r.Hata }
            $ogeler.Add($r.Deger)
            $i = $r.Son
        } else {
            $j = $i
            while ($j -lt $n -and $Metin[$j] -cne [char]',' -and $Metin[$j] -cne [char]']') { $j++ }
            $oge = $Metin.Substring($i, $j - $i).Trim()
            if ($oge.Length -gt 0) { $ogeler.Add($oge) }
            $i = $j
        }
        while ($i -lt $n -and [char]::IsWhiteSpace($Metin[$i])) { $i++ }
        if ($i -lt $n -and $Metin[$i] -ceq [char]',') { $i++; continue }
        if ($i -lt $n -and $Metin[$i] -ceq [char]']') { $i++; $kapandi = $true; break }
        if ($i -lt $n) { $hata = "listede beklenmeyen karakter: '$($Metin[$i])'"; break }
    }
    if (-not $kapandi -and -not $hata) { $hata = "satir ici liste ']' ile kapanmiyor" }
    if ($i -lt $n) {
        $kalan = $Metin.Substring($i).Trim()
        if ($kalan -and -not $kalan.StartsWith('#') -and -not $hata) { $hata = "listeden sonra fazla metin: '$kalan'" }
    }
    return @{ Deger = $ogeler.ToArray(); Hata = $hata }
}

function New-TaskSegment {
    param([AllowNull()][string]$Anahtar)
    return @{ Anahtar = $Anahtar; Satirlar = (New-Object System.Collections.Generic.List[string]) }
}

function Read-TaskFile {
    # Doner: Path, Id, Frontmatter (sirali hashtable), FrontmatterSatirlari, Body, Hatalar
    #        + FrontmatterVar, SatirSonu, BomVardi, Metin, AcilisSatiri, KapanisSatiri, KapanisSonu, Segmentler
    param([Parameter(Mandatory)][string]$Path)
    $hatalar = New-Object System.Collections.Generic.List[string]
    $fm = [ordered]@{}
    $fmSatirlari = New-Object System.Collections.Generic.List[string]
    $segler = New-Object System.Collections.Generic.List[object]
    $yol = (Resolve-Path -LiteralPath $Path).Path
    $okuma = Read-TaskMetni -Path $yol
    $obj = [pscustomobject]@{
        Path = $yol; Id = ''; Frontmatter = $fm; FrontmatterSatirlari = @(); Body = ''; Hatalar = @()
        FrontmatterVar = $false; SatirSonu = "`n"; BomVardi = [bool]$okuma.Bom; Metin = $okuma.Metin
        AcilisSatiri = '---'; KapanisSatiri = '---'; KapanisSonu = ''; Segmentler = @()
    }
    $dosyaId = ''
    $mAd = [regex]::Match([System.IO.Path]::GetFileName($yol), '^(T-\d+[A-Za-z]?)')
    if ($mAd.Success) { $dosyaId = $mAd.Groups[1].Value }
    $obj.Id = $dosyaId
    if ($okuma.Hata) { $hatalar.Add($okuma.Hata); $obj.Hatalar = $hatalar.ToArray(); return $obj }
    $metin = $okuma.Metin
    if ($metin -match "`r`n") { $obj.SatirSonu = "`r`n" }

    $m0 = [regex]::Match($metin, '\A(---[ \t]*)(\r?\n)')
    if (-not $m0.Success) {
        $hatalar.Add('frontmatter yok (ilk satir --- degil)')
        $obj.Body = $metin
        $obj.Hatalar = $hatalar.ToArray()
        return $obj
    }
    $obj.AcilisSatiri = $m0.Groups[1].Value
    $obj.SatirSonu = $m0.Groups[2].Value
    $pos = $m0.Length
    $kapanisBulundu = $false
    while ($pos -le $metin.Length) {
        $idx = $metin.IndexOf("`n", $pos)
        if ($idx -lt 0) { $ham = $metin.Substring($pos) } else { $ham = $metin.Substring($pos, $idx - $pos) }
        $icerik = $ham.TrimEnd([char]13)
        if ($icerik -match '^---[ \t]*$') {
            $kapanisBulundu = $true
            $obj.KapanisSatiri = $icerik
            $geri = $metin.Substring($pos + $icerik.Length)
            $mSon = [regex]::Match($geri, '\A\r?\n')
            if ($mSon.Success) { $obj.KapanisSonu = $mSon.Value; $obj.Body = $geri.Substring($mSon.Length) }
            else { $obj.KapanisSonu = ''; $obj.Body = $geri }
            break
        }
        $fmSatirlari.Add($icerik)
        if ($idx -lt 0) { break }
        $pos = $idx + 1
    }
    if (-not $kapanisBulundu) {
        $hatalar.Add('frontmatter kapanmiyor (ikinci --- satiri yok)')
        $obj.FrontmatterSatirlari = $fmSatirlari.ToArray()
        $obj.Body = ''
        $obj.Hatalar = $hatalar.ToArray()
        return $obj
    }
    $obj.FrontmatterVar = $true
    $obj.FrontmatterSatirlari = $fmSatirlari.ToArray()

    # --- frontmatter satirlarini coz ---
    $aktif = New-TaskSegment $null
    $segler.Add($aktif)
    $aktifAnahtar = $null
    $aktifListe = $null
    $aktifHarita = $null
    for ($i = 0; $i -lt $fmSatirlari.Count; $i++) {
        $s = $fmSatirlari[$i]
        $no = $i + 2
        if ($s -match '^\s*$' -or $s -match '^\s*#') { $aktif.Satirlar.Add($s); continue }
        $mk = [regex]::Match($s, '^([A-Za-z_][A-Za-z0-9_-]*)[ \t]*:(.*)$')
        if ($mk.Success) {
            if ($null -ne $aktifListe) { $fm[$aktifAnahtar] = $aktifListe.ToArray(); $aktifListe = $null }
            $k = $mk.Groups[1].Value
            $r = $mk.Groups[2].Value
            $aktif = New-TaskSegment $k
            $segler.Add($aktif)
            $aktif.Satirlar.Add($s)
            $aktifAnahtar = $k
            $aktifHarita = $null
            if ($fm.Contains($k)) { $hatalar.Add("satir ${no}: yinelenen anahtar '$k'") }
            $rt = $r.Trim()
            if ($rt.StartsWith('[')) {
                $l = ConvertFrom-TaskSatirIciListe -Metin $rt
                if ($l.Hata) { $hatalar.Add("satir ${no} ($k): $($l.Hata)") }
                $fm[$k] = $l.Deger
            } else {
                $v = ConvertFrom-TaskSkaler -Ham $r
                if ($v.Hata) { $hatalar.Add("satir ${no} ($k): $($v.Hata)") }
                $fm[$k] = [string]$v.Deger
            }
            continue
        }
        $ml = [regex]::Match($s, '^(\s+)-(?:\s+(.*))?$')
        if ($ml.Success) {
            $aktif.Satirlar.Add($s)
            if (-not $aktifAnahtar) { $hatalar.Add("satir ${no}: anahtarsiz liste maddesi"); continue }
            if ($null -eq $aktifListe) {
                $mevcut = $fm[$aktifAnahtar]
                if ($mevcut -is [string] -and $mevcut -eq '') { $aktifListe = New-Object System.Collections.Generic.List[object] }
                else { $hatalar.Add("satir ${no} (${aktifAnahtar}): deger varken liste maddesi"); continue }
            }
            $ogeHam = ''
            if ($ml.Groups[2].Success) { $ogeHam = $ml.Groups[2].Value }
            if ($script:TaskHaritaListeleri -contains $aktifAnahtar) {
                $mh = [regex]::Match($ogeHam, '^([A-Za-z_][A-Za-z0-9_]*)[ \t]*:(\s.*|)$')
                if ($mh.Success) {
                    $h = [ordered]@{}
                    $v = ConvertFrom-TaskSkaler -Ham $mh.Groups[2].Value
                    if ($v.Hata) { $hatalar.Add("satir ${no} (${aktifAnahtar}): $($v.Hata)") }
                    $h[$mh.Groups[1].Value] = [string]$v.Deger
                    $aktifListe.Add($h)
                    $aktifHarita = $h
                } else {
                    $hatalar.Add("satir ${no} (${aktifAnahtar}): madde harita degil (tip/durum/not bekleniyor)")
                    $v = ConvertFrom-TaskSkaler -Ham $ogeHam
                    $aktifListe.Add([string]$v.Deger)
                    $aktifHarita = $null
                }
            } else {
                $v = ConvertFrom-TaskSkaler -Ham $ogeHam
                if ($v.Hata) { $hatalar.Add("satir ${no} (${aktifAnahtar}): $($v.Hata)") }
                $aktifListe.Add([string]$v.Deger)
                $aktifHarita = $null
            }
            continue
        }
        $mc = [regex]::Match($s, '^\s+([A-Za-z_][A-Za-z0-9_]*)[ \t]*:(\s.*|)$')
        if ($mc.Success -and $null -ne $aktifHarita) {
            $aktif.Satirlar.Add($s)
            $alan = $mc.Groups[1].Value
            $v = ConvertFrom-TaskSkaler -Ham $mc.Groups[2].Value
            if ($v.Hata) { $hatalar.Add("satir ${no} (${aktifAnahtar}.${alan}): $($v.Hata)") }
            if ($aktifHarita.Contains($alan)) { $hatalar.Add("satir ${no} (${aktifAnahtar}): yinelenen alan '$alan'") }
            $aktifHarita[$alan] = [string]$v.Deger
            continue
        }
        $aktif.Satirlar.Add($s)
        $hatalar.Add("satir ${no}: cozulemedi: '$($s.Trim())'")
    }
    if ($null -ne $aktifListe) { $fm[$aktifAnahtar] = $aktifListe.ToArray() }

    $obj.Segmentler = $segler.ToArray()
    if ($fm.Contains('id') -and ($fm['id'] -is [string]) -and $fm['id']) { $obj.Id = $fm['id'] }
    $obj.Hatalar = $hatalar.ToArray()
    return $obj
}

function Get-Tasks {
    # <Root>/<TaskKlasoru> altinda ozyinelemeli T-*.md -> Read-TaskFile nesneleri (yol sirasiyla).
    param([Parameter(Mandatory)]$Config, [string[]]$Haric = @())
    $kok = Join-Path $Config.Root $Config.TaskKlasoru
    $sonuc = New-Object System.Collections.Generic.List[object]
    if (-not (Test-Path -LiteralPath $kok)) { return $sonuc.ToArray() }
    $dosyalar = Get-ChildItem -LiteralPath $kok -Recurse -File -Filter 'T-*.md' | Sort-Object FullName
    foreach ($d in $dosyalar) {
        $t = Read-TaskFile -Path $d.FullName
        if ($Haric -and ($Haric -contains $t.Id)) { continue }
        $sonuc.Add($t)
    }
    return $sonuc.ToArray()
}

# --- Deger okuma yardimcilari ------------------------------------------------------------------------------------
# Dizi donduren fonksiyonlar diziyi ACAR (PowerShell boru hatti); cagiran her zaman @(...) ile sarar: @(Get-TaskListe $t 'tests').
function Test-TaskListeDegeri {
    param($Deger)
    if ($null -eq $Deger) { return $false }
    if ($Deger -is [string]) { return $false }
    if ($Deger -is [System.Collections.IDictionary]) { return $false }
    return ($Deger -is [System.Collections.IEnumerable])
}

function Get-TaskSkaler {
    param([Parameter(Mandatory)]$Task, [Parameter(Mandatory)][string]$Anahtar)
    if (-not $Task.Frontmatter.Contains($Anahtar)) { return '' }
    $v = $Task.Frontmatter[$Anahtar]
    if ($null -eq $v) { return '' }
    if (Test-TaskListeDegeri $v) { return (@($v) | ForEach-Object { [string]$_ }) -join ', ' }
    return [string]$v
}

function Get-TaskListe {
    # Her zaman dizi doner: '' -> @(), 'x' -> @('x'), liste -> liste.
    param([Parameter(Mandatory)]$Task, [Parameter(Mandatory)][string]$Anahtar)
    if (-not $Task.Frontmatter.Contains($Anahtar)) { return @() }
    $v = $Task.Frontmatter[$Anahtar]
    if ($null -eq $v) { return @() }
    if (Test-TaskListeDegeri $v) { return @($v) }
    if ($v -is [string] -and $v.Trim() -eq '') { return @() }
    return @($v)
}

function Get-TaskNeeds {
    # Frontmatter needs maddeleri (yalniz harita olanlar).
    param([Parameter(Mandatory)]$Task)
    $l = New-Object System.Collections.Generic.List[object]
    foreach ($o in (Get-TaskListe -Task $Task -Anahtar 'needs')) { if ($o -is [System.Collections.IDictionary]) { $l.Add($o) } }
    return $l.ToArray()
}

function Get-TaskIdSayisi {
    param([AllowNull()][string]$Id)
    $m = [regex]::Match([string]$Id, '\d+')
    if ($m.Success) { return [int]$m.Value }
    return 0
}

# --- Yazma --------------------------------------------------------------------------------------------------------
function Test-TaskDegerEsit {
    param($A, $B)
    if ($null -eq $A) { $A = '' }
    if ($null -eq $B) { $B = '' }
    if ($A -is [System.Collections.IDictionary] -or $B -is [System.Collections.IDictionary]) {
        if (-not ($A -is [System.Collections.IDictionary] -and $B -is [System.Collections.IDictionary])) { return $false }
        if ($A.Count -ne $B.Count) { return $false }
        foreach ($k in @($A.Keys)) {
            if (-not $B.Contains($k)) { return $false }
            if (-not (Test-TaskDegerEsit $A[$k] $B[$k])) { return $false }
        }
        return $true
    }
    $al = Test-TaskListeDegeri $A
    $bl = Test-TaskListeDegeri $B
    if ($al -or $bl) {
        if (-not ($al -and $bl)) { return $false }
        $aa = @($A); $bb = @($B)
        if ($aa.Count -ne $bb.Count) { return $false }
        for ($i = 0; $i -lt $aa.Count; $i++) { if (-not (Test-TaskDegerEsit $aa[$i] $bb[$i])) { return $false } }
        return $true
    }
    return [string]::Equals([string]$A, [string]$B, [StringComparison]::Ordinal)
}

function ConvertTo-TaskYamlMetin {
    # Skaleri yaziya cevirir; parser'in yanlis okuyacagi durumlarda (ya da -Zorla) cift tirnak + kacis.
    param([AllowNull()][string]$Metin, [switch]$Zorla, [switch]$ListeOgesi)
    if ($null -eq $Metin) { $Metin = '' }
    $Metin = ($Metin -replace "[\r\n\t]+", ' ')
    $gerek = $Zorla.IsPresent
    if (-not $gerek) {
        if ($Metin.Length -eq 0) { $gerek = $ListeOgesi.IsPresent }
        elseif ($Metin -match '^\s|\s$') { $gerek = $true }
        elseif ($Metin -match '^[\[\]{}"''#&*!|>%@`]') { $gerek = $true }
        elseif ($Metin -match '^-(\s|$)') { $gerek = $true }
        elseif ($Metin -match '\s#') { $gerek = $true }
        elseif ($Metin -match ':(\s|$)') { $gerek = $true }
    }
    if (-not $gerek) { return $Metin }
    return '"' + $Metin.Replace('\', '\\').Replace('"', '\"') + '"'
}

function ConvertTo-TaskDegerSatirlari {
    # Anahtar + deger -> frontmatter satirlari (string[]).
    param([Parameter(Mandatory)][string]$Anahtar, $Deger)
    $l = New-Object System.Collections.Generic.List[string]
    if (Test-TaskListeDegeri $Deger) {
        $ogeler = @($Deger)
        if ($ogeler.Count -eq 0) { $l.Add("${Anahtar}: []"); return $l.ToArray() }
        $haritaVar = $false
        foreach ($o in $ogeler) { if ($o -is [System.Collections.IDictionary]) { $haritaVar = $true } }
        if ($haritaVar) {
            $l.Add("${Anahtar}:")
            foreach ($o in $ogeler) {
                if (-not ($o -is [System.Collections.IDictionary])) { $l.Add('  - ' + (ConvertTo-TaskYamlMetin ([string]$o) -ListeOgesi)); continue }
                $sira = New-Object System.Collections.Generic.List[string]
                foreach ($k in @('tip', 'durum', 'not')) { if ($o.Contains($k)) { $sira.Add($k) } }
                foreach ($k in @($o.Keys)) { if (-not $sira.Contains([string]$k)) { $sira.Add([string]$k) } }
                $ilk = $true
                foreach ($k in $sira) {
                    $v = [string]$o[$k]
                    if ($k -eq 'not') { $yazi = ConvertTo-TaskYamlMetin $v -Zorla } else { $yazi = ConvertTo-TaskYamlMetin $v }
                    if ($ilk) { $l.Add("  - ${k}: $yazi"); $ilk = $false } else { $l.Add("    ${k}: $yazi") }
                }
            }
            return $l.ToArray()
        }
        $basit = $true
        $toplam = 0
        foreach ($o in $ogeler) {
            $s = [string]$o
            $toplam += $s.Length + 2
            if ($s -notmatch '^[A-Za-z0-9_./@+-]+$') { $basit = $false }
        }
        if ($basit -and $toplam -le 90) {
            $l.Add("${Anahtar}: [" + ((@($ogeler) | ForEach-Object { [string]$_ }) -join ', ') + ']')
            return $l.ToArray()
        }
        $l.Add("${Anahtar}:")
        foreach ($o in $ogeler) { $l.Add('  - ' + (ConvertTo-TaskYamlMetin ([string]$o) -ListeOgesi)) }
        return $l.ToArray()
    }
    if ($Deger -is [System.Collections.IDictionary]) { throw "Anahtar '$Anahtar': harita yalniz liste icinde yazilir." }
    $yazi = ConvertTo-TaskYamlMetin ([string]$Deger)
    if ($yazi.Length -eq 0) { $l.Add("${Anahtar}:") } else { $l.Add("${Anahtar}: $yazi") }
    return $l.ToArray()
}

function Get-TaskYeniMetin {
    # Degisiklikleri uygulanmis TAM metni doner (diske yazmaz). Frontmatter disi govde -Govde verilmedikce aynen kalir.
    # Degismeyen anahtarlarin satirlari (yorumlar dahil) korunur; yeni anahtarlar templates/task.md sirasina gore yerlesir.
    # -Zorla: deger ayni olsa da satiri yeniden yazilacak anahtarlar (orn. 'status: done   # yorum' -> 'status: done').
    param([Parameter(Mandatory)]$Task, [System.Collections.IDictionary]$Degerler, [AllowNull()][string]$Govde, [string[]]$Zorla = @())
    if (-not $Task.FrontmatterVar) { throw "frontmatter yok, yazilamaz: $($Task.Path)" }
    $segler = New-Object System.Collections.Generic.List[object]
    foreach ($s in $Task.Segmentler) {
        $y = New-TaskSegment $s.Anahtar
        foreach ($x in $s.Satirlar) { $y.Satirlar.Add($x) }
        $segler.Add($y)
    }
    if ($Degerler) {
        foreach ($k in @($Degerler.Keys)) {
            $k = [string]$k
            $yeni = $Degerler[$k]
            if ($Zorla -notcontains $k -and $Task.Frontmatter.Contains($k) -and (Test-TaskDegerEsit $Task.Frontmatter[$k] $yeni)) { continue }
            $satirlar = @(ConvertTo-TaskDegerSatirlari -Anahtar $k -Deger $yeni)
            $idx = -1
            for ($j = 0; $j -lt $segler.Count; $j++) { if ([string]::Equals($segler[$j].Anahtar, $k, [StringComparison]::Ordinal)) { $idx = $j; break } }
            if ($idx -ge 0) {
                $eski = $segler[$idx].Satirlar
                $kuyruk = New-Object System.Collections.Generic.List[string]
                for ($j = $eski.Count - 1; $j -ge 1; $j--) {
                    if ($eski[$j] -match '^\s*$' -or $eski[$j] -match '^\s*#') { $kuyruk.Insert(0, $eski[$j]) } else { break }
                }
                $yeniSeg = New-TaskSegment $k
                foreach ($x in $satirlar) { $yeniSeg.Satirlar.Add($x) }
                foreach ($x in $kuyruk) { $yeniSeg.Satirlar.Add($x) }
                $segler[$idx] = $yeniSeg
            } else {
                $yeniSeg = New-TaskSegment $k
                foreach ($x in $satirlar) { $yeniSeg.Satirlar.Add($x) }
                $hedef = -1
                $p = [array]::IndexOf($script:TaskAnahtarSirasi, $k)
                if ($p -ge 0) {
                    for ($q = $p - 1; $q -ge 0 -and $hedef -lt 0; $q--) {
                        for ($j = 0; $j -lt $segler.Count; $j++) { if ([string]::Equals($segler[$j].Anahtar, $script:TaskAnahtarSirasi[$q], [StringComparison]::Ordinal)) { $hedef = $j + 1; break } }
                    }
                    for ($q = $p + 1; $q -lt $script:TaskAnahtarSirasi.Count -and $hedef -lt 0; $q++) {
                        for ($j = 0; $j -lt $segler.Count; $j++) { if ([string]::Equals($segler[$j].Anahtar, $script:TaskAnahtarSirasi[$q], [StringComparison]::Ordinal)) { $hedef = $j; break } }
                    }
                }
                if ($hedef -lt 0) { $hedef = $segler.Count }
                $segler.Insert($hedef, $yeniSeg)
            }
        }
    }
    $nl = $Task.SatirSonu
    $hepsi = New-Object System.Collections.Generic.List[string]
    foreach ($s in $segler) { foreach ($x in $s.Satirlar) { $hepsi.Add($x) } }
    $govdeMetni = $Task.Body
    if ($PSBoundParameters.ContainsKey('Govde') -and $null -ne $Govde) { $govdeMetni = $Govde }
    $sonEk = $Task.KapanisSonu
    if (-not $sonEk -and $govdeMetni.Length -gt 0) { $sonEk = $nl }
    $sb = New-Object System.Text.StringBuilder
    [void]$sb.Append($Task.AcilisSatiri).Append($nl)
    if ($hepsi.Count -gt 0) { [void]$sb.Append(($hepsi.ToArray() -join $nl)).Append($nl) }
    [void]$sb.Append($Task.KapanisSatiri).Append($sonEk).Append($govdeMetni)
    return $sb.ToString()
}

function Write-TaskMetni {
    # UTF-8 BOM'suz yazar (kit-lib Write-Utf8NoBom).
    param([Parameter(Mandatory)][string]$Path, [Parameter(Mandatory)][AllowEmptyString()][string]$Metin)
    Write-Utf8NoBom -Path $Path -Text $Metin
}

function Set-TaskFrontmatter {
    # Yalniz frontmatter'i yeniden yazar: anahtar sirasi korunur, bilinmeyen anahtarlar (phase, estimate...) SILINMEZ,
    # govde bayt bayt korunur, orijinal satir sonu (CRLF/LF) korunur, UTF-8 BOM'suz yazilir.
    # Doner: $true = dosya degisti, $false = degisiklik yok (dosyaya dokunulmadi).
    [CmdletBinding()]
    param([string]$Path, $Task, [Parameter(Mandatory)][System.Collections.IDictionary]$Degerler, [string[]]$Zorla = @())
    if (-not $Task) {
        if (-not $Path) { throw 'Set-TaskFrontmatter: -Path ya da -Task gerekli.' }
        $Task = Read-TaskFile -Path $Path
    }
    if (-not $Task.FrontmatterVar) { throw "frontmatter yok: $($Task.Path)" }
    if ($Task.Hatalar.Count -gt 0) { throw "frontmatter hatali, yazilmadi: $($Task.Path): $($Task.Hatalar -join '; ')" }
    $yeni = Get-TaskYeniMetin -Task $Task -Degerler $Degerler -Zorla $Zorla
    if ([string]::Equals($yeni, $Task.Metin, [StringComparison]::Ordinal)) { return $false }
    Write-TaskMetni -Path $Task.Path -Metin $yeni
    return $true
}

# --- Govdedeki NEEDS_ satirlari (goc ve tur-sonu ortak sezgisi) ---------------------------------------------------
# Turkce harf siniflari: desenlerde {I} {S} {C} {G} {U} {O} yazilir; ConvertFrom-TaskTrDesen gercek sinifa cevirir
# (bu dosyada ASCII disi karakter TUTULMAZ). {TIK} = onay isaretleri, {TIRE} = uzun tire, {UCNOKTA} = uc nokta karakteri.
$script:TaskTrHarita = [ordered]@{
    '{I}'       = '[i' + [char]0x131 + 'I' + [char]0x130 + ']'
    '{S}'       = '[s' + [char]0x15F + 'S' + [char]0x15E + ']'
    '{C}'       = '[c' + [char]0xE7 + 'C' + [char]0xC7 + ']'
    '{G}'       = '[g' + [char]0x11F + 'G' + [char]0x11E + ']'
    '{U}'       = '[u' + [char]0xFC + 'U' + [char]0xDC + ']'
    '{O}'       = '[o' + [char]0xF6 + 'O' + [char]0xD6 + ']'
    '{TIK}'     = '[' + [char]0x2713 + [char]0x2714 + [char]0x2705 + ']'
    '{TIRE}'    = '' + [char]0x2014
    '{UCNOKTA}' = '' + [char]0x2026
}

function ConvertFrom-TaskTrDesen {
    param([string]$Desen)
    foreach ($k in $script:TaskTrHarita.Keys) { $Desen = $Desen.Replace([string]$k, [string]$script:TaskTrHarita[$k]) }
    return $Desen
}

# (?i) desenleri kulturden bagimsiz: tr-TR'de 'I'.ToLower() = U+0131 (noktasiz i) oldugundan "BEKLIYOR" ~ "bekliyor" eslesmez.
# Turkce harfler {I} {S} ... siniflariyla acikca yazildigi icin CultureInvariant guvenlidir.
$script:TaskRxSecenek = [System.Text.RegularExpressions.RegexOptions]::CultureInvariant
function New-TaskRegex {
    param([string]$Desen)
    return (New-Object regex ((ConvertFrom-TaskTrDesen $Desen), $script:TaskRxSecenek))
}

# "NEEDS_X:", "NEEDS_X / NEEDS_Y:", "**NEEDS_X:**", "NEEDS_X (RunBinder - supervizor):"
$script:TaskGrupRegex = New-Object regex '(?<![A-Za-z0-9_])NEEDS_(?:SCENE|WIRING|ASSET)(?:\s*`?\s*(?:/|,|\+|&)\s*`?\s*NEEDS_(?:SCENE|WIRING|ASSET))*(?:\*\*)?\s*(?:\([^()\r\n]{0,120}\))?\s*(?:\*\*)?\s*:'
$script:TaskTipRegex = New-Object regex 'NEEDS_(SCENE|WIRING|ASSET)'
# Satirda tek basina kalin etiket (iki noktasiz): "**NEEDS_SCENE**" / "**NEEDS_WIRING (RunBinder - supervizor)**" -> alttaki liste
$script:TaskYalnizEtiketRegex = New-Object regex '^\s*(?:[-*+]\s+)?\*\*NEEDS_(SCENE|WIRING|ASSET)\s*(?:\([^()\r\n]{0,120}\))?\s*\*\*\s*$'
$script:TaskFiil = ConvertFrom-TaskTrDesen '(yaz|yazar|yaz{I}n|yaz{I}l{I}r|yaz{I}lmal{I}|yaz{I}lacak|b{I}rak|b{I}rak{I}r|b{I}rak{I}l{I}r|b{I}rak{I}n|sat{I}r{I}|sat{I}rlar{I}|sat{I}r{I}yla|notu|notuyla|notuna|olarak|ile)'
$script:TaskTalimatRegexleri = @(
    # `NEEDS_X: ...` yaz / birak / satiri / notu / olarak  (ardindan yalniz ':' gelen "satirlari:" basligi haric)
    (New-Object regex (('(?i)`[^`]*NEEDS_(?:SCENE|WIRING|ASSET)[^`]*`\s*(?:\*\*)?\s*' + $script:TaskFiil + '\b(?!\s*:\s*$)'), $script:TaskRxSecenek)),
    # gerekirse / gerekiyorsa ... NEEDS_
    (New-TaskRegex '(?i)\b(gerekirse|gerekiyorsa|gerekliyse|istenirse|laz{I}msa|de{G}i{S}meliyse|de{G}i{S}mesi gerekiyorsa)\b[^.;!?]{0,80}NEEDS_'),
    # yaz: `NEEDS_ ...  /  birak (`NEEDS_ ...
    (New-TaskRegex '(?i)\b(yaz|b{I}rak)\b\s*[:(]?\s*[`*]*NEEDS_'),
    # YAZMA / DOKUNMA / KURMA (`NEEDS_ ...
    (New-Object regex ('(?i)\b(yazma|dokunma|kurma)\b[^.]{0,40}\(\s*[`*]*NEEDS_', $script:TaskRxSecenek)),
    # Bitince / Bitirince ... (teslim talimati)
    (New-TaskRegex '(?i)^\s*(?:[-*+]|\d+[.)])?\s*(?:\*\*)?\s*(bitince|bitirince|bitti{G}inde|sonunda)\b'),
    # kabul kriteri onay kutusu
    (New-Object regex '^\s*[-*+]\s*\[[ xX]\]')
)
$script:TaskAnmaRegex = New-TaskRegex '(?i)(notundaki|notunda|sat{I}rlar{I}nda|sat{I}rlar{I}ndaki|a{S}a{G}{I}daki|yukar{I}daki|bkz\.?)\s*[`*]*NEEDS_'
$script:TaskKapanisRegex = New-TaskRegex '(?i)(\bkapand{I}\b|\bkapat{I}ld{I}\b|\byap{I}ld{I}\b|\bba{G}land{I}\b|\btamam\b|\b{C}{O}z{U}ld{U}\b|\bresolved\b|{TIK})'
$script:TaskKapanisBuyukRegex = New-Object regex '\bDONE\b'
# Sonradan kapanis (ihtiyac satirindan SONRA gelen satirlar; talimat bolumleri haric):
#  - "NEEDS_WIRING uygulandi / kapandi / yapildi" (iki noktasiz anma; kapanis kelimesi tokenden SONRA, 30 karakter icinde,
#    arada parantez/; yok; satirda "kalan/bekliyor/acik" yok) -> o tipteki onceki ihtiyaclar kapanir
#  - "Sahne calisma notu / Sahne turu / Sahne duzeltme turu" (scene-integrator notu; baslik ya da satir basi) -> onceki SCENE kapanir
$script:TaskSonrakiKapanisRegex = New-TaskRegex '(?i)NEEDS_(SCENE|WIRING|ASSET)\b[^;()]{0,30}?(\bkapand{I}\b|\bkapat{I}ld{I}\b|\byap{I}ld{I}\b|\bba{G}land{I}\b|\buyguland{I}\b|\btamamland{I}\b|\b{C}{O}z{U}ld{U}\b|\bba{G}l{I}\b(?!\s+de{G}il)|\bresolved\b|\bDONE\b|{TIK})'
# Cumle siniri (sonradan kapanista "acik/kalan" denetimi yalniz kapanisin gectigi cumleye bakar)
$script:TaskCumleSonuRegex = New-Object regex '[.;!?](?=\s|$)'
$script:TaskAcikKalanRegex = New-TaskRegex '(?i)(\bkalan\b|\bbekliyor\b|\ba{C}{I}k\b|\bopen\b|\bsonra\b|yap{I}lmad{I}|ba{G}lanmad{I}|yap{I}lacak|ba{G}lanacak)'
$script:TaskSahneNotuRegex = New-TaskRegex '(?i)^\s*(?:#{1,6}\s+)?(?:\*\*)?(?:T-\d+[A-Za-z]?\s+)?sahne\s+(?:{C}al{I}{S}ma\s+notu|turu|d{U}zeltme\s+turu)'
$script:TaskYerTutucuRegex = New-Object regex '(?:^|[\s(=:`".''/])<(?![-=\s])[^<>\r\n]{1,60}>'
$script:TaskBaslikRegex = New-Object regex '^(#{1,6})\s+(.*?)\s*#*\s*$'
$script:TaskMaddeRegex = New-Object regex '^(\s*)(?:[-*+]|\d+[.)])\s+(.*)$'
$script:TaskBaslikNeedsRegex = New-TaskRegex '^NEEDS_(SCENE|WIRING|ASSET)\b|(?:{TIRE}|-{1,2}|\+|/)\s*NEEDS_(SCENE|WIRING|ASSET)\s*$'
$script:TaskBaslikTalimatRegex = New-TaskRegex '(?i)\b(yaz|b{I}rak)\b'
$script:TaskSatirlariRegex = New-TaskRegex '(?i)^(sat{I}rlar{I}|listesi|maddeleri)\s*:?$'
$script:TaskUcNoktaRegex = New-TaskRegex '^(\.\.\.|{UCNOKTA})$'

function Test-TaskKapanisIsareti {
    param([AllowNull()][string]$Metin)
    if (-not $Metin) { return $false }
    if ($script:TaskKapanisRegex.IsMatch($Metin)) { return $true }
    if ($script:TaskKapanisBuyukRegex.IsMatch($Metin)) { return $true }
    return $false
}

function Get-TaskBolumTuru {
    # Baslik metni -> 'needs' | 'rapor' | 'talimat' | $null (ustten miras).
    param([string]$Baslik)
    if ($Baslik -cmatch 'NEEDS_(SCENE|WIRING|ASSET)') { return 'needs' }
    $h = (ConvertTo-KitAscii $Baslik).ToLowerInvariant()
    $h = $h -replace '[*`_]', ''
    $h = $h -replace '^[\s\W\d]+', ''
    if ($h -match '^(calisma notu|calisma notlari|supervizor notu|supervisor notu|sonuc|rapor|teslim not|yapilanlar|yapilan|durum|sahne turu|sahne calisma|sahne notu|sahne duzeltme|cozum|bulgu|ozet|degisiklik|kapanis|dogrulama sonuc|test sonuc|notlar|not\b|duzeltme turu|ek not|guncelleme)') { return 'rapor' }
    if ($h -match '^(hedef|amac|kabul|kural|kapsam|istenen|istek|yapilacak|kaynak|is\b|is$|dogrulama|kisit|baglam|tuzak|oku|once oku|yapma|yasak|test|bitince|bitirince|bittiginde|neden|teknik|ortak|sinir|arka plan|davranis|mimari|sozlesme|calisma kural|calisma klasor|oyun gercegi|bugun|kullanici|karar|spec|tasarim|plan|adim|gorev|talimat|onemli|uyari|paralel|sabit api|imza|oncelik|girdi|cikti|teslim\b|teslim$|beklenen|problem|sorun|kok neden|gosterim|donma)') { return 'talimat' }
    return $null
}

function ConvertTo-TaskNotMetni {
    # Ham need metnini temizler: ** kaldir, bastaki/sondaki isaretleri ayikla, bosluk daralt, en fazla 160 karakter.
    param([AllowNull()][string]$Metin)
    if ($null -eq $Metin) { return '' }
    $c = $Metin -replace '\*\*', ''
    $c = ($c -replace '\s+', ' ').Trim()
    $c = $c.TrimStart([char[]]@([char]'*', [char]':', [char]' ', [char]'-', [char]'>'))
    $c = $c.TrimEnd([char[]]@([char]' ', [char]'*'))
    $tik = ([regex]::Matches($c, '`')).Count
    if ($tik % 2 -eq 1) {
        if ($c.EndsWith('`')) { $c = $c.Substring(0, $c.Length - 1).TrimEnd() }
        elseif ($c.StartsWith('`')) { $c = $c.Substring(1).TrimStart() }
    }
    # "(... ; NEEDS_SCENE: X, Y)." -> sondaki eslesmeyen ')' disaridaki parantezin kapanisi: at
    if (([regex]::Matches($c, '\)')).Count -gt ([regex]::Matches($c, '\(')).Count) {
        $mp = [regex]::Match($c, '\)\.?$')
        if ($mp.Success) { $c = ($c.Substring(0, $mp.Index).TrimEnd() + $mp.Value.Substring(1)) }
    }
    if ($c.Length -gt $script:TaskNotSiniri) { $c = $c.Substring(0, $script:TaskNotSiniri - 3).TrimEnd() + '...' }
    return $c
}

function Get-TaskNotAnahtari {
    # Tekrar birlestirme anahtari: tip + kucuk harf + bosluk daraltilmis not.
    param([string]$Tip, [AllowNull()][string]$Not)
    $n = ([string]$Not).ToLowerInvariant() -replace '\s+', ' '
    $n = $n.Trim()
    if ($n.EndsWith('...')) { $n = $n.Substring(0, $n.Length - 3).TrimEnd() }
    return ($Tip.ToUpperInvariant() + '|' + $n)
}

function ConvertTo-TaskNotNormal {
    # Karsilastirma bicimi: YAML kacislari (\" \\) acilir, kucuk harf (invariant), bosluk daraltilir, sondaki "..." atilir.
    param([AllowNull()][string]$Not)
    $n = ([string]$Not).Replace('\"', '"').Replace('\\', '\').ToLowerInvariant()
    $n = ($n -replace '\s+', ' ').Trim()
    if ($n.EndsWith('...')) { $n = $n.Substring(0, $n.Length - 3).TrimEnd() }
    return $n
}

$script:TaskNotEkiRegex = New-Object regex '^(?:\.\.\.)?(?:\s*->.*)?$'
$script:TaskNotKesikRegex = New-Object regex '\.\.\.(?=\s*->|\s*$)'

function Test-TaskAyniNot {
    # Govdeden cikan not ($Yeni) frontmatter'daki notla ($Mevcut) ayni ihtiyac mi? Tip ve durum burada bakilmaz.
    # Kapatan kisi not'un sonuna " -> <commit/aciklama>" ekler (templates/task.md); ilk goc notu 160 karakterde "..."
    # ile kesmis ve YAML'da kacislamis olabilir: "<kesik not>... -> 28.10 kontrol: ..." de ayni ihtiyactir.
    param([AllowNull()][string]$Yeni, [AllowNull()][string]$Mevcut)
    $y = ConvertTo-TaskNotNormal $Yeni
    $m = ConvertTo-TaskNotNormal $Mevcut
    if (-not $y -or -not $m) { return ($y -ceq $m) }
    if ($m -ceq $y) { return $true }
    # mevcut = yeni + ["..."] + [" -> ek"]
    if ($m.StartsWith($y, [StringComparison]::Ordinal) -and $script:TaskNotEkiRegex.IsMatch($m.Substring($y.Length))) { return $true }
    # mevcut baska uzunlukta kesilmis: kesik govdesi (en az 40 karakter) yeni notun basi
    $k = $script:TaskNotKesikRegex.Match($m)
    if ($k.Success -and $k.Index -ge 40) {
        $cekirdek = $m.Substring(0, $k.Index).TrimEnd()
        if ($y.StartsWith($cekirdek, [StringComparison]::Ordinal)) { return $true }
    }
    return $false
}

function Test-TaskOlumsuz {
    # "yok", "YOK", "zorunlu yok", "ek is yok", "gerekmiyor" ...
    param([string]$Metin)
    $a = (ConvertTo-KitAscii $Metin).ToLowerInvariant().Trim()
    # "(yok; RunBinder baglamasi ...)" gibi parantezli olumsuzlar da sayilir
    $a = $a.Trim([char[]]@([char]'`', [char]'*', [char]' ', [char]'.', [char]':', [char]'(', [char]')'))
    if ($a -match '^(yok|none|n/a|gerek yok|gerekmiyor|gerekmez|gerekmedi|zorunlu degil)\b') { return $true }
    $ilk = ($a -split '\s+(?:-|--)\s+|[(;.:]', 2)[0].Trim()
    if ($ilk -match '\byok$' -and $ilk.Length -le 60) { return $true }
    if ($a -match '\bek (is|yapilacak) yok\b') { return $true }
    return $false
}

function Get-TaskYerTutucuDurumu {
    # Doner: $true = sablon/isaretci: yer tutucuyla basliyor, >=2 yer tutucu, yalniz '...',
    #        "(ui-agent doldurur - ...)" gibi doldurulacak alan, ya da "asagida." / "yukaridaki X" (ihtiyacin kendisi
    #        baska satirda; o satir ayrica cikarilir).
    param([string]$Metin)
    $ms = $script:TaskYerTutucuRegex.Matches($Metin)
    if ($ms.Count -ge 2) { return $true }
    if ($ms.Count -eq 1) {
        $bas = $Metin.TrimStart([char[]]@([char]'`', [char]'*', [char]' '))
        if ($bas.StartsWith('<')) { return $true }
    }
    if ($script:TaskUcNoktaRegex.IsMatch($Metin.Trim())) { return $true }
    $a = (ConvertTo-KitAscii $Metin).ToLowerInvariant().Trim().TrimStart([char[]]@([char]'`', [char]'*', [char]' ', [char]':', [char]'-'))
    if ($a -match '^\(\s*[^()]{0,80}\bdoldur\w*') { return $true }
    if ($a -match '^(asagida|asagidaki|yukarida|yukaridaki)\b') { return $true }
    return $false
}

function Get-TaskMaddeMetinleri {
    # $Satirlar[$Bas..$Son] araligindaki ust duzey liste maddeleri (devam satirlariyla). Madde yoksa tek paragraf.
    # Doner: @( @{ Metin; Satir (0-tabanli); Isaretli ([x]) } )
    param([string[]]$Satirlar, [int]$Bas, [int]$Son)
    $sonuc = New-Object System.Collections.Generic.List[object]
    if ($Son -lt $Bas) { return $sonuc.ToArray() }
    $minG = [int]::MaxValue
    for ($i = $Bas; $i -le $Son; $i++) {
        $m = $script:TaskMaddeRegex.Match($Satirlar[$i])
        if ($m.Success -and $m.Groups[1].Value.Length -lt $minG) { $minG = $m.Groups[1].Value.Length }
    }
    if ($minG -eq [int]::MaxValue) {
        $parca = New-Object System.Collections.Generic.List[string]
        $ilk = -1
        for ($i = $Bas; $i -le $Son; $i++) {
            $t = $Satirlar[$i].Trim()
            if ($t) { if ($ilk -lt 0) { $ilk = $i }; $parca.Add($t) }
            elseif ($parca.Count -gt 0) { break }
        }
        if ($parca.Count -gt 0) { $sonuc.Add(@{ Metin = ($parca -join ' '); Satir = $ilk; Isaretli = $false }) }
        return $sonuc.ToArray()
    }
    $aktif = $null
    for ($i = $Bas; $i -le $Son; $i++) {
        $s = $Satirlar[$i]
        $m = $script:TaskMaddeRegex.Match($s)
        if ($m.Success -and $m.Groups[1].Value.Length -le $minG) {
            if ($aktif) { $sonuc.Add($aktif) }
            $govde = $m.Groups[2].Value
            $isaretli = $false
            $mk = [regex]::Match($govde, '^\[([ xX])\]\s*(.*)$')
            if ($mk.Success) { $isaretli = ($mk.Groups[1].Value -ne ' '); $govde = $mk.Groups[2].Value }
            $aktif = @{ Metin = $govde.Trim(); Satir = $i; Isaretli = $isaretli }
        } elseif ($aktif -and $s.Trim()) {
            $aktif.Metin = $aktif.Metin + ' ' + $s.Trim()
        }
    }
    if ($aktif) { $sonuc.Add($aktif) }
    return $sonuc.ToArray()
}

function Get-TaskGovdeNeeds {
    # Govdedeki somut NEEDS_(SCENE|WIRING|ASSET) ihtiyaclarini cikarir (govdeyi DEGISTIRMEZ).
    # Bicimler: satir ("NEEDS_X: metin", "**NEEDS_X:** metin", "`NEEDS_X:` metin", "NEEDS_X:" + alttaki liste)
    #           ve baslik ("## NEEDS_X (...)" altindaki maddeler; giris paragrafi varsa bolum TEK ihtiyac).
    # Atlananlar: yer tutucular (<sistem>), olumsuzlar (yok/YOK/zorunlu yok), duzyazi anmalari (notundaki NEEDS_...),
    #             talimat/sablon satirlari (`NEEDS_X: ...` yaz, gerekirse NEEDS_...), talimat bolumleri (Hedef, Kabul
    #             kriterleri, Kurallar, Kapsam...), HTML yorumlari.
    # Kapanis: satirda ya da sonraki satirda kapanis isareti, ~~...~~, [x]; sonradan "NEEDS_X uygulandi" ya da
    #          scene-integrator "Sahne calisma notu / Sahne turu" (yalniz onceki SCENE).
    # Doner: @( [pscustomobject]@{ Tip; Not; Kapali; Satir (govde, 1-tabanli); Bicim } ) - tekrarlar birlesmis.
    param([AllowNull()][string]$Govde)
    $ham = New-Object System.Collections.Generic.List[object]
    if ([string]::IsNullOrEmpty($Govde)) { return @() }
    $satirlar = [regex]::Split($Govde, '\r?\n')
    $n = $satirlar.Length

    # 1) her satirin bolum turu + HTML yorumu maskesi
    $tur = New-Object 'string[]' $n
    $yorumda = New-Object 'bool[]' $n
    $seviyeTur = @{}
    $yorumIcinde = $false
    $baslikSatirlari = New-Object System.Collections.Generic.List[int]
    $etkin = $null
    for ($i = 0; $i -lt $n; $i++) {
        $s = $satirlar[$i]
        if ($yorumIcinde) {
            $yorumda[$i] = $true
            if ($s.Contains('-->')) { $yorumIcinde = $false }
            $tur[$i] = $etkin
            continue
        }
        $ac = $s.IndexOf('<!--')
        if ($ac -ge 0) {
            $kap = $s.IndexOf('-->', $ac + 4)
            if ($kap -lt 0) { $yorumIcinde = $true; $yorumda[$i] = ($ac -eq 0 -or $s.Substring(0, $ac).Trim() -eq '') }
        }
        $mb = $script:TaskBaslikRegex.Match($s)
        if ($mb.Success) {
            $sev = $mb.Groups[1].Value.Length
            foreach ($k in @($seviyeTur.Keys)) { if ($k -ge $sev) { $seviyeTur.Remove($k) } }
            $seviyeTur[$sev] = Get-TaskBolumTuru $mb.Groups[2].Value
            $etkin = $null
            for ($l = $sev; $l -ge 1; $l--) { if ($seviyeTur.ContainsKey($l) -and $seviyeTur[$l]) { $etkin = $seviyeTur[$l]; break } }
            $baslikSatirlari.Add($i)
        }
        $tur[$i] = $etkin
    }

    # 2) baslik bicimi: "## NEEDS_SCENE (...)" altindaki maddeler
    foreach ($bi in $baslikSatirlari) {
        if ($yorumda[$bi]) { continue }
        $mb = $script:TaskBaslikRegex.Match($satirlar[$bi])
        $metin = $mb.Groups[2].Value
        $sade = ($metin -replace '[*`]', '') -replace '^\s*\(?\d+[.)]\s*', ''
        $mt = $script:TaskBaslikNeedsRegex.Match($sade)
        if (-not $mt.Success) { continue }
        $tip = $mt.Groups[1].Value
        if (-not $tip) { $tip = $mt.Groups[2].Value }
        if (Test-TaskOlumsuz (($sade -replace '^.*?NEEDS_(SCENE|WIRING|ASSET)', ''))) { continue }
        if ($script:TaskBaslikTalimatRegex.IsMatch($sade)) { continue }
        $sev = $mb.Groups[1].Value.Length
        $son = $n - 1
        for ($j = $bi + 1; $j -lt $n; $j++) {
            $mj = $script:TaskBaslikRegex.Match($satirlar[$j])
            if ($mj.Success -and $mj.Groups[1].Value.Length -le $sev) { $son = $j - 1; break }
        }
        $grupVar = $false
        for ($j = $bi + 1; $j -le $son; $j++) { if ($script:TaskGrupRegex.IsMatch($satirlar[$j])) { $grupVar = $true; break } }
        if ($grupVar) { continue }
        $baslikKapali = Test-TaskKapanisIsareti $metin
        $ilkDolu = -1
        for ($j = $bi + 1; $j -le $son; $j++) { if (-not $yorumda[$j] -and $satirlar[$j].Trim()) { $ilkDolu = $j; break } }
        if ($ilkDolu -lt 0) { continue }
        if (-not $script:TaskMaddeRegex.IsMatch($satirlar[$ilkDolu])) {
            # giris paragrafi var ("Dosya: X" + maddeler): bolumun tamami tek ihtiyac
            $parca = New-Object System.Collections.Generic.List[string]
            $uzunluk = 0
            for ($j = $ilkDolu; $j -le $son -and $uzunluk -le ($script:TaskNotSiniri + 40); $j++) {
                $t = $satirlar[$j].Trim()
                if ($t -and -not $script:TaskBaslikRegex.IsMatch($satirlar[$j])) { $parca.Add($t); $uzunluk += $t.Length + 1 }
            }
            $ham1 = $parca -join ' '
            $not = ConvertTo-TaskNotMetni $ham1
            if (-not $not -or (Test-TaskOlumsuz $not) -or (Get-TaskYerTutucuDurumu $ham1)) { continue }
            $kapali = $baslikKapali -or (Test-TaskKapanisIsareti $satirlar[$ilkDolu])
            $ham.Add([pscustomobject]@{ Tip = $tip; Not = $not; Kapali = [bool]$kapali; Satir = $ilkDolu + 1; Bicim = 'baslik' })
            continue
        }
        foreach ($md in (Get-TaskMaddeMetinleri -Satirlar $satirlar -Bas ($bi + 1) -Son $son)) {
            if ($yorumda[$md.Satir]) { continue }
            $not = ConvertTo-TaskNotMetni $md.Metin
            if (-not $not -or $not -match '^[\W_]*$') { continue }
            if (Test-TaskOlumsuz $not) { continue }
            if (Get-TaskYerTutucuDurumu $md.Metin) { continue }
            $kapali = $baslikKapali -or $md.Isaretli -or (Test-TaskKapanisIsareti $md.Metin) -or ($satirlar[$md.Satir] -match '~~')
            $ham.Add([pscustomobject]@{ Tip = $tip; Not = $not; Kapali = [bool]$kapali; Satir = $md.Satir + 1; Bicim = 'baslik' })
        }
    }

    # 3) satir bicimi: NEEDS_X: <somut metin>
    for ($i = 0; $i -lt $n; $i++) {
        $s = $satirlar[$i]
        if ($yorumda[$i]) { continue }
        if ($tur[$i] -eq 'talimat') { continue }
        if ($script:TaskBaslikRegex.IsMatch($s)) { continue }
        $me = $script:TaskYalnizEtiketRegex.Match($s)
        if ($me.Success) {
            # kalin etiket: alttaki liste (bos satira, basliga ya da yeni NEEDS_ satirina kadar) ihtiyaclarin kendisi
            $j = $i + 1
            if ($j -lt $n -and $satirlar[$j].Trim() -eq '') { $j++ }
            $ilk = $j
            $sonJ = $j - 1
            while ($j -lt $n) {
                $t = $satirlar[$j]
                if ($t.Trim() -eq '' -or $t.TrimStart().StartsWith('```') -or $script:TaskBaslikRegex.IsMatch($t) -or $script:TaskYalnizEtiketRegex.IsMatch($t)) { break }
                # yeni NEEDS_ satiri listeyi bitirir; ilk satir zaten NEEDS_ ise etiketin kendi listesi yoktur
                if ($script:TaskGrupRegex.IsMatch($t)) { break }
                $sonJ = $j
                $j++
            }
            if ($sonJ -lt $ilk) { continue }
            $etiketKapali = Test-TaskKapanisIsareti $s
            foreach ($md in (Get-TaskMaddeMetinleri -Satirlar $satirlar -Bas $ilk -Son $sonJ)) {
                $nm = ConvertTo-TaskNotMetni $md.Metin
                if (-not $nm -or (Test-TaskOlumsuz $nm) -or (Get-TaskYerTutucuDurumu $md.Metin)) { continue }
                $kp = $etiketKapali -or $md.Isaretli -or (Test-TaskKapanisIsareti $md.Metin) -or ($satirlar[$md.Satir] -match '~~')
                $ham.Add([pscustomobject]@{ Tip = $me.Groups[1].Value; Not = $nm; Kapali = [bool]$kp; Satir = $md.Satir + 1; Bicim = 'etiket' })
            }
            continue
        }
        $gruplar = $script:TaskGrupRegex.Matches($s)
        if ($gruplar.Count -eq 0) { continue }
        $talimat = $false
        foreach ($rx in $script:TaskTalimatRegexleri) { if ($rx.IsMatch($s)) { $talimat = $true; break } }
        if ($talimat) { continue }
        if ($script:TaskAnmaRegex.IsMatch($s)) { continue }
        $mi = $script:TaskMaddeRegex.Match($s)
        $maddeMi = $mi.Success
        $girinti = 0
        if ($maddeMi) { $girinti = $mi.Groups[1].Value.Length }
        $satirKapali = (Test-TaskKapanisIsareti $s)
        if (-not $satirKapali -and ($i + 1) -lt $n -and -not $script:TaskGrupRegex.IsMatch($satirlar[$i + 1])) {
            # sonraki satir yalniz devam satiriysa sayilir (kardes liste maddesinin "YAPILDI"si bu ihtiyaci kapatmaz)
            $mn = $script:TaskMaddeRegex.Match($satirlar[$i + 1])
            $devam = $true
            if ($mn.Success) { $devam = ($maddeMi -and $mn.Groups[1].Value.Length -gt $girinti) }
            if ($devam) { $satirKapali = Test-TaskKapanisIsareti $satirlar[$i + 1] }
        }
        for ($g = 0; $g -lt $gruplar.Count; $g++) {
            $m = $gruplar[$g]
            $tipler = @($script:TaskTipRegex.Matches($m.Value) | ForEach-Object { $_.Groups[1].Value } | Select-Object -Unique)
            $bas = $m.Index + $m.Length
            $sonraki = $s.Length
            if (($g + 1) -lt $gruplar.Count) { $sonraki = $gruplar[$g + 1].Index }
            $icerik = $s.Substring($bas, $sonraki - $bas)
            $kuyruk = ''
            if ($m.Index -gt 0 -and $s[$m.Index - 1] -ceq [char]'`') {
                $kap = $s.IndexOf('`', $bas)
                if ($kap -ge 0 -and $kap -lt $sonraki) {
                    $ic = $s.Substring($bas, $kap - $bas)
                    $kuyruk = $s.Substring($kap + 1, $sonraki - $kap - 1)
                    if ($ic.Trim() -eq '') { $icerik = $kuyruk; $kuyruk = '' } else { $icerik = $ic }
                }
            }
            if ($kuyruk -and [regex]::IsMatch($kuyruk, ('(?i)^\s*(?:\*\*)?\s*' + $script:TaskFiil + '\b'), $script:TaskRxSecenek)) { continue }
            # "`NEEDS_WIRING: analytics` YOK." -> tirnak disindaki olumsuz, ihtiyaci iptal eder
            if ($kuyruk -and $kuyruk.Trim() -and (Test-TaskOlumsuz $kuyruk)) { continue }
            $strike = $false
            $ss = $s.LastIndexOf('~~', [Math]::Max(0, $m.Index))
            if ($m.Index -gt 0 -and $ss -ge 0 -and $ss -lt $m.Index -and $s.IndexOf('~~', $m.Index) -gt $m.Index) { $strike = $true }
            $temiz = ConvertTo-TaskNotMetni $icerik
            $kapali = $satirKapali -or $strike
            $notlar = New-Object System.Collections.Generic.List[object]
            $baslikMi = ((-not $temiz) -or ($temiz -match '^[\W_]*$') -or $script:TaskSatirlariRegex.IsMatch($temiz))
            if ($baslikMi) {
                # "NEEDS_X:" tek basina: ardindaki satirlar (liste ya da paragraf) ihtiyacin kendisi
                $j = $i + 1
                if ($j -lt $n -and $satirlar[$j].Trim() -eq '') { $j++ }
                $ilk = $j
                $sonJ = $j - 1
                while ($j -lt $n) {
                    $t = $satirlar[$j]
                    if ($t.Trim() -eq '' -or $t.TrimStart().StartsWith('```')) { break }
                    if ($script:TaskBaslikRegex.IsMatch($t)) { break }
                    # yeni NEEDS_ satiri listeyi bitirir (o satir kendi basina islenir); onceki maddeler GECERLI kalir
                    # ("NEEDS_SCENE:" + liste + hemen ardindan "NEEDS_WIRING: yok" kalibi)
                    if ($script:TaskGrupRegex.IsMatch($t)) { break }
                    $sonJ = $j
                    $j++
                }
                if ($sonJ -lt $ilk) { continue }
                foreach ($md in (Get-TaskMaddeMetinleri -Satirlar $satirlar -Bas $ilk -Son $sonJ)) {
                    $nm = ConvertTo-TaskNotMetni $md.Metin
                    if (-not $nm -or (Test-TaskOlumsuz $nm) -or (Get-TaskYerTutucuDurumu $md.Metin)) { continue }
                    $notlar.Add(@{ Not = $nm; Kapali = ($kapali -or $md.Isaretli -or (Test-TaskKapanisIsareti $md.Metin)); Satir = $md.Satir + 1 })
                }
            } else {
                if (Test-TaskOlumsuz $temiz) { continue }
                if (Get-TaskYerTutucuDurumu $icerik) { continue }
                $not = $temiz
                if ($g -eq ($gruplar.Count - 1)) {
                    # sarilmis devam satirlarini ekle: madde ise daha girintili satirlar; degilse duz satirlar
                    # (':' ile bitiyorsa alttaki liste de). Bos satir / baslik / yeni NEEDS_ / kardes maddede durur.
                    $ikiNokta = $not.EndsWith(':')
                    $ek = New-Object System.Collections.Generic.List[string]
                    $ekUz = 0
                    for ($j = $i + 1; $j -lt $n -and ($not.Length + $ekUz) -lt $script:TaskNotSiniri; $j++) {
                        $t = $satirlar[$j]
                        if ($t.Trim() -eq '' -or $t.TrimStart().StartsWith('```')) { break }
                        if ($script:TaskBaslikRegex.IsMatch($t) -or $script:TaskGrupRegex.IsMatch($t)) { break }
                        $tg = $t.Length - $t.TrimStart().Length
                        $mj = $script:TaskMaddeRegex.Match($t)
                        if ($maddeMi) { if ($tg -le $girinti) { break } }
                        elseif ($mj.Success -and -not $ikiNokta) { break }
                        $ek.Add($t.Trim())
                        $ekUz += $t.Trim().Length + 1
                    }
                    if ($ek.Count -gt 0) { $not = ConvertTo-TaskNotMetni ($not + ' ' + ($ek -join ' ')) }
                }
                $notlar.Add(@{ Not = $not; Kapali = $kapali; Satir = $i + 1 })
            }
            foreach ($tp in $tipler) {
                foreach ($nn in $notlar) {
                    $ham.Add([pscustomobject]@{ Tip = $tp; Not = $nn.Not; Kapali = [bool]$nn.Kapali; Satir = $nn.Satir; Bicim = 'satir' })
                }
            }
        }
    }

    # 4) sonradan kapanis
    if ($ham.Count -gt 0) {
        for ($i = 0; $i -lt $n; $i++) {
            if ($yorumda[$i] -or $tur[$i] -eq 'talimat') { continue }
            $s = $satirlar[$i]
            $kapatilan = New-Object System.Collections.Generic.List[string]
            if ($script:TaskSahneNotuRegex.IsMatch($s)) { $kapatilan.Add('SCENE') }
            if (-not $script:TaskGrupRegex.IsMatch($s)) {
                foreach ($mm in $script:TaskSonrakiKapanisRegex.Matches($s)) {
                    # "NEEDS_WIRING kapandi - ... . Acik: DoubleShift ..." -> "acik" baska cumlede, kapanisi bozmaz
                    $cBas = 0
                    foreach ($cs in $script:TaskCumleSonuRegex.Matches($s.Substring(0, $mm.Index))) { $cBas = $cs.Index + 1 }
                    $cSon = $s.Length
                    $sonra = $script:TaskCumleSonuRegex.Match($s, $mm.Index + $mm.Length)
                    if ($sonra.Success) { $cSon = $sonra.Index }
                    if ($script:TaskAcikKalanRegex.IsMatch($s.Substring($cBas, $cSon - $cBas))) { continue }
                    $kapatilan.Add($mm.Groups[1].Value.ToUpperInvariant())
                }
            }
            if ($kapatilan.Count -eq 0) { continue }
            foreach ($h in $ham) { if ($h.Satir -lt ($i + 1) -and $kapatilan.Contains($h.Tip)) { $h.Kapali = $true } }
        }
    }

    # 5) tekrarlar birlesir (kapali olan kazanir), satir sirasina gore
    $sirali = @($ham | Sort-Object Satir)
    $harita = [ordered]@{}
    foreach ($h in $sirali) {
        $k = Get-TaskNotAnahtari -Tip $h.Tip -Not $h.Not
        if ($harita.Contains($k)) { if ($h.Kapali) { $harita[$k].Kapali = $true } }
        else { $harita[$k] = $h }
    }
    return @($harita.Values)
}
