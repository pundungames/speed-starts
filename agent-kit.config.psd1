# agent-kit.config.psd1 - projeye ozel ayarlar. Proje KOKUNE konur.
# Kit script'leri (hook'lar, tur-sonu, kopya, install) yollari ve kurallari buradan okur;
# hicbir script'te proje yolu sabit yazilmaz.
@{
    KitSurumu           = '2.0.0'
    KitYolu             = 'D:\Pundun\agent-kit'        # install.ps1 -Guncelle buradan okur

    ProjeAdi            = 'Speed Stars (speed-starts)'
    Motor               = 'Unity 2022.3.62f2 (Built-in RP)'

    # --- Dosya sahipligi (hook-guard-write) ---
    # Bu uzantilara yalniz scene-integrator yazar (AGENT_PIPELINE_ALLOW_SCENE=1).
    KorunanUzantilar    = @('.unity', '.prefab', '.meta', '.asset', '.uxml', '.uss', '.mat', '.controller')
    # Kompozisyon kokleri: alt agent yazamaz, seam acip NEEDS_WIRING birakir. Zenject installer'lari.
    KompozisyonKokleri  = @('Assets/Speed-Staes/Scripts/Core/Installers/')
    # Kisiye ozel kilitler ORTAK config'te DEGIL: her kisinin agent-kit.config.local.psd1'inde (git'e girmez; ayni anahtari ezer).
    # Sablonlar: docs/kit-yerel/<kisi>.config.local.psd1 · kurulum: powershell -File scripts/kit-kur-yerel.ps1 -Kisi biz|malik
    # (speed-starts: UI tarafinin kilidi Malik'in alani, Malik'inki UI tarafininki; tek ortak liste birini kendi dosyalarina kilitliyordu.)
    KorunanBelgeler     = @()
    GuvenlikDosyalari   = @('.claude/settings.json', 'scripts/hook-', 'agent-kit.config.psd1', 'agent-kit.config.local.psd1', 'CLAUDE.local.md')

    # --- Task sistemi ---
    TaskKlasoru         = 'docs/tasks'
    DurumDosyasi        = 'docs/DURUM.md'
    TuzakDosyasi        = 'docs/TUZAKLAR.md'
    CheckpointKlasoru   = 'docs/checkpoints'

    # --- Dogrulama kopyalari (kopya.ps1) ---
    # Kopyalar depo KOKUNUN DISINDA durur: D:\Pundun\speed-starts-verify-1, -2
    KopyaKoku           = 'D:\Pundun'
    KopyaOnEki          = 'speed-starts-verify'
    KopyaSayisi         = 2
    KopyaUrunEki        = ' VERIFY'                   # kopyanin productName'ine eklenir: ayri PlayerPrefs + kayit klasoru
    # Dogrulama script'i: -ProjectPath <yol> [-CompileOnly] [-TestFilter <regex>] alir;
    # cikis 0 yesil / 1-3 kirmizi / 10-19 dogrulanamadi (ortam). Ornek: scripts/verify.ps1.ORNEK
    DogrulamaScripti    = 'scripts/unity-verify.ps1'

    # --- Unity kimligi (PlayerPrefs yedegi). Bos = ProjectSettings.asset'ten okunur ---
    UnityCompany        = ''
    UnityProduct        = ''

    # --- Analytics kapisi (tur-sonu). Bos = kontrol atlanir (projede analytics yok) ---
    AnalyticsKatalog    = ''
    AnalyticsGaEslemesi = ''

    # --- Shell ---
    HeredocSatirSiniri  = 150                         # ustu Bash'te "unexpected EOF" ile kiriliyor
    IzinSuresiDakika    = 10                          # kit-izin.ps1 ile verilen gecis suresi
}
