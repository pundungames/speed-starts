# kit-lib.ps1 - agent-kit script'lerinin ortak kutuphanesi.
# Kullanim (script basinda):  . (Join-Path $PSScriptRoot 'kit-lib.ps1')
#
# Windows PowerShell 5.1 uyumlu: ?? / ?: / ?. YOK.
# Proje yollari ve kurallar koda degil, proje kokundeki agent-kit.config.psd1'e yazilir.
# Dosya yoksa asagidaki varsayilanlar gecerlidir.

$script:KitVarsayilan = @{
    KitSurumu           = '2.0.0'
    KitYolu             = ''
    ProjeAdi            = ''
    Motor               = 'Unity'
    KorunanUzantilar    = @('.unity', '.prefab', '.meta', '.asset', '.uxml', '.uss', '.mat', '.controller')
    KompozisyonKokleri  = @()
    KorunanBelgeler     = @()
    GuvenlikDosyalari   = @('CLAUDE.md', '.claude/settings.json', 'scripts/hook-', 'agent-kit.config.psd1')
    TaskKlasoru         = 'docs/tasks'
    DurumDosyasi        = 'docs/DURUM.md'
    TuzakDosyasi        = 'docs/TUZAKLAR.md'
    CheckpointKlasoru   = 'docs/checkpoints'
    RaporKlasoru        = 'reports'
    LogKlasoru          = 'pipeline-logs'
    KopyaKoku           = ''
    KopyaOnEki          = 'verify'
    KopyaSayisi         = 3
    KopyaUrunEki        = ' VERIFY'
    KopyaKlasorleri     = @('Assets', 'ProjectSettings', 'Packages')
    # Dogrulama script'i sozlesmesi (verify.ps1.ORNEK): -ProjectPath <yol> [-CompileOnly] [-TestFilter <regex>]
    # cikis 0 yesil / 1-3 kirmizi / 10-19 dogrulanamadi (ortam).
    DogrulamaScripti    = 'scripts/unity-verify.ps1'
    UnityCompany        = ''
    UnityProduct        = ''
    AnalyticsKatalog    = ''
    AnalyticsGaEslemesi = ''
    HeredocSatirSiniri  = 150
    IzinSuresiDakika    = 10
}

function Get-GitExe {
    # Bu makinede system32'de bos bir 'git' golgesi var; git.exe'yi acikca coz.
    $aday = 'C:\Program Files\Git\cmd\git.exe'
    if (Test-Path $aday) { return $aday }
    $cmd = Get-Command git.exe -CommandType Application -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($cmd) { return $cmd.Source }
    throw 'git.exe bulunamadi.'
}

function Get-KitRoot {
    param([string]$Baslangic)
    if (-not $Baslangic) { $Baslangic = (Get-Location).Path }
    $git = Get-GitExe
    $kok = & $git -C $Baslangic rev-parse --show-toplevel 2>$null
    if ($LASTEXITCODE -eq 0 -and $kok) { return ($kok -replace '/', '\') }
    return $Baslangic
}

function Get-KitConfig {
    param([string]$Root)
    if (-not $Root) { $Root = Get-KitRoot }
    $cfg = @{}
    foreach ($k in $script:KitVarsayilan.Keys) { $cfg[$k] = $script:KitVarsayilan[$k] }
    $dosya = Join-Path $Root 'agent-kit.config.psd1'
    if (Test-Path $dosya) {
        $proje = Import-PowerShellDataFile -Path $dosya
        foreach ($k in $proje.Keys) { $cfg[$k] = $proje[$k] }
    }
    $cfg['Root'] = $Root
    return $cfg
}

function Get-AnaWorktree {
    # Ana calisma agaci = 'git worktree list' ilk satiri (Unity Editor'un actigi klasor).
    param([string]$Root)
    if (-not $Root) { $Root = Get-KitRoot }
    $git = Get-GitExe
    $satir = & $git -C $Root worktree list --porcelain 2>$null | Where-Object { $_ -like 'worktree *' } | Select-Object -First 1
    if ($satir) { return ($satir.Substring(9) -replace '/', '\') }
    return $Root
}

function Get-UnityKimlik {
    # PlayerPrefs kayit anahtari companyName/productName'e baglidir.
    param([string]$ProjeYolu, $Config)
    $company = ''; $product = ''
    if ($Config) { $company = $Config.UnityCompany; $product = $Config.UnityProduct }
    $ps = Join-Path $ProjeYolu 'ProjectSettings\ProjectSettings.asset'
    if ((-not $company -or -not $product) -and (Test-Path $ps)) {
        foreach ($l in (Get-Content $ps -TotalCount 60)) {
            if (-not $company -and $l -match '^\s*companyName:\s*(.+)$') { $company = $Matches[1].Trim() }
            if (-not $product -and $l -match '^\s*productName:\s*(.+)$') { $product = $Matches[1].Trim() }
        }
    }
    return @{
        Company      = $company
        Product      = $product
        EditorPrefs  = "HKCU\Software\Unity\UnityEditor\$company\$product"
        PlayerPrefs  = "HKCU\Software\$company\$product"
    }
}

function Write-KitLog {
    param([string]$Ad, [string]$Mesaj, [string]$Root)
    if (-not $Root) { $Root = Get-KitRoot }
    $cfg = Get-KitConfig -Root $Root
    $klasor = Join-Path $Root $cfg.LogKlasoru
    New-Item -ItemType Directory -Force -Path $klasor | Out-Null
    Add-Content -Path (Join-Path $klasor "$Ad.log") -Value "$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss') $Mesaj"
}

function ConvertFrom-HookInput {
    # Hook script'i stdin'i KENDI kapsaminda okur ($input fonksiyona gecmez):
    #   $raw = @($input) -join "`n"
    #   if (-not $raw) { try { $raw = [Console]::In.ReadToEnd() } catch { $raw = '' } }
    #   $h = ConvertFrom-HookInput $raw 'guard-command'
    param([string]$Raw, [string]$HookAdi)
    if ([string]::IsNullOrWhiteSpace($Raw)) {
        Write-KitLog -Ad 'hook-sorunlari' -Mesaj "${HookAdi}: stdin BOS geldi, denetim yapilamadi."
        return $null
    }
    try { return ($Raw | ConvertFrom-Json) } catch {
        Write-KitLog -Ad 'hook-sorunlari' -Mesaj "${HookAdi}: JSON cozulemedi."
        return $null
    }
}

function Get-KitIzinYolu {
    # Izin isaretleri ANA calisma agacinin Temp'inde durur: oturum hangi worktree'den
    # calisirsa calissin Unity Editor ana klasoru aciktir ve hook'lar oraya bakar.
    param([string]$Ad, [string]$Root)
    $ana = Get-AnaWorktree -Root $Root
    return (Join-Path $ana "Temp\agent-kit-izin-$Ad")
}

function New-KitIzin {
    param([string]$Ad, [string]$Root)
    $yol = Get-KitIzinYolu -Ad $Ad -Root $Root
    New-Item -ItemType Directory -Force -Path (Split-Path $yol) | Out-Null
    Set-Content -Path $yol -Value (Get-Date -Format 'yyyy-MM-dd HH:mm:ss')
    return $yol
}

function Test-KitIzin {
    # Bilincli gecis: supervizor kullaniciya sorduktan sonra 'scripts/kit-izin.ps1 <ad>' ile
    # zaman damgali bir isaret birakir; IzinSuresiDakika icinde gecerlidir.
    param([string]$Ad, [string]$Root)
    if (-not $Root) { $Root = Get-KitRoot }
    $cfg = Get-KitConfig -Root $Root
    $isaret = Get-KitIzinYolu -Ad $Ad -Root $Root
    if (-not (Test-Path $isaret)) { return $false }
    $yas = (Get-Date) - (Get-Item $isaret).LastWriteTime
    return ($yas.TotalMinutes -le $cfg.IzinSuresiDakika)
}

function Write-Utf8NoBom {
    # git commit -F dosyasi BOM'suz olmali (PowerShell 5.1 Set-Content BOM yazar).
    param([string]$Path, [string]$Text)
    [System.IO.File]::WriteAllText($Path, $Text, (New-Object System.Text.UTF8Encoding($false)))
}

function Block-Tool {
    # PreToolUse hook'unda araci engelle: mesaj stderr'e, cikis 2.
    param([string]$Mesaj, [string]$HookAdi)
    if ($HookAdi) { Write-KitLog -Ad 'hook-engellenen' -Mesaj "${HookAdi}: $Mesaj" }
    [Console]::Error.WriteLine("ENGELLENDI: $Mesaj")
    exit 2
}
