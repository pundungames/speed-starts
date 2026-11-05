---
id: T-120
title: Gercek sesler - kullanicinin Speed Stars SFX paketi yer tutucularin yerine, yeni olaylar (tabanca, adim, kalabalik, bitis, rekor, engel)
status: merged
owner: systems-agent
depends_on: [T-052]
owns_paths:
  - "Assets/Speed-Staes/Scripts/Core/Audio/**"
  - "Assets/Speed-Staes/Scripts/UI/Race/RaceStartView.cs"
  - "Assets/Speed-Staes/Scripts/UI/Race/ResultsScreenView.cs"
  - "Assets/Speed-Staes/Scripts/Bridge/RaceHurdles.cs"
  - "Assets/Speed-Staes/Scripts/Bridge/RaceStepSounds.cs"
  - "Assets/Speed-Staes/Tools/Editor/Builders/Scene/AudioConfigSeed.cs"
  - "Assets/Speed-Staes/Tests/EditMode/Core/Audio/**"
  - "Assets/Speed-Staes/Tests/PlayMode/RaceSoundsPlayTests.cs"
  - "scripts/sfx-uret.py"
  - "docs/tasks/T-120-sesler.md"
touches_scene: false
branch: agent/T-120-sesler
commits: []
tests:
  - "Assets/Speed-Staes/Tests/EditMode/Core/Audio/RaceSfxRulesTests.cs"
  - "Assets/Speed-Staes/Tests/PlayMode/RaceSoundsPlayTests.cs"
analytics:
  - "yok: projede analytics yok"
needs:
  - tip: WIRING
    durum: closed
    not: "RaceSceneInstaller: Container.BindInterfacesTo<RaceStepSounds>().AsSingle().WithArguments(raceSession).NonLazy(); (supervizor)"
  - tip: ASSET
    durum: closed
    not: "Yeni WAV ve .cs .meta dosyalari + AudioConfig.asset cue'lari: kopyada AudioConfigSeed.ApplySoundPack (supervizor)"
started: 2026-11-05
finished: 2026-11-05
blocked_reason:
---

## Hedef
Kullanici `main`'e gercek sesleri istiyor (Downloads/Speed Stars SFX.zip). WAV'lar `Assets/Speed-Staes/Audio/Sfx/` altina `<SfxId>.wav` adiyla
yazildi (supervizor, ffmpeg; ready/set/go'nun 14.5 sn sessizligi ve sondaki kacak sesi kesildi). Kod olaylari bu seslere baglar.

| SfxId | Paketteki dosya | Ne zaman |
|---|---|---|
| Tap | UI-tap.mp3 | buton (mevcut) |
| Denied | denied.wav | kullanilamayan buton (mevcut); yanlis cikista artik calmaz |
| Claim | reward.wav | hediye alma (mevcut); bitis cizgisinde artik calmaz |
| Purchase | purchase.wav | satin alma (mevcut) |
| CountdownReady / Set / Go | ready / set / go.mp3 | geri sayim (mevcut) |
| Reward | reward.wav | buyuk odul, nakit dokumu (mevcut); bitis cizgisinde artik calmaz |
| StartGun (yeni) | start-gun.mp3 | Go fazi, CountdownGo ile birlikte |
| CrowdStart (yeni) | crowd-start.mp3 | Go fazi |
| FalseStart (yeni) | wrong-start.mp3 | FalseStart fazi (Denied yerine) |
| StepLeft / StepRight (yeni) | tap1 / tap2.mp3 | oyuncunun her basisi: sol = tap1, sag = tap2 |
| ObstacleHit (yeni) | obstacle-hit.wav | oyuncunun kendi engelini devirmesi |
| FinishFirst (yeni) | 1st-win.mp3 | cizgi, rakipli yarista 1. |
| FinishRace (yeni) | complete-race.mp3 | cizgi, digerleri |
| CrowdEnd (yeni) | crowd-end.mp3 | cizgi, ikisinin yaninda |
| NewRecord (yeni) | new-record.wav | sonuc ekrani acilip rekor punch'i oynarken |

Kaynak: denied, obstacle-hit "UI & Effects" (Mad Brokli); purchase, reward, new-record "Cute UI & Interact Sound Effects Pack"; digerleri etiketsiz. Lisans kaniti yok, kullanici verdi.

## Yapilacaklar
1. `AudioContracts.cs`: `SfxId` sonuna yeni degerler (asset'te int duruyor: mevcutlarin sirasi degismez, araya eklenmez).
2. Saf kural (`SpeedStars.Audio` asmdef'inde, Unity/Assembly-CSharp'siz, testli): bacak -> StepLeft/StepRight (0 = sol, 1 = sag) ve (sira, kosucu sayisi) -> FinishFirst/FinishRace (mevcut kosul: `Rank == 1 && RunnerCount > 1`).
3. `RaceStartView`: Go'da CountdownGo + StartGun + CrowdStart; FalseStart'ta FalseStart; `OnFinished`'te kural + CrowdEnd.
4. `ResultsScreenView`: `InjectOptional ISfx`; rekor punch'inin oynadigi yerde (`_punchRecord`) NewRecord, sonuc basina bir kez.
5. `Bridge/RaceStepSounds.cs` (`ArrowKeyRunnerInput` kalibi: duz Zenject nesnesi, ctor `RaceSessionAdapter`, `[InjectOptional] ISfx`): `session.PlayerBody.Tapped` dinler, yalniz basista calar. Yalniz oyuncunun kendi adimi: `session.State` kosu halindeyken ve `IsFailing` degilken (yanlis cikista gate'in bastigi ayaklar, bitiste `RunnerCooldown`'un adimlari, bot ve replay ses vermez). PlayerBody ilk anda null olabilir (`docs/TUZAKLAR.md` "Tek seferlik bind"): hazir olana kadar tekrar dene, `Dispose`'ta birak.
6. `Bridge/RaceHurdles.cs`: `[InjectOptional] ISfx`; `Update`'te `hurdles.Knocked` artinca ObstacleHit (Configure/reset sayaci sifirlar, azalmada ses yok). `Physical/RunnerHurdles.cs` degismez.
7. `AudioConfigSeed`: public static `ApplySoundPack()` (`-executeMethod` ile kosacak): mevcut `AudioConfig.asset`'te her SfxId icin cue, klip `Audio/Sfx/<SfxId>.wav`; var olan cue'nun ses seviyesine dokunmaz, yeni cue 1; muzik ve haptic alanlari degismez; iki kez kosunca ayni; klip bulunamazsa hata verir. Ozet yorumlari ("placeholder") guncelle (`AudioConfig.cs`, `AudioConfigSeed.cs`).
8. `scripts/sfx-uret.py`: `--force` olmadan yazmayi reddetsin, yoksa tekrar kosunca gercek sesleri ezer.
9. Testler: `RaceSfxRulesTests` (kurallar + sevk edilen `AudioConfig.asset`'te her SfxId'nin klibi var; ses seviyesi/sure sayisi assert etme). `RaceSoundsPlayTests`: `Tests/PlayMode`'daki mevcut duman testlerinin kalibi, `ProjectContext` konteynerinde `ISfx`'i casusla degistirip Race sahnesini yukle; Ready/Set/Go + StartGun + CrowdStart, basista Step, bitiste FinishRace ya da FinishFirst + CrowdEnd calar; yanlis cikista FalseStart calar, Step calmaz. `WaitForEndOfFrame` yok.

## Kabul kriterleri
- [ ] Tablodaki her olay kendi SfxId'sini calar; bot, replay, geri sayim ve bitis adimi Step calmaz
- [ ] Ayarlardaki ses anahtari hepsini susturur (`AudioService` yolundan gecer, baska AudioSource yok)
- [ ] `RaceFinishGate`, `RaceController`, `Physical/**`, sahne, prefab, `.meta`, `.asset` dosyalarina dokunulmadi

## Geri bildirim
GORSEL  : yok
SES     : bu task
DOKUNSAL: yok
ZAMAN   : yok
KAMERA  : yok

## Kapsam disi
- Muzik klibi, ses seviyesi ayari, klip kesme, yeni kalabalik dongusu, bot sesleri

## Calisma notu
Yapildi: 1-9 uygulandi (SfxId sonuna 10 deger; saf kural `Core/Audio/RaceSfxRules.cs`; RaceStartView, ResultsScreenView, RaceHurdles, `Bridge/RaceStepSounds.cs`, `AudioConfigSeed.ApplySoundPack`, sfx-uret.py `--force`, testler). Derlenmedi, test kosulmadi.
Sapma: adim sesi kapisi "State == Running" degil "geri sayim yok + Finished degil + IsFailing degil": ilk basis Tapped'te State hala Ready (Running sonradan), `Running` isteseydim ilk adim sessiz kalirdi.
Not: `RaceSfxRulesTests.ShippedConfig_...` ApplySoundPack kosulana kadar kirmizi; `RaceSoundsPlayTests` adim testleri RaceStepSounds WIRING'i olmadan kirmizi. Yeni .cs'lerin .meta'si yok (RaceSfxRules, RaceStepSounds, iki test).
Zenject: `RaceStepSounds(RaceSessionAdapter session, [InjectOptional] ISfx sfx)`, ITickable + IDisposable (BindInterfacesTo yeterli).
Supervizor: kopyada derleme temiz, `ApplySoundPack` calisti, EditMode 23/23 (RaceSfxRulesTests + AudioServiceTests), PlayMode RaceSoundsPlayTests 2/2. Calistirilarak denenmeyenler: FinishFirst, NewRecord, ObstacleHit (kod bagli; RaceHurdles Race.unity'de var); gercekte kulakla dinlenmedi, ses seviyeleri 1. Tam suite kosulmadi (hizli rejim).
