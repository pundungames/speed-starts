# UI/meta tarafinin (Pundun) yerel kit ayari. Kurulum: powershell -File scripts/kit-kur-yerel.ps1 -Kisi biz
# -> agent-kit.config.local.psd1 (git'e girmez), ortak agent-kit.config.psd1'deki ayni anahtarlari EZER.
@{
    # Malik'in alani (karakter yurume/kosma mekanigi, 2026-10-28 kullanici karari): bu oturumlar yazmaz.
    KorunanBelgeler     = @(
        'Assets/Speed-Staes/Scripts/Physical/',
        'Assets/Speed-Staes/Physical/',
        'Assets/Speed-Staes/Prefabs/Character/',
        'Assets/Speed-Staes/Scenes/Test/PhysicalTest.unity',
        'Assets/Speed-Staes/Scripts/Runner/RunnerInput.cs',
        'Assets/Speed-Staes/Scripts/Runner/SideFollowCamera.cs',
        'Assets/Kevin Iglesias/',
        'Assets/Ash Assets/',
        'Assets/Samples/Animation Rigging/',
        'docs/CALISMA-KURALLARI-malik.md',
        'docs/DURUM-malik.md',
        'docs/tasks-malik/'
    )
    # CLAUDE.md Malik'in; bizim kurallar CLAUDE.local.md -> docs/CALISMA-KURALLARI.md.
    GuvenlikDosyalari   = @('CLAUDE.md', 'CLAUDE.local.md', 'docs/CALISMA-KURALLARI.md', '.claude/settings.json',
                            'scripts/hook-', 'agent-kit.config.psd1', 'agent-kit.config.local.psd1')

    DurumDosyasi        = 'docs/DURUM.md'
    TaskKlasoru         = 'docs/tasks'
    CheckpointKlasoru   = 'docs/checkpoints'

    KopyaKoku           = 'D:\Pundun'
    KopyaOnEki          = 'speed-starts-verify'
    KopyaSayisi         = 2
    KitYolu             = 'D:\Pundun\agent-kit'
}
