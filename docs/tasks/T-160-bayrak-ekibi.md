---
id: T-160
title: Bayrak yarisi ekibi: ana ekranda 4 kisilik ekip, ayak secimi, yarista ayni ekip
status: merged
owner: ui-agent
depends_on: [T-159]
owns_paths:
  - "Assets/Speed-Staes/Scripts/Meta/Relay/"
  - "Assets/Speed-Staes/Scripts/Meta/Contracts/RelayContracts.cs"
  - "Assets/Speed-Staes/Scripts/UI/Home/"
  - "Assets/Speed-Staes/Scripts/Bridge/MenuRelayTeam.cs"
  - "Assets/Speed-Staes/Scripts/Bridge/MenuCharacter.cs"
  - "Assets/Speed-Staes/Scripts/Bridge/RaceRelay.cs"
  - "Assets/Speed-Staes/Tests/EditMode/Meta/Relay/"
  - "Assets/Speed-Staes/Tools/Editor/Builders/"
  - "docs/tasks/T-160-bayrak-ekibi.md"
touches_scene: true
branch: agent/ik-kosucu
commits:
  - 3cc6ea7a
  - 5ba3d660
  - 374b9266
tests:
  - "Assets/Speed-Staes/Tests/EditMode/Meta/Relay/"
analytics:
  - "yok: projede analytics yok"
needs:
  - tip: SCENE
    durum: closed
    not: "HomeScreen prefab: CharacterCard/Content/RelayLeg satiri (etiket + onceki/sonraki ok) ve HomeScreenView.relayLegRoot/relayLegText/previousLegButton/nextLegButton baglari. Builder: Tools > Speed Stars > Build > Home card - add the relay leg row to the HomeScreen prefab (patch). Satir kartin isim hapinin kopyasidir, bir satir asagida; konumu Inspector'dan ayarlanir. -> builder kopyada calisti (HOME_RELAY_ROW_OK); satir CharacterCard/RelayLeg (el ile duzenlenmis prefab'ta Select/Previous/Next Content altinda degil, builder Select'i kopyalayacak sekilde duzeltildi), 4 view baglari dolu"
  - tip: SCENE
    durum: closed
    not: "Home.unity: 'Relay Team' nesnesi (MenuRelayTeam, runnerPrefab, mainRunner, 3 slot). Builder: Tools > Speed Stars > Build > Home scene - add the relay team stage (patch). Slotlar ana karakterin yanina konur, sahnede tasinir. Yeni .cs dosyalarinin .meta'lari Unity acilinca olusur. -> builder kopyada calisti (HOME_RELAY_TEAM_OK); Relay Team: runnerPrefab, mainRunner, 3 slot dolu; .meta dosyalari geldi"
  - tip: WIRING
    durum: closed
    not: "ProjectInstaller -> IRelayTeam: Container.Bind<IRelayTeam>().FromMethod(c => SpeedStars.Meta.Relay.RelayTeamService.Create(c.Container.Resolve<ISaveStore>(), c.Container.Resolve<ICosmeticService>(), c.Container.Resolve<RaceEventCatalog>())).AsSingle();  (ICosmeticService satirindan sonra)"
  - tip: WIRING
    durum: closed
    not: "RaceBots (Scripts/Bridge/RaceBots.cs, owns_paths disinda) -> RaceRelay.UseTeam: [Inject] void ConstructRelayTeam([InjectOptional] IRelayTeam team) alani sakla; _relay.Bind(this) satirindan hemen sonra _relay.UseTeam(_relayTeam, _cosmetics, _catalog); (Awake'te _relay yaratildigi yer, satir ~197). Bu olmadan yaris takimi eskisi gibi rastgele giyinir."
started: 2026-11-06
finished: 2026-11-06
blocked_reason:
---

## Hedef
Kullanici 06.11 (referans oyundan ekran goruntusu): "bayrak yarisi icin bunu yap. bayraka girerken boyle ekip olmasi lazim." Secili etkinlik bayrak yarisi (`RaceEventDefinition.IsRelay`) iken ana ekranda tek karakter yerine 4 kisilik ekip durur, hangi ayagin duzenlendigi secilir, yarista oyuncunun takimi bu ekiptir.

Fikir alinir, gorunum kopyalanmaz: yerlesim, ikon, renk bizim arayuzumuzun (mevcut `CharacterCard`, American Captain, mevcut ok butonlari). Kullanici sahnede tasiyabilir.

## Bugun
- `HomeScreenView`: `_selection.Changed` -> `RefreshEventName` -> `RefreshRacerOfTheDay` (etkinlige gore kart parcasi gizleme ornegi). Kart oklari `StepPreset` ile `ICosmeticService` kiyafetini degistirir.
- `Bridge/MenuCharacter`: sahnedeki runner instance'ini durdurur, `ICosmeticPreview` kiyafetini giydirir.
- `Bridge/RaceRelay`: oyuncu seridi de NPC takimi; `Show(leg)` -> `_owner.DressRunner(leg.Look)` rastgele kiyafet.

## Istenen
**1. Veri (Meta, EditMode testli).** `IRelayTeam` + `RelayTeamService`: ayak sayisi (`RelayLegCount`, 4), her ayagin kiyafet preset'i, secili ayak. Varsayilan: ayak i -> preset (i mod `PresetCount`). Kayit `CosmeticService`'in kayit yoluyla ayni sistem. Ayni preset iki ayakta olabilir. Preset sayisi azalirsa bozulmaz. `Changed` olayi.

**2. Ana ekran.** Secili etkinlik bayraksa:
- Kartta ayak satiri: `relayLegRoot`, etiket (`1st leg`, `2nd leg`, `3rd leg`, `4th leg`, `TMP_Text`, metin `HomeTexts`), onceki/sonraki ok. Sarar.
- Ayak degisince o ayagin preset'i `ICosmeticService`'te secilir: Edit racer, Emote, isim, stat barlari ek kod olmadan o ayagi gosterir. Kart oklari preset degistirir ve ayaga yazar.
- Bayraktan cikinca (etkinlik degisti) girerken secili olan preset'e donulur. Race of the Day ile cakismaz.
- Bayrak degilse ayak satiri gizli, davranis bugunku gibi birebir.

**3. Ekip (`Bridge/MenuRelayTeam`).** `[SerializeField] GameObject runnerPrefab`, `[SerializeField] Transform[] slots` (3 yer, sahnede, kullanici tasir). Bayrakta ilk kez gerekince 3 runner kopyasi; `MenuCharacter`'in durdurma yolunu paylasir (input, sim, surucu kapali, `CosmeticApplier` ile kiyafet). Ortadaki ana karakter secili ayagi giyer, digerleri kalan ayaklari sirayla. Gelis/gidis: kisa DOTween (hizasindan kayarak, snap yok, ~0.25 sn, sure alan). Ana ekran disindaki ekranlarda (kostum, emote, ...) ekip gizli: `HomeScreenView` `OnEnable/OnDisable` ya da mevcut akisla.
Ayak degistirince kiyafet degisimi aninda ama kucuk bir geri bildirimle (ana karakterde kisa olcek ya da oyuncu kartinda mevcut `UiMotionConfig` receteleri; `agent-kit/05-DOTWEEN-UI-ANIMASYON.md`).

**4. Yaris.** `RaceRelay` oyuncu takiminda her ayagin runner'i `IRelayTeam` kiyafetini giyer. NPC takimlari rastgele kalir. `InjectOptional`: servis yoksa bugunku gibi.

**5. Sahne/prefab.** Sen yazma: `needs:` `tip: SCENE`: Home ekranina ayak satiri (builder `Tools/Editor/Builders` icinde ise builder'i sen yaz, calistirmayi scene-integrator yapar), Home sahnesine `MenuRelayTeam` ve 3 slot, referanslar. Bagimlilik: `needs:` `tip: WIRING`: `IRelayTeam` -> `ProjectInstaller`. Installer'a yazma.

## Kabul
- [ ] EditMode: varsayilan atama, secim, kayit-yukleme, preset azalinca guvenli, sarma.
- [ ] Etkinlik bayrak: 4 karakter, satir gorunur; degil: 1 karakter, satir yok, bugunku akis ayni.
- [ ] Ayak degistirince kart, isim, stat o ayagi gosterir; Edit racer o preset'i acar.
- [ ] Yarista oyuncu takimi secilen kiyafetlerle.
- [ ] Inspector alanlarinda Tooltip. Metin Ingilizce.

## Kurallar
`docs/CALISMA-KURALLARI.md` §3, §3.1, §9, §11, §12 gecerli: UI runtime'da kurulmaz, `.unity/.prefab/.asset/.meta` yazma, `Scripts/Core/Installers/` yazma, `Find` yok, Zenject, DOTween, `TMP_Text`, `[SerializeField] private`. Unity calistirma. Commit `git commit -F <mesaj-dosyasi> -- <yollar>`, mesaj dosyasi scratch'te (`C:\Users\Monster\AppData\Local\Temp\claude\d--Pundun-speed-starts\1c7d0dba-289b-4948-ba09-42f88d086602\scratchpad\`), adinda "main" yok, son satir `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`. Git'i Bash ile cagir. `git add -A`, push yok. Calisma yeri `D:\Pundun\speed-starts-ik`.

## Calisma notu
- Veri: `IRelayTeam`/`RelayTeamService` ayri kayit anahtari ("relay", Practice gibi `ISaveStore`); `PlayerProgress` owns_paths disinda oldugu icin ona alan eklenmedi. Ayak sayisi katalogdaki en uzun bayrak etkinligi.
- Ana ekran: `HomeScreenView` bayrakta ayagin preset'ini `ICosmeticService`'te secer, cikinca onceki preset'e doner; wardrobe'dan donuste secilen preset ayagin yeni presetidir.
- Sahne: `MenuRelayTeam` (Bridge) kendi kendine gorunur (Home ustte + bayrak, Race of the Day degil); UI asmdef Bridge'i goremedigi icin HomeScreenView ile baglanti yok.
- Yaris: `RaceRelay.UseTeam` seam'i acildi; `RaceBots` owns_paths disinda oldugu icin cagri WIRING maddesinde.
- Zorlandigim yer: derleyici/Unity yok, hicbir sey calistirilmadi; builder yerlesimi (satir ve slot konumu) gorulmeden tahmin, kullanici tasir.
- WIRING: `ProjectInstaller` `IRelayTeam`, `RaceBots` `UseTeam` (7a22f2a4). Malik a670ea7b ile birlesti (oyuncu tum takimi kosuyor); tek cakisma `Build` icinde, ikisi de tutuldu. Testler 30/30 (t161-a). Ana ekrani kimse gormedi, satir ve slot yerleri tahmin: kullanici bakacak.
