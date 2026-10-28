---
id: T-017
title: Bridge - IRaceTelemetry (hiz, enerji, cadence) + ana menu karakteri (canli, girdisiz, ayakta)
status: merged
owner: gameplay-agent
depends_on: [T-012]
owns_paths:
  - "Assets/Speed-Staes/Scripts/Bridge/**"
touches_scene: false
branch: agent/ui-v2
commits: []
tests:
  - "gerekmiyor: Bridge asmdef'siz ve Malik'in MonoBehaviour'larina bagli; kabul Play + PlayMode smoke (T-018)"
analytics:
  - "yok: projede analytics yok"
needs:
  - tip: WIRING
    durum: closed
    not: "RaceSessionAdapter -> IRaceTelemetry (RaceSceneInstaller), supervizor -> RaceSceneInstaller IRaceSession+IRaceTelemetry ayni adaptor"
started: 2026-10-28
finished: 2026-10-28
blocked_reason:
---

## Hedef
1. Yarış HUD'u (UI v2) hız, enerji ve cadence gösteriyor: `SpeedStars.Core.Race.IRaceTelemetry` (`Scripts/Core/Runtime/Race/IRaceTelemetry.cs`, hazır) —
   `RaceSessionAdapter` uygular: `SpeedMetersPerSecond` = sim hızı, `Energy01` = enerji / enerji üst sınırı (RunnerTuning'den, Malik'in public alanı),
   `PressesPerSecond` = `RunnerBody.Tapped` (basış) olaylarından kısa pencere (pencere süresi serileştirilmiş alan, Tooltip). Salt okunur.
2. **Ana menüye canlı karakter** (kullanıcı 28.10: "karakteri canlı canlı ana menüye koyacağız, customization çalıştıracağız"): Home sahnesinde
   `Prefabs/Character/HumanCharacterDummy_F.prefab` örneği duracak (sahneye koymak T-019). Bu task `Bridge/MenuCharacter.cs` bileşenini yazar: örnekteki
   koşu/girdi davranışını kapatır (RunnerInput, RunnerBody girdisi, RunnerReplay, RunnerBodyHUD) ve karakteri **ayakta bekleme** pozunda tutar —
   Malik'in `RunnerPoseDriver`'ı ile mi (girdisiz bekleme duruşu = çömelme olabilir, CLAUDE.md "Bekleme duruşu") yoksa sürücüyü kapatıp rig'in prefab
   varsayılanıyla mı, **kodu okuyup karar ver ve gerekçeyi yaz**; tasarımda karakter dik duruyor (`docs/gdd/referans-ekranlar/tasarim-ana-ekran-v2.png`).
   Malik'in koduna ve prefab'ına YAZILMAZ; yalnız sahnedeki örneğin bileşenleri (enabled) ve public alanları.
   Kozmetik (Faz 5) bu örneğin `Accessories/<Kategori>` yuvalarını açıp kapatacak: MenuCharacter onlara dokunmaz.

## Kabul kriterleri
- [ ] `RaceSessionAdapter : IRaceTelemetry`; Finished/Started davranışı değişmez.
- [ ] `MenuCharacter` (MonoBehaviour, `SpeedStars.Bridge`): `[SerializeField]` hedef (koşucu kökü), Awake/OnEnable'da davranışları kapatır; Play'de
      karakter yerinde, dik, kıpırdamaz (ya da hafif idle — yalnız Malik'in sistemi zaten veriyorsa).
- [ ] Çalışma notunda: T-019 sahne builder'ı için bağlama tablosu (nesne, bileşen, alan) ve seçtiğin bekleme yöntemi.

## Çalışma kuralları
- Çalışma klasörü MUTLAK: `D:\Pundun\speed-starts-uiv2` (worktree, dal `agent/ui-v2`). Ana klasöre yazma.
- Önce oku: Malik'in `Scripts/Physical/RunnerBody.cs`, `RunnerPoseDriver.cs`, `RunnerSim.cs`, `RaceController.cs`, `Scripts/Runner/RunnerInput.cs` (salt okunur),
  `CLAUDE.md` (bilgi), mevcut `Scripts/Bridge/**`.
- Unity/batchmode/verify YASAK, `git commit` YASAK, `.meta` yazma.
- Bitince `status: review`, `## Çalışma notu` 5 satır + bağlama tablosu.

## Çalışma notu

1. `RaceSessionAdapter` artik `IRaceTelemetry` da uyguluyor: hiz = `body.Sim.Speed`, enerji = `Sim.Energy / Sim.T.energyMax` (0..1), cadence = `Tapped(pressed=true)` olaylarindan `cadenceWindowSeconds` (serilestirilmis, Tooltip'li, varsayilan 1 sn) penceresi; `RunReset`'te pencere temizlenir. Started/Finished mantigina dokunulmadi.
2. `Bridge/MenuCharacter.cs` yeni: Awake/OnEnable/Initialize (idempotent) RunnerInput, RunnerReplay, RunnerBodyHUD, RunnerPoseDriver, RunnerBody'yi `enabled=false` yapar (+ `acceptInput=false`). Yalnizca ornegin bilesenleri.
3. Bekleme yontemi: pose driver KAPALI, rig prefab varsayilaninda. Gerekce: driver'in girdisiz duruşu `crouch` (blok cömelmesi, CLAUDE.md "Bekleme durusu"); `crouch` public alanini ezmek Malik'in ayarini ornekte degistirmek olurdu. Driver kapaliyken rig hedefleri prefab'in kendi rest'inde (driver ayni degerleri "rest/standing" diye okuyor); Awake kapali bilesende de kosar, yani `Bind()` kol IK agirligini 0 yapar. Prefab'ta Animator controller yok (m_Controller: 0), kipirdamaz.
4. Derleme: Roslyn (Unity csc) ile Bridge + Core/Race kaynaklari, referans verify-1 ScriptAssemblies: 0 hata (yalniz alan-atanmaz uyarilari).
5. Riskler: (a) kollarin duruşu (T/A-pose) gorsel olarak dogrulanmadi, Play'de bakilmali; fark varsa karar Malik'te (driver ayri 'standing' pozu). (b) Sahnede RaceController/SideFollowCamera/RunnerContactShadow ornekle birlikte gelirse T-019 builder'i koymamali ya da kapatmali.

### Baglama tablosu (T-019 sahne builder'i)
| Nesne | Bilesen | Alan | Deger |
|---|---|---|---|
| Home sahnesi, HumanCharacterDummy_F ornegi (kok) | `MenuCharacter` (SpeedStars.Bridge) | `runner` | bos = kendi GameObject'i (ya da ornek kokunu surukle) |
| Race sahnesi, RaceSessionAdapter nesnesi | `RaceSessionAdapter` | `cadenceWindowSeconds` | 1 (varsayilan) |
| Race kompozisyon koku (supervizor) | RaceSceneInstaller | `IRaceTelemetry` | ayni `RaceSessionAdapter` ornegi (`.FromInstance(adapter).AsSingle()`), IRaceSession ile ayni nesne |

