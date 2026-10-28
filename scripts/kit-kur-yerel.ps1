<#
.SYNOPSIS
    Bu makinede kisiye ozel agent-kit dosyalarini kurar (git'e girmeyenler). speed-starts'a ozel.
.DESCRIPTION
    Ayni main'de birden cok kisi kitle calisir (Pundun: UI/meta, Malik: kosucu mekanigi). Hook ayari, agent tanimlari,
    kilitler ve oturum kurallari kisiye ozeldir ve git'e girmez; sablonlari docs/kit-yerel/ altinda durur:
      agent-kit.config.local.psd1  <- docs/kit-yerel/<kisi>.config.local.psd1  (ortak config'in anahtarlarini ezer)
      CLAUDE.local.md              <- docs/kit-yerel/<kisi>.CLAUDE.local.md
      .claude/settings.json        <- docs/kit-yerel/<kisi>.settings.json (yoksa settings.json)
      .claude/agents/*.md          <- docs/kit-yerel/agents/*.md
      .git/info/exclude            <- yukaridakiler + pipeline-logs/  (yanlislikla commit'lenmesin)
    Var olan dosyanin ustune yazmaz (-Zorla haric). Kurulumdan sonra Claude Code oturumunu yeniden ac.
    Cikis: 0 tamam | 1 eksik kaynak / config'te doldurulmamis yer | 3 beklenmeyen hata
.EXAMPLE
    powershell -NoProfile -ExecutionPolicy Bypass -File scripts/kit-kur-yerel.ps1 -Kisi malik -Kuru
    powershell -NoProfile -ExecutionPolicy Bypass -File scripts/kit-kur-yerel.ps1 -Kisi malik
#>
param(
    [Parameter(Mandatory = $true)][ValidateSet('biz', 'malik')][string]$Kisi,
    [switch]$Zorla,
    [switch]$Kuru
)

$ErrorActionPreference = 'Stop'
try {
    . (Join-Path $PSScriptRoot 'kit-lib.ps1')
    $kok = Get-KitRoot -Baslangic (Split-Path $PSScriptRoot -Parent)
    $kaynak = Join-Path $kok 'docs\kit-yerel'
    $eksik = 0

    function Install-YerelDosya([string]$Kaynak, [string]$Hedef) {
        $h = Join-Path $kok $Hedef
        if (-not (Test-Path -LiteralPath $Kaynak)) { Write-Host "EKSIK kaynak: $Kaynak"; $script:eksik++; return }
        if ((Test-Path -LiteralPath $h) -and -not $Zorla) { Write-Host "atlandi (var): $Hedef"; return }
        if ($Kuru) { Write-Host "KURU yazilacakti: $Hedef"; return }
        New-Item -ItemType Directory -Force -Path (Split-Path $h -Parent) | Out-Null
        Copy-Item -LiteralPath $Kaynak -Destination $h -Force
        Write-Host "kuruldu: $Hedef"
    }

    Install-YerelDosya (Join-Path $kaynak "$Kisi.config.local.psd1") 'agent-kit.config.local.psd1'
    Install-YerelDosya (Join-Path $kaynak "$Kisi.CLAUDE.local.md") 'CLAUDE.local.md'
    $ayar = Join-Path $kaynak "$Kisi.settings.json"
    if (-not (Test-Path -LiteralPath $ayar)) { $ayar = Join-Path $kaynak 'settings.json' }
    Install-YerelDosya $ayar '.claude\settings.json'
    foreach ($a in @(Get-ChildItem -LiteralPath (Join-Path $kaynak 'agents') -Filter '*.md')) {
        Install-YerelDosya $a.FullName ".claude\agents\$($a.Name)"
    }

    # Yerel dosyalar git'e girmesin: ortak .gitignore'a degil, bu makinenin exclude'una (ekibi etkilemez).
    $git = Get-GitExe
    $exclude = (& $git -C $kok rev-parse --git-path info/exclude).Trim()
    if (-not [System.IO.Path]::IsPathRooted($exclude)) { $exclude = Join-Path $kok $exclude }
    $satirlar = @('.claude/settings.json', '.claude/settings.local.json', '.claude/agents/', 'CLAUDE.local.md',
                  'agent-kit.config.local.psd1', 'pipeline-logs/')
    $var = @(); if (Test-Path -LiteralPath $exclude) { $var = @(Get-Content -LiteralPath $exclude) }
    $ekle = @($satirlar | Where-Object { $var -notcontains $_ })
    if ($ekle.Count -gt 0) {
        if ($Kuru) { Write-Host "KURU exclude'a eklenecekti: $($ekle -join ', ')" }
        else {
            New-Item -ItemType Directory -Force -Path (Split-Path $exclude -Parent) | Out-Null
            Add-Content -LiteralPath $exclude -Value (@('', "# agent-kit yerel ($Kisi)") + $ekle)
            Write-Host "exclude'a eklendi: $($ekle -join ', ')"
        }
    }

    if (-not $Kuru) {
        $cfg = Get-KitConfig -Root $kok
        if ([string]$cfg.KopyaKoku -match '<') {
            Write-Host "DOLDUR: agent-kit.config.local.psd1 -> KopyaKoku (dogrulama kopyalarinin klasoru, depo disinda)."
            $eksik++
        }
        Write-Host "Kilitli yollar (bu makinede yazilmaz): $(@($cfg.KorunanBelgeler).Count) | durum: $($cfg.DurumDosyasi) | task: $($cfg.TaskKlasoru)"
    }
    Write-Host "Sonra: Claude Code oturumunu bu klasorde yeniden ac (hook'lar ve CLAUDE.local.md oturum basinda yuklenir)."
    if ($eksik -gt 0) { exit 1 }
    exit 0
} catch {
    Write-Host "HATA (beklenmeyen): $($_.Exception.Message)"
    exit 3
}
