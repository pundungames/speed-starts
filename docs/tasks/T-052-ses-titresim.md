---
id: T-052
title: Ses ve titresim - UI efekt sesleri (yer tutucu, uretilmis), muzik yuvasi, ayarlardaki Music/Sound anahtarlari, hafif titresim
status: todo
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
tests: []
analytics:
  - "yok: projede analytics yok"
needs: []
started:
finished:
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
