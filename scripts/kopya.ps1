<#
.SYNOPSIS
    Dogrulama kopya havuzu: kopya al / birak / listele / senkronla / dogrula / geri al.
.DESCRIPTION
    Havuz: <KopyaKoku>\<KopyaOnEki>-1 .. -<KopyaSayisi>  (agent-kit.config.psd1).
    Kilit: <kopya>\.kopya-kilit.json {sahip, not, alindi, makine}. Kilit dosyasi atomik (CreateNew)
    olusturulur: ayni anda iki oturum ayni kopyayi alamaz. 6 saatten eski kilit 'bayat-kilit' gorunur
    ama kendiliginden ALINMAZ (sahibi hala calisiyor olabilir); -Birak -Ad ile elle birakilir.

    Neden (bmbp 21-23.10.2026): kopyalar elle ve sabit yollu script'lerle yonetiliyordu; kimin hangisini
    kullandigi belli degildi. Bir kopya ana projeyle ayni productName'i tasidigi icin kullanicinin kaydini
    ezdi; bir geri alma, sync'ten sonra worktree'de degisen .cs'leri kopyadaki eski halleriyle ezdi.

    Komutlar (tam olarak biri):
      -Liste [-Json]
      -Al [-Sahip <ad>] [-Not <metin>]                       son satir = kopya yolu
      -Birak -Ad <kopya> [-Sahip <ad>]
      -Birak -Hepsi -Sahip <ad>
      -Senkron -Ad <kopya> [-Kaynak <worktree>] [-HeadOnly] [-Sahip <ad>]
      -Dogrula -Ad <kopya> [-TestFilter <regex>] [-CompileOnly] [-Sahip <ad>]
      -GeriAl  -Ad <kopya> -Hedef <worktree> -Yollar <yol,yol,...> [-Onayla] [-KodDahil]

    -Ad    : kopya adi (bmbp-verify-2), havuz numarasi (2) ya da KopyaKoku altindaki tam yol.
    -Root  : proje (config'in okundugu worktree). Vars.: bu script'in bulundugu deponun koku.
    -Kaynak: Senkron kaynagi. Vars.: -Root.
    -Yollar: kopyaya gore goreli yol / klasor / son parcada joker (Assets/Game/UI/*.prefab). Dosya ve
             klasorlerin .meta'si otomatik eklenir. Ilk parca KopyaKlasorleri'nden biri olmali.

    Cikis kodlari:
      0      tamam (Dogrula: yesil)
      1-3    Dogrula: kirmizi        (dogrulama script'inin kodu aynen)
      10-19  Dogrula: dogrulanamadi  (dogrulama script'inin kodu aynen). kopya.ps1'in kendi engelleri:
        13   kopyanin productName'ine KopyaUrunEki konamadi / kopyada yok (kayit testleri ana kaydi ezer)
        14   GeriAl listesinde .cs var ve -KodDahil yok
        15   hedef ana worktree'nin icinde / kopya bir worktree'nin icinde
      20     havuz dolu (Al) - kilit sahipleri listelenir
      21     kopya kilitli degil ya da kilit baska sahibin
      22     kullanim / config hatasi
      23     robocopy / git hatasi
      24     beklenmeyen script hatasi
.EXAMPLE
    $k = (powershell -File scripts\kopya.ps1 -Al -Sahip T-455 -Not "tam suite")[-1]
    powershell -File scripts\kopya.ps1 -Senkron -Ad $k -HeadOnly
    powershell -File scripts\kopya.ps1 -Dogrula -Ad $k -TestFilter "T337|T388"
    powershell -File scripts\kopya.ps1 -Birak -Ad $k
#>
param(
    [switch]$Liste,
    [switch]$Json,
    [switch]$Al,
    [switch]$Birak,
    [switch]$Hepsi,
    [switch]$Senkron,
    [switch]$HeadOnly,
    [switch]$Dogrula,
    [switch]$CompileOnly,
    [switch]$GeriAl,
    [switch]$Onayla,
    [switch]$KodDahil,
    [string]$Ad,
    [string]$Sahip,
    [string]$Not,
    [string]$Kaynak,
    [string]$TestFilter,
    [string]$Hedef,
    [string[]]$Yollar,
    [string]$Root,
    [double]$BayatSaat = 6
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'kit-lib.ps1')

$script:KilitAdi = '.kopya-kilit.json'

# ------------------------------------------------------------------ yardimcilar

function Write-Bilgi {
    param([string]$Metin)
    # -Json modunda stdout yalniz JSON tasir (tur-sonu.ps1 okur).
    if ($Json) { [Console]::Error.WriteLine($Metin) } else { Write-Host $Metin }
}

function Stop-Kopya {
    param([int]$Kod, [string]$Mesaj)
    Write-Bilgi "HATA: $Mesaj"
    exit $Kod
}

function Get-TamYol {
    param([string]$Yol)
    if (-not [System.IO.Path]::IsPathRooted($Yol)) { $Yol = Join-Path (Get-Location).Path $Yol }
    $tam = [System.IO.Path]::GetFullPath($Yol)
    if ($tam.Length -gt 3) { $tam = $tam.TrimEnd('\') }
    return $tam
}

function Test-Icinde {
    # $Yol, $Kok'un kendisi ya da altinda mi (buyuk/kucuk harf duyarsiz).
    param([string]$Yol, [string]$Kok)
    $y = (Get-TamYol $Yol).ToLowerInvariant()
    $k = (Get-TamYol $Kok).ToLowerInvariant().TrimEnd('\')
    return ($y -eq $k -or $y.StartsWith($k + '\'))
}

function ConvertTo-KomutArg {
    # Windows komut satiri kurallari (CommandLineToArgvW): bosluk/tirnak varsa tirnakla.
    param([string]$A)
    if ($A -eq '') { return '""' }
    if ($A -notmatch '[\s"]') { return $A }
    $sb = New-Object System.Text.StringBuilder
    [void]$sb.Append('"')
    $ters = 0
    foreach ($ch in $A.ToCharArray()) {
        if ($ch -eq [char]'\') { $ters++; continue }
        if ($ch -eq [char]'"') {
            [void]$sb.Append([string]::new([char]'\', $ters * 2 + 1)); [void]$sb.Append('"'); $ters = 0; continue
        }
        if ($ters -gt 0) { [void]$sb.Append([string]::new([char]'\', $ters)); $ters = 0 }
        [void]$sb.Append($ch)
    }
    if ($ters -gt 0) { [void]$sb.Append([string]::new([char]'\', $ters * 2)) }
    [void]$sb.Append('"')
    return $sb.ToString()
}

function Invoke-Git {
    # git'i Process ile cagirir: stdout UTF-8 (konsol kod sayfasi yok), stderr ayri, istenirse stdout
    # BAYT BAYT dosyaya (PowerShell yonlendirmesi kodlama ekler). LC_ALL=C: hata metni Ingilizce kalir.
    param([string]$Dizin, [string[]]$Arg, [string]$CiktiDosyasi)
    $psi = New-Object System.Diagnostics.ProcessStartInfo
    $psi.FileName = $script:Git
    $hepsi = @('-C', $Dizin) + $Arg
    $psi.Arguments = (($hepsi | ForEach-Object { ConvertTo-KomutArg $_ }) -join ' ')
    $psi.UseShellExecute = $false
    $psi.CreateNoWindow = $true
    $psi.RedirectStandardOutput = $true
    $psi.RedirectStandardError = $true
    $psi.EnvironmentVariables['LC_ALL'] = 'C'
    $utf8 = New-Object System.Text.UTF8Encoding($false)
    if (-not $CiktiDosyasi) { $psi.StandardOutputEncoding = $utf8 }
    $psi.StandardErrorEncoding = $utf8
    $p = [System.Diagnostics.Process]::Start($psi)
    $hataGorevi = $p.StandardError.ReadToEndAsync()
    $cikti = ''
    if ($CiktiDosyasi) {
        $fs = [System.IO.File]::Create($CiktiDosyasi)
        try { $p.StandardOutput.BaseStream.CopyTo($fs) } finally { $fs.Dispose() }
    } else {
        $cikti = $p.StandardOutput.ReadToEnd()
    }
    $p.WaitForExit()
    $sonuc = [pscustomobject]@{ Kod = $p.ExitCode; Cikti = $cikti; Hata = $hataGorevi.Result }
    $p.Dispose()
    return $sonuc
}

function Get-Worktreeler {
    $r = Invoke-Git $script:RootYol @('worktree', 'list', '--porcelain')
    if ($r.Kod -ne 0) { return @() }
    $liste = @()
    foreach ($s in ($r.Cikti -split "`n")) {
        if ($s.StartsWith('worktree ')) { $liste += (Get-TamYol (($s.Substring(9).Trim()) -replace '/', '\')) }
    }
    return $liste
}

function Assert-KopyaGuvenli {
    # Kopya hicbir worktree'nin icinde olamaz (Unity onu projenin parcasi sanip ice aktarir; senkron
    # /MIR ile worktree'yi siler) ve hicbir worktree kopyanin icinde olamaz.
    param([string]$Yol)
    if (Test-Icinde $Yol $script:Ana) { Stop-Kopya 15 "kopya ana worktree'nin icinde: $Yol (ana: $($script:Ana))" }
    foreach ($wt in $script:Worktreeler) {
        if ((Test-Icinde $Yol $wt) -or (Test-Icinde $wt $Yol)) { Stop-Kopya 15 "kopya bir worktree ile ic ice: $Yol <-> $wt" }
    }
}

function Get-UrunAdi {
    # Kopyanin productName'i (tirnaksiz). Dosya yoksa $null, satir yoksa ''.
    param([string]$KopyaYol)
    $ayar = Join-Path $KopyaYol 'ProjectSettings\ProjectSettings.asset'
    if (-not (Test-Path -LiteralPath $ayar)) { return $null }
    $m = [regex]::Match([System.IO.File]::ReadAllText($ayar), '(?m)^[ \t]*productName:[ \t]*(.*?)\r?$')
    if (-not $m.Success) { return '' }
    return (ConvertFrom-YamlDeger $m.Groups[1].Value).Ic
}

function ConvertFrom-YamlDeger {
    param([string]$Ham)
    $v = $Ham.Trim()
    if ($v.Length -ge 2 -and (($v[0] -eq [char]"'" -and $v[-1] -eq [char]"'") -or ($v[0] -eq [char]'"' -and $v[-1] -eq [char]'"'))) {
        return @{ Tirnak = [string]$v[0]; Ic = $v.Substring(1, $v.Length - 2) }
    }
    return @{ Tirnak = ''; Ic = $v }
}

function Read-Kilit {
    param([string]$KopyaYol)
    $f = Join-Path $KopyaYol $script:KilitAdi
    if (-not (Test-Path -LiteralPath $f)) { return $null }
    $o = $null
    try { $o = [System.IO.File]::ReadAllText($f) | ConvertFrom-Json } catch { $o = $null }
    $alindi = $null
    if ($o -and $o.alindi) {
        if ($o.alindi -is [datetime]) { $alindi = $o.alindi }
        else {
            $t = [datetime]::MinValue
            if ([datetime]::TryParseExact([string]$o.alindi, 's', [System.Globalization.CultureInfo]::InvariantCulture, [System.Globalization.DateTimeStyles]::None, [ref]$t)) { $alindi = $t }
            elseif ([datetime]::TryParse([string]$o.alindi, [ref]$t)) { $alindi = $t }
        }
    }
    if (-not $alindi) { $alindi = (Get-Item -LiteralPath $f).LastWriteTime }
    $sahipAd = '?'; $notMetin = ''; $makine = ''
    if ($o) { $sahipAd = [string]$o.sahip; $notMetin = [string]$o.not; $makine = [string]$o.makine }
    return [pscustomobject]@{
        Sahip     = $sahipAd
        Not       = $notMetin
        Makine    = $makine
        Alindi    = $alindi.ToString('s')
        YasDakika = [int][math]::Floor(((Get-Date) - $alindi).TotalMinutes)
    }
}

function New-Kilit {
    # Atomik: dosya zaten varsa $false (baskasi aldi).
    param([string]$KopyaYol, [string]$SahipAd, [string]$NotMetin)
    $f = Join-Path $KopyaYol $script:KilitAdi
    $icerik = [ordered]@{
        sahip  = $SahipAd
        not    = $NotMetin
        alindi = (Get-Date).ToString('s')
        makine = $env:COMPUTERNAME
    } | ConvertTo-Json
    $fs = $null
    try {
        $fs = [System.IO.File]::Open($f, [System.IO.FileMode]::CreateNew, [System.IO.FileAccess]::Write, [System.IO.FileShare]::None)
    } catch {
        if (Test-Path -LiteralPath $f) { return $false }
        throw
    }
    try {
        $b = (New-Object System.Text.UTF8Encoding($false)).GetBytes($icerik)
        $fs.Write($b, 0, $b.Length)
    } finally { $fs.Dispose() }
    return $true
}

function Get-KopyaDurumu {
    param([string]$KopyaAd, [string]$Yol, [bool]$Havuz)
    $var = Test-Path -LiteralPath $Yol -PathType Container
    $kilit = $null
    if ($var) { $kilit = Read-Kilit $Yol }
    if (-not $var) { $durum = 'yok' }
    elseif (-not $Havuz) { $durum = 'eski' }
    elseif ($kilit) {
        if ($kilit.YasDakika -gt ($BayatSaat * 60)) { $durum = 'bayat-kilit' } else { $durum = 'kilitli' }
    } else { $durum = 'bos' }
    $urun = $null
    if ($var) { $urun = Get-UrunAdi $Yol }
    $urunVerify = $null
    if ($null -ne $urun) { $urunVerify = ($urun.Length -gt 0 -and $urun.EndsWith($script:Eki)) }
    $k = @{ Sahip = ''; YasDakika = $null; Not = ''; Alindi = ''; Makine = '' }
    if ($kilit) { $k = @{ Sahip = $kilit.Sahip; YasDakika = $kilit.YasDakika; Not = $kilit.Not; Alindi = $kilit.Alindi; Makine = $kilit.Makine } }
    return [pscustomobject]@{
        Ad         = $KopyaAd
        Yol        = $Yol
        Durum      = $durum
        Sahip      = $k.Sahip
        YasDakika  = $k.YasDakika
        UrunVerify = $urunVerify
        UrunAdi    = $urun
        Not        = $k.Not
        Alindi     = $k.Alindi
        Makine     = $k.Makine
        Havuz      = $Havuz
    }
}

function Get-HavuzAdlari {
    $adlar = @()
    for ($i = 1; $i -le $script:Sayi; $i++) { $adlar += "$($script:OnEk)-$i" }
    return $adlar
}

function Get-Havuz {
    $sonuc = @()
    $adlar = Get-HavuzAdlari
    foreach ($a in $adlar) { $sonuc += Get-KopyaDurumu $a (Join-Path $script:Koku $a) $true }
    if (Test-Path -LiteralPath $script:Koku) {
        $desen = [System.Management.Automation.WildcardPattern]::Escape($script:OnEk) + '*'
        $eskiler = Get-ChildItem -LiteralPath $script:Koku -Directory -Force |
            Where-Object { $_.Name -like $desen -and $adlar -notcontains $_.Name } |
            Where-Object { $y = $_.FullName; -not ($script:Worktreeler | Where-Object { Test-Icinde $_ $y }) } |
            Sort-Object Name
        foreach ($d in $eskiler) { $sonuc += Get-KopyaDurumu $d.Name $d.FullName $false }
    }
    return $sonuc
}

function Resolve-Kopya {
    param([string]$AdVeyaYol)
    if ([string]::IsNullOrWhiteSpace($AdVeyaYol)) { Stop-Kopya 22 '-Ad gerekli (kopya adi, numarasi ya da yolu).' }
    if ($AdVeyaYol -match '^\d+$') { $yol = Join-Path $script:Koku "$($script:OnEk)-$AdVeyaYol" }
    elseif ([System.IO.Path]::IsPathRooted($AdVeyaYol)) { $yol = $AdVeyaYol }
    else { $yol = Join-Path $script:Koku $AdVeyaYol }
    $yol = Get-TamYol $yol
    $kopyaAd = Split-Path $yol -Leaf
    $ust = Get-TamYol (Split-Path $yol -Parent)
    $desen = [System.Management.Automation.WildcardPattern]::Escape($script:OnEk) + '*'
    if ($ust.ToLowerInvariant() -ne $script:Koku.ToLowerInvariant() -or -not ($kopyaAd -like $desen)) {
        Stop-Kopya 22 "havuzun parcasi degil: $yol (beklenen: $($script:Koku)\$($script:OnEk)*)"
    }
    Assert-KopyaGuvenli $yol
    return [pscustomobject]@{ Ad = $kopyaAd; Yol = $yol; Havuz = ((Get-HavuzAdlari) -contains $kopyaAd) }
}

function Assert-Kilit {
    param($Kopya, [string]$Islem)
    if (-not (Test-Path -LiteralPath $Kopya.Yol -PathType Container)) { Stop-Kopya 22 "kopya yok: $($Kopya.Yol) (once -Al)" }
    $kilit = Read-Kilit $Kopya.Yol
    if (-not $kilit) { Stop-Kopya 21 "$Islem reddedildi: $($Kopya.Ad) kilitli degil. Once: kopya.ps1 -Al -Sahip <ad>" }
    if ($Sahip -and $kilit.Sahip -ne $Sahip) {
        Stop-Kopya 21 "$Islem reddedildi: $($Kopya.Ad) kilidi '$($kilit.Sahip)' sahibinde, sen '$Sahip'."
    }
    return $kilit
}

function Clear-BayatUnityKilidi {
    # Yarida kesilmis batchmode Temp\UnityLockfile birakir; dogrulama bosuna 12 verir. Yalniz bu kopyayi
    # acik tutan bir Unity sureci YOKSA silinir (kontrol hemen silmeden once yapilir).
    param([string]$KopyaYol)
    $kilit = Join-Path $KopyaYol 'Temp\UnityLockfile'
    if (-not (Test-Path -LiteralPath $kilit)) { return }
    $desen = [regex]::Escape($KopyaYol.ToLowerInvariant()) + '(["\s\\]|$)'
    $acik = @(Get-CimInstance Win32_Process -Filter "Name='Unity.exe'" -ErrorAction SilentlyContinue | Where-Object {
        $_.CommandLine -and [regex]::IsMatch(($_.CommandLine -replace '/', '\').ToLowerInvariant(), $desen)
    })
    if ($acik.Count -gt 0) {
        Write-Bilgi "UYARI: Unity bu kopyayi acik tutuyor (pid $($acik[0].ProcessId)); Temp\UnityLockfile silinmedi."
        return
    }
    Remove-Item -LiteralPath $kilit -Force -ErrorAction SilentlyContinue
    Write-Bilgi "  bayat Temp\UnityLockfile silindi (bu kopyayi acan Unity sureci yok)"
}

function Set-UrunEki {
    # Kopya ana projeyle ayni companyName/productName'i tasirsa Editor PlayerPrefs'i ve persistentDataPath
    # ORTAK olur; kayit testleri kullanicinin kaydini ezer (bmbp 21.10.2026). Diskten tekrar okunarak dogrulanir.
    param([string]$KopyaYol)
    $ayar = Join-Path $KopyaYol 'ProjectSettings\ProjectSettings.asset'
    if (-not (Test-Path -LiteralPath $ayar)) { Stop-Kopya 13 "kopyada ProjectSettings.asset yok: $ayar" }
    $eki = $script:Eki
    $metin = [System.IO.File]::ReadAllText($ayar)
    $rx = '(?m)^([ \t]*productName:[ \t]*)(.*?)(\r?)$'
    $yeni = [regex]::Replace($metin, $rx, {
        param($m)
        $d = ConvertFrom-YamlDeger $m.Groups[2].Value
        if ($d.Ic.EndsWith($eki)) { return $m.Value }
        return $m.Groups[1].Value + $d.Tirnak + $d.Ic + $eki + $d.Tirnak + $m.Groups[3].Value
    })
    if ($yeni -ne $metin) {
        Write-Utf8NoBom -Path $ayar -Text $yeni
        Write-Bilgi "  productName'e '$eki' eklendi (ayri PlayerPrefs + kayit klasoru)"
    }
    $kontrol = [regex]::Matches([System.IO.File]::ReadAllText($ayar), $rx)
    if ($kontrol.Count -eq 0) { Stop-Kopya 13 "productName satiri bulunamadi: $ayar" }
    foreach ($m in $kontrol) {
        if (-not (ConvertFrom-YamlDeger $m.Groups[2].Value).Ic.EndsWith($eki)) {
            Stop-Kopya 13 "productName'e '$eki' eklenemedi; kayit testleri ana kaydi ezebilir. Durduruldu: $ayar"
        }
    }
}

function Restore-HeadHali {
    # Kaynaktaki commit'siz degisiklikleri kopyada HEAD haline dondurur (verify-head-only mantigi):
    #   ?? (izlenmeyen)      -> kopyada sil (+ .meta)
    #   HEAD'de olan yol     -> 'git cat-file --filters HEAD:<yol>' bayt bayt (checkout'un yazacagi hal:
    #                           core.autocrlf=true depoda CRLF; 'git show' LF verirdi)
    #   HEAD'de olmayan yol  -> kopyada sil (yeni eklenmis/stage'lenmis dosya)
    param([string]$KaynakYol, [string]$KopyaYol)
    $on = Invoke-Git $KaynakYol @('rev-parse', '--show-prefix')
    if ($on.Kod -ne 0) { Stop-Kopya 22 "-HeadOnly: kaynak bir git deposu degil: $KaynakYol" }
    $onek = $on.Cikti.Trim()
    $st = Invoke-Git $KaynakYol (@('status', '--porcelain', '-z', '--untracked-files=normal', '--') + $script:Klasorler)
    if ($st.Kod -ne 0) { Stop-Kopya 23 "git status basarisiz: $($st.Hata)" }
    $sil = New-Object System.Collections.Generic.List[string]
    $geri = New-Object System.Collections.Generic.List[string]
    $parca = $st.Cikti.Split([char]0)
    for ($i = 0; $i -lt $parca.Length; $i++) {
        $e = $parca[$i]
        if ($e.Length -lt 4) { continue }
        $xy = $e.Substring(0, 2)
        $yol = $e.Substring(3)
        if ($xy -eq '??') { $sil.Add($yol); continue }
        $geri.Add($yol)
        if ('RC'.Contains([string]$xy[0]) -or 'RC'.Contains([string]$xy[1])) {
            # -z: yeniden adlandirmada ikinci kayit eski yoldur (XY oneki yok).
            $i++
            if ($i -lt $parca.Length -and $parca[$i]) { $geri.Add($parca[$i]) }
        }
    }
    $sayac = @{ Silinen = 0; Donen = 0 }
    $goreli = {
        param([string]$KokGoreli)
        if ($onek -and $KokGoreli.StartsWith($onek)) { $KokGoreli = $KokGoreli.Substring($onek.Length) }
        return (Join-Path $KopyaYol ($KokGoreli.TrimEnd('/') -replace '/', '\'))
    }
    foreach ($y in $sil) {
        $h = & $goreli $y
        foreach ($p in @($h, "$h.meta")) {
            if (Test-Path -LiteralPath $p) { Remove-Item -LiteralPath $p -Recurse -Force; $sayac.Silinen++; Write-Bilgi "  silindi (izlenmeyen): $p" }
        }
    }
    foreach ($y in $geri) {
        $h = & $goreli $y
        $ust = Split-Path $h -Parent
        if (-not (Test-Path -LiteralPath $ust)) { New-Item -ItemType Directory -Force -Path $ust | Out-Null }
        $gecici = "$h.kopya-tmp"
        $r = Invoke-Git $KaynakYol @('cat-file', '--filters', "HEAD:$y") -CiktiDosyasi $gecici
        if ($r.Kod -eq 0) {
            Move-Item -LiteralPath $gecici -Destination $h -Force
            $sayac.Donen++
            Write-Bilgi "  HEAD'e donduruldu: $y"
        } else {
            Remove-Item -LiteralPath $gecici -Force -ErrorAction SilentlyContinue
            if ($r.Hata -match "does not exist|not in '?HEAD") {
                if (Test-Path -LiteralPath $h) { Remove-Item -LiteralPath $h -Recurse -Force; $sayac.Silinen++; Write-Bilgi "  silindi (HEAD'de yok): $y" }
            } else {
                Stop-Kopya 23 "git cat-file basarisiz ($y): $($r.Hata.Trim())"
            }
        }
    }
    Write-Bilgi "  HEAD hali: $($sayac.Donen) dosya donduruldu, $($sayac.Silinen) yol silindi"
}

function Get-GeriAlIsleri {
    param([string]$KopyaYol, [string]$HedefYol, [string[]]$Liste)
    $isler = @()
    foreach ($ham in $Liste) {
        $y = ($ham -replace '/', '\').Trim()
        while ($y.StartsWith('.\')) { $y = $y.Substring(2) }
        $y = $y.TrimStart('\').TrimEnd('\')
        if (-not $y -or [System.IO.Path]::IsPathRooted($y)) { Stop-Kopya 22 "gecersiz -Yollar ogesi (kopyaya gore goreli olmali): '$ham'" }
        $parcalar = $y.Split('\')
        if ($parcalar -contains '..') { Stop-Kopya 22 "'..' iceren yol kabul edilmez: '$ham'" }
        if ($script:Klasorler -notcontains $parcalar[0]) { Stop-Kopya 22 "yol KopyaKlasorleri disinda ($($script:Klasorler -join ', ')): '$ham'" }
        for ($i = 0; $i -lt $parcalar.Length - 1; $i++) {
            if ($parcalar[$i] -match '[*?]') { Stop-Kopya 22 "joker yalniz son parcada olabilir: '$ham'" }
        }
        $yaprak = $parcalar[-1]
        $ustGoreli = ''
        if ($parcalar.Length -gt 1) { $ustGoreli = ($parcalar[0..($parcalar.Length - 2)] -join '\') }
        $ustKaynak = $KopyaYol; $ustHedef = $HedefYol
        if ($ustGoreli) { $ustKaynak = Join-Path $KopyaYol $ustGoreli; $ustHedef = Join-Path $HedefYol $ustGoreli }
        $tam = Join-Path $KopyaYol $y
        if ($yaprak -notmatch '[*?]' -and (Test-Path -LiteralPath $tam -PathType Container)) {
            $isler += [pscustomobject]@{ Kaynak = $tam; Hedef = (Join-Path $HedefYol $y); Dosyalar = @(); Ozyinelemeli = $true }
            if (Test-Path -LiteralPath "$tam.meta") {
                $isler += [pscustomobject]@{ Kaynak = $ustKaynak; Hedef = $ustHedef; Dosyalar = @("$yaprak.meta"); Ozyinelemeli = $false }
            }
            continue
        }
        if (-not (Test-Path -LiteralPath $ustKaynak -PathType Container)) { Write-Bilgi "UYARI: kopyada yok, atlandi: $ham"; continue }
        $dosyalar = @($yaprak)
        if (-not $yaprak.EndsWith('.meta') -and $yaprak -ne '*' -and $yaprak -ne '*.*') { $dosyalar += "$yaprak.meta" }
        $isler += [pscustomobject]@{ Kaynak = $ustKaynak; Hedef = $ustHedef; Dosyalar = $dosyalar; Ozyinelemeli = $false }
    }
    return $isler
}

function Invoke-GeriAlIsi {
    param($Is, [bool]$Kuru, [bool]$KodHaric)
    # /XO: hedefte daha yeni olani ezme (sync'ten sonra worktree'de degisen dosya). /XX: hedefteki fazlaliga dokunma/listeleme.
    $a = @($Is.Kaynak, $Is.Hedef) + $Is.Dosyalar + @('/XO', '/XX', '/R:1', '/W:1', '/NP', '/NJH', '/NJS', '/NDL', '/FP', '/NS', '/NC')
    if ($Is.Ozyinelemeli) { $a += '/E' }
    if ($KodHaric) { $a += @('/XF', '*.cs') }
    if ($Kuru) { $a += '/L' }
    $cikti = & robocopy.exe @a
    $rc = $LASTEXITCODE
    if ($rc -ge 8) { Stop-Kopya 23 "robocopy basarisiz ($rc): $($Is.Kaynak) -> $($Is.Hedef)`n$(@($cikti) -join "`n")" }
    return @(@($cikti) | ForEach-Object { "$_".Trim() } | Where-Object { $_ -and $_.Length -gt 2 -and $_[1] -eq [char]':' })
}

# ------------------------------------------------------------------ komutlar

function Invoke-Liste {
    $liste = @(Get-Havuz)
    if ($Json) {
        Write-Output (ConvertTo-Json -InputObject $liste -Depth 3)
        return
    }
    Write-Output "Havuz: $($script:Koku)\$($script:OnEk)-1..$($script:Sayi)   urun eki: '$($script:Eki)'   bayat kilit: > $BayatSaat sa"
    Write-Output (($liste | Format-Table Ad, Durum, Sahip, YasDakika, UrunVerify, Not, Yol -AutoSize | Out-String -Width 250).TrimEnd())
}

function Invoke-Al {
    $sahipAd = $Sahip
    if (-not $sahipAd) { $sahipAd = $env:USERNAME }
    $liste = @(Get-Havuz | Where-Object { $_.Havuz })
    $secilen = $null
    foreach ($k in ($liste | Where-Object { $_.Durum -eq 'bos' })) {
        Assert-KopyaGuvenli $k.Yol
        if (New-Kilit $k.Yol $sahipAd $Not) { $secilen = $k; break }
    }
    if (-not $secilen) {
        foreach ($k in ($liste | Where-Object { $_.Durum -eq 'yok' })) {
            Assert-KopyaGuvenli $k.Yol
            New-Item -ItemType Directory -Force -Path $k.Yol | Out-Null
            if (New-Kilit $k.Yol $sahipAd $Not) { $secilen = $k; Write-Bilgi "  klasor olusturuldu (ilk dogrulama YAVAS: Library sifirdan kurulur)"; break }
        }
    }
    if (-not $secilen) {
        Write-Bilgi "HAVUZ DOLU ($($script:Sayi) kopya):"
        foreach ($k in @(Get-Havuz | Where-Object { $_.Havuz })) {
            Write-Bilgi ("  {0,-24} {1,-12} sahip={2}  yas={3} dk  not={4}" -f $k.Ad, $k.Durum, $k.Sahip, $k.YasDakika, $k.Not)
        }
        if (@(Get-Havuz | Where-Object { $_.Durum -eq 'bayat-kilit' }).Count -gt 0) {
            Write-Bilgi "  bayat kilit sahibi artik calismiyorsa: kopya.ps1 -Birak -Ad <ad>"
        }
        exit 20
    }
    Write-Bilgi "kopya alindi: $($secilen.Ad) (sahip: $sahipAd)"
    Write-Bilgi "  sonra: -Senkron -Ad $($secilen.Ad) [-HeadOnly]  ->  -Dogrula -Ad $($secilen.Ad)  ->  -Birak -Ad $($secilen.Ad)"
    Write-Output $secilen.Yol
}

function Invoke-Birak {
    if ($Hepsi) {
        if (-not $Sahip) { Stop-Kopya 22 '-Birak -Hepsi icin -Sahip gerekli.' }
        $n = 0
        foreach ($k in @(Get-Havuz)) {
            if ($k.Sahip -and $k.Sahip -eq $Sahip -and (Test-Path -LiteralPath (Join-Path $k.Yol $script:KilitAdi))) {
                Remove-Item -LiteralPath (Join-Path $k.Yol $script:KilitAdi) -Force
                Write-Bilgi "birakildi: $($k.Ad)"
                $n++
            }
        }
        Write-Bilgi "$n kopya birakildi (sahip: $Sahip)"
        return
    }
    $k = Resolve-Kopya $Ad
    $f = Join-Path $k.Yol $script:KilitAdi
    if (-not (Test-Path -LiteralPath $f)) { Write-Bilgi "$($k.Ad) zaten kilitli degil."; return }
    $kilit = Read-Kilit $k.Yol
    if ($Sahip -and $kilit.Sahip -ne $Sahip) { Stop-Kopya 21 "$($k.Ad) kilidi '$($kilit.Sahip)' sahibinde, sen '$Sahip'. Sahipsiz birakmak icin -Sahip verme." }
    Remove-Item -LiteralPath $f -Force
    Write-Bilgi "birakildi: $($k.Ad) (sahip: $($kilit.Sahip), $($kilit.YasDakika) dk)"
}

function Invoke-Senkron {
    $k = Resolve-Kopya $Ad
    $null = Assert-Kilit $k 'Senkron'
    $src = $script:RootYol
    if ($Kaynak) { $src = Get-TamYol $Kaynak }
    if (-not (Test-Path -LiteralPath $src -PathType Container)) { Stop-Kopya 22 "kaynak yok: $src" }
    if ((Test-Icinde $k.Yol $src) -or (Test-Icinde $src $k.Yol)) { Stop-Kopya 15 "kaynak ile kopya ic ice: $src <-> $($k.Yol)" }
    $varOlan = @($script:Klasorler | Where-Object { Test-Path -LiteralPath (Join-Path $src $_) -PathType Container })
    if ($varOlan.Count -eq 0) { Stop-Kopya 22 "kaynakta KopyaKlasorleri yok ($($script:Klasorler -join ', ')): $src" }
    Write-Bilgi "Senkron  kaynak: $src"
    Write-Bilgi "         kopya : $($k.Yol)   HeadOnly: $([bool]$HeadOnly)"
    foreach ($kl in $script:Klasorler) {
        $s = Join-Path $src $kl
        $d = Join-Path $k.Yol $kl
        if (-not (Test-Path -LiteralPath $s -PathType Container)) {
            if (Test-Path -LiteralPath $d) { Remove-Item -LiteralPath $d -Recurse -Force; Write-Bilgi "  UYARI: kaynakta $kl yok; kopyadaki silindi (ayna)" }
            continue
        }
        # /MIR: kopya kaynakla birebir ayni olmali, yoksa 'yesil' yalan olur. /R:2 /W:1: Unity dosyayi kilitliyse kisa deneme.
        $null = & robocopy.exe $s $d /MIR /XD .git /NFL /NDL /NJH /NJS /NP /R:2 /W:1
        if ($LASTEXITCODE -ge 8) { Stop-Kopya 23 "robocopy basarisiz ($kl), cikis $LASTEXITCODE" }
    }
    Write-Bilgi "  yansitma OK ($($script:Klasorler -join ', '))"
    if ($HeadOnly) { Restore-HeadHali $src $k.Yol }
    Set-UrunEki $k.Yol
    Clear-BayatUnityKilidi $k.Yol
    Write-Bilgi "SENKRON OK: $($k.Yol)"
}

function Invoke-Dogrula {
    $k = Resolve-Kopya $Ad
    $null = Assert-Kilit $k 'Dogrula'
    $urun = Get-UrunAdi $k.Yol
    if ($null -eq $urun -or -not $urun.EndsWith($script:Eki)) {
        Stop-Kopya 13 "kopyanin productName'i '$($script:Eki)' ile bitmiyor ('$urun'); kayit testleri ana kaydi ezebilir. Once -Senkron."
    }
    $vs = Join-Path $script:RootYol ($script:Cfg.DogrulamaScripti -replace '/', '\')
    if (-not (Test-Path -LiteralPath $vs)) { Stop-Kopya 22 "dogrulama script'i yok: $vs (config DogrulamaScripti)" }
    Clear-BayatUnityKilidi $k.Yol
    $argv = @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', $vs, '-ProjectPath', $k.Yol)
    if ($CompileOnly) { $argv += '-CompileOnly' }
    if (-not [string]::IsNullOrWhiteSpace($TestFilter)) { $argv += @('-TestFilter', $TestFilter) }
    Write-Bilgi "Dogrula  $vs -ProjectPath $($k.Yol)$(if ($CompileOnly) { ' -CompileOnly' })$(if ($TestFilter) { " -TestFilter $TestFilter" })"
    # Dogrulama script'i log'larini calisma klasorune yazar -> proje kokunden cagir.
    Push-Location $script:RootYol
    try {
        & powershell.exe @argv
        $kod = $LASTEXITCODE
    } finally { Pop-Location }
    if ($kod -eq 0) { $etiket = 'yesil' }
    elseif ($kod -ge 1 -and $kod -le 3) { $etiket = 'kirmizi' }
    else { $etiket = 'dogrulanamadi' }
    Write-Bilgi "DOGRULAMA: $etiket (cikis $kod)  kopya: $($k.Ad)"
    exit $kod
}

function Invoke-GeriAl {
    $k = Resolve-Kopya $Ad
    if (-not (Test-Path -LiteralPath $k.Yol -PathType Container)) { Stop-Kopya 22 "kopya yok: $($k.Yol)" }
    if (-not $Hedef) { Stop-Kopya 22 '-GeriAl icin -Hedef <worktree> gerekli.' }
    $h = Get-TamYol $Hedef
    if (Test-Icinde $h $script:Ana) {
        Stop-Kopya 15 "hedef ana worktree'nin icinde: $h. Ana klasore geri alma YAPILMAZ (kullanici orada calisiyor); kendi worktree'ne al, sonra birlestir."
    }
    if ((Test-Icinde $h $k.Yol) -or (Test-Icinde $k.Yol $h)) { Stop-Kopya 22 "hedef ile kopya ic ice: $h" }
    if (-not (Test-Path -LiteralPath $h -PathType Container)) { Stop-Kopya 22 "hedef yok: $h" }
    $liste = @()
    foreach ($y in $Yollar) { foreach ($p in ($y -split '[,;]')) { if ($p.Trim()) { $liste += $p.Trim() } } }
    if ($liste.Count -eq 0) { Stop-Kopya 22 '-GeriAl icin -Yollar <yol,...> gerekli.' }
    $isler = @(Get-GeriAlIsleri $k.Yol $h $liste)
    # 1) Kuru liste (/L). .cs'ler HARIC TUTULMADAN listelenir ki gorunsun: sync'ten sonra degismis kodu eski
    #    haliyle ezmek (bmbp 23.10) ya da builder'in yazdigi kodu sessizce birakmak istenmez.
    $dosyalar = @()
    foreach ($is in $isler) { $dosyalar += Invoke-GeriAlIsi $is $true $false }
    $dosyalar = @($dosyalar | Sort-Object -Unique)
    $kodlar = @($dosyalar | Where-Object { $_ -match '\.cs$' })
    $onekUz = $k.Yol.Length + 1
    Write-Bilgi "GeriAl  $($k.Yol) -> $h   ($($dosyalar.Count) dosya, /XO$(if (-not $KodDahil) { ' /XF *.cs' }))"
    foreach ($d in $dosyalar) {
        $g = $d; if ($d.Length -gt $onekUz) { $g = $d.Substring($onekUz) }
        $isaret = '  '; if ($d -match '\.cs$') { $isaret = '! ' }
        Write-Bilgi "  $isaret$g"
    }
    if ($kodlar.Count -gt 0 -and -not $KodDahil) {
        Stop-Kopya 14 "listede $($kodlar.Count) .cs var (! isaretli). Kod geri alinmaz; bilerek almak icin -KodDahil (once worktree'deki hallerine bak)."
    }
    if ($dosyalar.Count -eq 0) { Write-Bilgi 'kopyalanacak dosya yok (hedef ayni ya da daha yeni).'; return }
    if (-not $Onayla) { Write-Bilgi 'KURU: hicbir sey kopyalanmadi. Uygulamak icin ayni komuta -Onayla ekle.'; return }
    foreach ($is in $isler) { $null = Invoke-GeriAlIsi $is $false (-not $KodDahil) }
    Write-Bilgi "GERI ALINDI: $($dosyalar.Count) dosya -> $h  (kontrol: git -C `"$h`" status)"
}

# ------------------------------------------------------------------ ana akis

try {
    $komutlar = @()
    foreach ($n in @('Liste', 'Al', 'Birak', 'Senkron', 'Dogrula', 'GeriAl')) {
        if ((Get-Variable -Name $n -ValueOnly)) { $komutlar += $n }
    }
    if ($komutlar.Count -ne 1) {
        Stop-Kopya 22 "tam olarak bir komut ver: -Liste | -Al | -Birak | -Senkron | -Dogrula | -GeriAl (verilen: $($komutlar -join ', '))"
    }

    $script:Git = Get-GitExe
    if ($Root) { $script:RootYol = Get-TamYol $Root }
    else { $script:RootYol = Get-TamYol (Get-KitRoot -Baslangic (Split-Path $PSScriptRoot -Parent)) }
    $script:Cfg = Get-KitConfig -Root $script:RootYol
    $ham = [string]$script:Cfg.KopyaKoku
    if ([string]::IsNullOrWhiteSpace($ham) -or $ham.Contains('<')) {
        Stop-Kopya 22 "KopyaKoku doldurulmamis ($($script:RootYol)\agent-kit.config.psd1). Kopyalar depo kokunun DISINDA bir klasorde durur."
    }
    $script:Koku = Get-TamYol $ham
    $script:OnEk = [string]$script:Cfg.KopyaOnEki
    if ([string]::IsNullOrWhiteSpace($script:OnEk) -or $script:OnEk.Contains('<')) { Stop-Kopya 22 'KopyaOnEki doldurulmamis.' }
    $script:Sayi = [int]$script:Cfg.KopyaSayisi
    if ($script:Sayi -lt 1) { Stop-Kopya 22 "KopyaSayisi en az 1 olmali ($($script:Cfg.KopyaSayisi))." }
    $script:Eki = [string]$script:Cfg.KopyaUrunEki
    if ([string]::IsNullOrEmpty($script:Eki)) { Stop-Kopya 22 'KopyaUrunEki bos: kopya ana projenin kaydini paylasir. Config''e yaz (orn. '' VERIFY'').' }
    $script:Klasorler = @($script:Cfg.KopyaKlasorleri)
    $script:Ana = Get-TamYol (Get-AnaWorktree -Root $script:RootYol)
    $script:Worktreeler = @(Get-Worktreeler)
    if ($script:Worktreeler -notcontains $script:Ana) { $script:Worktreeler += $script:Ana }

    switch ($komutlar[0]) {
        'Liste'   { Invoke-Liste }
        'Al'      { Invoke-Al }
        'Birak'   { Invoke-Birak }
        'Senkron' { Invoke-Senkron }
        'Dogrula' { Invoke-Dogrula }
        'GeriAl'  { Invoke-GeriAl }
    }
    exit 0
} catch {
    Write-Bilgi "HATA (beklenmeyen): $($_.Exception.Message)"
    Write-Bilgi "  $($_.InvocationInfo.PositionMessage)"
    exit 24
}
