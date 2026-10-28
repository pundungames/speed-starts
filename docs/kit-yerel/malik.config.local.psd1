# Malik'in yerel kit ayari. Kurulum: powershell -File scripts/kit-kur-yerel.ps1 -Kisi malik
# -> proje kokune agent-kit.config.local.psd1 olarak kopyalanir (git'e girmez) ve ortak agent-kit.config.psd1'deki
# ayni anahtarlari EZER. Malik = karakter yurume/kosma mekanigi. Kilitler: UI/meta tarafinin (Pundun) dosyalari.
@{
    # UI/meta tarafinin alani: Malik'in oturumlari yazmaz (hook-guard-write). Okumak serbest.
    KorunanBelgeler     = @(
        'Assets/Speed-Staes/Scripts/Core/',
        'Assets/Speed-Staes/Scripts/Meta/',
        'Assets/Speed-Staes/Scripts/UI/',
        'Assets/Speed-Staes/Scripts/Bridge/',
        'Assets/Speed-Staes/Tools/',
        'Assets/Speed-Staes/UI/',
        'Assets/Speed-Staes/Data/',
        'Assets/Speed-Staes/Tests/',
        'Assets/Speed-Staes/Scenes/Menu/',
        'Assets/Speed-Staes/Scenes/Game/Race.unity',
        'Assets/Resources/ProjectContext.prefab',
        'Assets/Plugins/',
        'docs/CALISMA-KURALLARI.md',
        'docs/DURUM.md',
        'docs/tasks/',
        'docs/YOL-HARITASI.md',
        'docs/TEMEL-SOZLESME.md',
        'docs/gdd/'
    )
    # CLAUDE.md Malik'in devir notu: Malik'in oturumlari onu guncel tutar, bu yuzden burada KORUNMAZ.
    GuvenlikDosyalari   = @('.claude/settings.json', 'scripts/hook-', 'agent-kit.config.psd1', 'agent-kit.config.local.psd1',
                            'CLAUDE.local.md', 'docs/CALISMA-KURALLARI-malik.md')

    # Kendi durum/task/checkpoint dosyalari: UI tarafininkiyle cakismasin (ayni main'e iki kisi push ediyor).
    DurumDosyasi        = 'docs/DURUM-malik.md'
    TaskKlasoru         = 'docs/tasks-malik'
    CheckpointKlasoru   = 'docs/checkpoints-malik'

    # Dogrulama kopyalari Malik'in makinesinde: depo kokunun DISINDA bir klasor yaz (or. 'D:\Pundun').
    KopyaKoku           = '<MALIK: kopya klasoru, or. D:\Pundun>'
    KopyaOnEki          = 'speed-starts-malik-verify'
    KopyaSayisi         = 1

    # Kit bu makinede baska yerdeyse (install.ps1 -Fark/-Guncelle icin); yoksa bos birak.
    KitYolu             = ''
}
