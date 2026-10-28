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
    # Malik'in alani (karakter yurume/kosma mekanigi, 2026-10-28 kullanici karari): bu oturumlar yazmaz.
    # Kitte ayri bir anahtar olmadigi icin "korunan belge" listesi kullaniliyor; ayni hook, ayni engel.
    KorunanBelgeler     = @(
        'Assets/Speed-Staes/Scripts/Physical/',
        'Assets/Speed-Staes/Physical/',
        'Assets/Speed-Staes/Prefabs/Character/',
        'Assets/Speed-Staes/Scenes/Test/PhysicalTest.unity',
        'Assets/Speed-Staes/Scripts/Runner/RunnerInput.cs',
        'Assets/Speed-Staes/Scripts/Runner/SideFollowCamera.cs',
        'Assets/Kevin Iglesias/',
        'Assets/Ash Assets/',
        'Assets/Samples/Animation Rigging/'
    )
    # Kit varsayilani + bu projenin kural dosyalari (CLAUDE.md Malik'in; bizim kurallar CLAUDE.local.md -> docs/CALISMA-KURALLARI.md).
    GuvenlikDosyalari   = @('CLAUDE.md', 'CLAUDE.local.md', 'docs/CALISMA-KURALLARI.md', '.claude/settings.json', 'scripts/hook-', 'agent-kit.config.psd1')

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
