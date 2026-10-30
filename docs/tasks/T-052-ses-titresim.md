---
id: T-052
title: Ses ve titresim - UI efekt sesleri (yer tutucu, uretilmis), muzik yuvasi, ayarlardaki Music/Sound anahtarlari, hafif titresim
status: review
owner: systems-agent
depends_on: [T-031]
owns_paths:
  - "Assets/Speed-Staes/Scripts/Core/Audio/**"
  - "Assets/Speed-Staes/Scripts/UI/Framework/ButtonSfxHook.cs"
  - "Assets/Speed-Staes/Scripts/UI/Race/RaceStart*.cs"
  - "Assets/Speed-Staes/Scripts/UI/Rewards/**"
  - "Assets/Speed-Staes/Audio/**"
  - "Assets/Speed-Staes/Tools/Editor/Builders/Scene/AudioConfigSeed.cs"
  - "Assets/Speed-Staes/Tools/Editor/Builders/Scene/ConfigAssetSeeder.cs"
  - "Assets/Speed-Staes/Tools/Editor/Builders/Scene/ProjectContextBuilder.cs"
  - "Assets/Speed-Staes/Tests/EditMode/Core/Audio/**"
  - "scripts/sfx-uret.py"
touches_scene: false
branch: agent/T-052-ses-titresim
commits: []
tests:
  - "Assets/Speed-Staes/Tests/EditMode/Core/Audio/AudioServiceTests.cs"
analytics:
  - "yok: projede analytics yok"
needs:
  - tip: ASSET
    durum: open
    not: "asmdef referanslari (yeni asmdef SpeedStars.Audio = Scripts/Core/Audio, Core+Meta+Zenject'e bagli; UI ve testler ona bagli degil): Assets/Speed-Staes/Scripts/UI/SpeedStars.UI.asmdef, Assets/Speed-Staes/Tests/EditMode/SpeedStars.Tests.asmdef ve Assets/Speed-Staes/Tests/PlayMode/SpeedStars.PlayTests.asmdef 'references' listesine \"SpeedStars.Audio\" ekle. Eklenmeden UI (ButtonSfxHook, RaceStartView, DailyGiftPopupView, SevenDayPanelView) ve testler DERLENMEZ."
  - tip: ASSET
    durum: open
    not: "Data/Config/AudioConfig.asset tohumu: Unity'de ConfigAssetSeeder.SeedAll(false) (AudioConfigSeed, 8 SfxId -> Audio/Sfx/<SfxId>.wav, muzik klibi bos). Once WAV'lar ve .meta'lari (yeni .cs/.asmdef .meta'lari dahil) Unity'de import edilmeli; AudioConfigSeed klip bulamazsa uyarir ve o ses sessiz kalir. Sonra ProjectContextBuilder.Build(overwrite: true) ProjectContext.prefab'ta ProjectInstaller.audio alanini baglar (alan yoksa uyarip atlar)."
  - tip: ASSET
    durum: open
    not: "Android manifest: android.permission.VIBRATE (AndroidJavaObject ile cagrildigi icin Unity otomatik eklemeyebilir; Assets/Plugins/Android/AndroidManifest.xml ya da Player Settings). Izin yoksa titresim sessizce hic calismaz (kod try/catch ile kapanir)."
  - tip: WIRING
    durum: open
    not: "ProjectInstaller: (1) alan `[SerializeField] private AudioConfig audio;` + InstallBindings'te `BindConfig(audio);` + `using SpeedStars.Core.Audio;`. (2) `Container.Bind(typeof(IAudioOutput), typeof(System.IDisposable)).To<AudioSourceOutput>().AsSingle();` (3) `Container.Bind(typeof(ISfx), typeof(IMusic), typeof(System.IDisposable)).To<AudioService>().AsSingle().NonLazy();` (NonLazy: muzik acilista baslasin, ayar anahtarlari dinlensin) (4) `Container.Bind<IHapticsDevice>().FromMethod(_ => HapticDevices.CreateDefault()).AsSingle();` (5) `Container.Bind<IHaptics>().To<HapticsService>().AsSingle();` (6) `Container.Bind<IButtonFeedbackHook>().To<SpeedStars.UI.Framework.ButtonSfxHook>().AsSingle();` IPlayerSettings bagliysa (ayni installer) servisler onu otomatik kullanir; hepsi InjectOptional."
started: 2026-10-30
finished: 2026-10-30
blocked_reason:
---

## Hedef
YOL-HARITASI Faz 8: "ses ve titresim geri bildirimi". Bugun: `IPlayerSettings.MusicOn` / sound anahtari kayitli ama onlari dinleyen ses
sistemi yok; `IButtonFeedbackHook` (UI/Framework) baglanmamis; projede hic ses dosyasi yok.

1) **Ses servisi** (`Scripts/Core/Audio/`, `SpeedStars.Core.Audio`): `enum SfxId` (Tap, Denied, Claim, Purchase, CountdownReady,
   CountdownSet, CountdownGo, Reward), `ISfx.Play(SfxId)`, muzik (`IMusic`, tek dongu kaynak; klip config'te bos olabilir -> sessiz).
   `AudioConfig` SO: SfxId -> klip + ses seviyesi, muzik klibi + seviyesi, ayni sesin art arda en kisa araligi. Ayar anahtarlari
   (`IPlayerSettings.Changed`) aninda uygulanir: muzik kapali -> muzik susar; efekt kapali -> efekt calmaz. AudioSource'lari servis
   kendi kalici GameObject'inde tutar (UI degil; ProjectContext altinda, sahne gecisinde kesilmez). Ayni karede ayni ses tek kez.
2) **Titresim** (`IHaptics`): Android'de kisa (config ms) `Vibrator`/`VibrationEffect` (AndroidJavaObject, API 26+ ve alti), editor/
   diger platformda no-op. Ayar: ayri anahtar yok -> efekt anahtarini izler (supervizor karari; calisma notuna yaz). Bekleme suresiyle
   spam yok.
3) **Baglanti noktalari** (yalniz bunlar): `ButtonSfxHook : IButtonFeedbackHook` (Pressed -> Tap + hafif titresim, Denied -> Denied);
   yaris basi READY/SET/GO (`UI/Race/RaceStart*` gorunumu, faz degisiminde CountdownReady/Set/Go); gunluk hediye CLAIM ve 7 gun CLAIM
   (`UI/Rewards`, Claim/Reward). Hepsi `[InjectOptional]` (baglama yokken bozulmaz).
4) **Yer tutucu sesler**: `scripts/sfx-uret.py` (yalniz standart kutuphane: wave + math) 44.1 kHz mono 16 bit WAV'lari
   `Assets/Speed-Staes/Audio/Sfx/` altina uretir: yumusak, kisa, sert olmayan (tepe -6 dBFS, 5 ms giris/cikis rampasi): Tap ~30 ms klik,
   Denied ~120 ms alcak cift blip, Claim/Reward ~350 ms yukselen uc nota, Purchase ~250 ms, CountdownReady/Set ~150 ms bip, CountdownGo
   ~400 ms daha tiz bip. Betigi calistirip WAV'lari olustur (.meta'yi Unity uretir, supervizor). Indirilenlerdeki baska projelerin
   sesleri KULLANILMAZ (lisans).
5) Seed: `AudioConfig.asset` tohumu (klipleri yoldan baglar) + ProjectContext'e config alani; baglama supervizorde (`needs:`).

## Kabul kriterleri
- [ ] EditMode (sahte ayar ve sahte cikis ile): efekt kapaliyken Play calmaz, acilinca calar; muzik anahtari mute'u izler; ayni karede
      cift ses yok; bos klip guvenli; titresim bekleme suresi; sabit ses seviyesi/ms assert edilmez
- [ ] WAV'lar uretildi, betik tekrar calisinca ayni dosyalar
- [ ] needs: WIRING (ISfx/IMusic/IHaptics/IButtonFeedbackHook, AudioConfig instance) + ASSET (AudioConfig.asset tohumu, WAV .meta) yazili

## Geri bildirim
GORSEL  : yok
SES     : bu task
DOKUNSAL: bu task
ZAMAN   : yok
KAMERA  : yok

## Kapsam disi
- Kosu sesleri (ayak, nefes, kalabalik) — mekanik tarafi; gercek ses tasarimi; ayarlarda ayri titresim satiri

## Calisma notu
1. Yapildi: Scripts/Core/Audio (SfxId, ISfx/IMusic/IHaptics, AudioService + HapticsService saf cekirdek, AudioSourceOutput kalici GameObject, Android AndroidJavaObject titresim #if'li, AudioConfig SO), ButtonSfxHook, RaceStart/Daily/7-gun baglantilari, AudioConfigSeed, scripts/sfx-uret.py + 8 WAV (iki calistirma ayni md5).
2. Zorluk: Scripts/Core/Audio hicbir asmdef'e dusmuyordu (Core asmdef yalniz Runtime/) ve UI/testler Assembly-CSharp'i goremez -> yeni SpeedStars.Audio asmdef acildi; UI ve test asmdef'lerine referans eklenmesi needs (ASSET), eklenene dek UI ve testler derlenmez.
3. Karar (supervizor): ayri titresim anahtari yok; titresim efekt (SfxOn) anahtarini izler. Titresim yalniz buton Pressed'ta (Denied'da yok).
4. Ses: Tap/Denied buton hook'unda; Ready/Set/Go faz degisiminde (gec-baglanma yakalamasinda calmaz); gunluk hediye Claim yalniz bilet verilince, 7 gun Reward. Muzik klibi bos tohumlandi; muzik susturma `mute` ile (durdurulmaz).
5. Calistirilmadi: Unity/derleme/test yok. Kayit/ekonomi dokunulmadi; tam suite gerekmez (hizli rejim: derleme + Core/Audio testleri). Belirsiz: test asmdef Zenject'i gormuyor; AudioService ctor'unda [Inject] var, kullanimda sorun cikmamali ama derleme dogrulamasi gerek.
