---
id: T-090
title: Practice paneli - referanstaki antrenman secenekleri (baslangic 100/200/300/400 m, engeller, enerji; ayakta cikis Malik'te)
status: merged
owner: gameplay-agent
depends_on: [T-086]
owns_paths:
  - "Assets/Speed-Staes/Scripts/Meta/Practice/**"
  - "Assets/Speed-Staes/Scripts/Bridge/PracticeSession.cs"
  - "Assets/Speed-Staes/Scripts/Bridge/RaceHurdles.cs"
  - "Assets/Speed-Staes/Scripts/Bridge/RaceSessionAdapter.cs"
  - "Assets/Speed-Staes/Scripts/UI/Race/PracticePanelView.cs"
  - "Assets/Speed-Staes/Tools/Editor/Builders/Ui/UiPracticePrefabs.cs"
  - "Assets/Speed-Staes/Tools/Editor/Builders/Scene/ScenePracticePanel.cs"
  - "Assets/Speed-Staes/Tests/EditMode/Meta/Practice/**"
  - "Assets/Speed-Staes/Tests/PlayMode/PracticePanelSmokeTests.cs"
touches_scene: false
branch: agent/T-090-practice-paneli
commits: [32858536, c95d1791]
tests:
  - "Assets/Speed-Staes/Tests/EditMode/Meta/Practice/PracticeOptionsTests.cs"
  - "Assets/Speed-Staes/Tests/EditMode/Meta/Practice/PracticePanelModelTests.cs"
  - "Assets/Speed-Staes/Tests/PlayMode/PracticePanelSmokeTests.cs"
analytics:
  - "yok: projede analytics yok"
needs:
  - tip: DECISION
    durum: open
    not: "Malik: RunnerSim'e `RefillEnergy()` ve `lockEnergy` bayragi (public); arayuz Meta/Practice/PracticeContracts.cs IPracticeEnergy, PracticeSession.BindEnergy ile baglanir. Lock/Reset energy satirlari o zamana dek gri."
  - tip: DECISION
    durum: open
    not: "Malik: ayakta cikis (standing start) mekanigi, public bayrak/metot; arayuz IPracticeStandingStart, PracticeSession.BindStandingStart. Satir o zamana dek gri."
  - tip: SCENE
    durum: closed
    not: "Race sahnesine yama: Tools > Speed Stars > Build > Race scene - add Practice panel (ScenePracticePanel.PatchRaceScene). Config asset (Data/Config/PracticeConfig.asset) ve UI/Widgets/PracticePanel.prefab ayni adimda uretilir; sahneyi YENIDEN KURMAZ. -> c95d1791 (kopyada 03.11)"
  - tip: ASSET
    durum: closed
    not: "Yeni .cs dosyalarinin .meta'lari kopyada derlenip alinir (TUZAKLAR: .meta'siz commit). -> c95d1791 (kopyada 03.11)"

started: 2026-11-03
finished: 2026-11-03
blocked_reason:
---

## Hedef
Kullanici (03.11): "practice kismi var mi, yoksa ekleyelim". Bizde Practice var (Malik 8b246685: rakipsiz, biletsiz, 100 m, odulsuz) ama
referanstaki antrenman paneli yok. Referans (03.11 telefon): yarista sag ustte panel -
**Start: 100m / 200m / 300m / 400m** (acilir liste; secilen mesafeden kosulur), **Reset energy** dugmesi, **Lock energy**, **Hurdles**,
**Standing start** kutulari; saat GO beklemeden akar, sonuc/odul yok, Exit / Restart ust solda (var).
Yapilacak (yalniz Practice etkinliginde gorunur): panel prefab'i (builder) + view + saf secenek modeli (secimler kayitli: bir sonraki
Practice'te ayni). Mesafe: secim aninda kosucu basa alinir, `RaceController.raceDistance` yeni mesafe (Malik'in public alani; dosyasina yazma)
ve bitis cizgisi yerlesir. Hurdles: acikken secilen mesafeye engel dizilir (`RaceHurdles`; dizilim config'te, ornegin 100 m icin 110mH araligi,
uzun mesafede 400mH araligi - kendi tablonu config'e koy). Enerji: `RunnerSim.Energy` disaridan yazilamiyor (private set) -> Lock energy ve
Reset energy icin Malik'in public API'si gerekir: needs: DECISION ("RunnerSim'e `RefillEnergy()` ve `lockEnergy` bayragi"), satirlari yine
ekle ama API gelene kadar pasif (gri). Standing start: mekanikte ayakta cikis yok -> needs: DECISION, satir pasif.
Panel Race sahnesine sahneyi YENIDEN KURMADAN eklenir (hedefli yama, `SceneRaceEmotes.PatchRaceScene` kalibi; TUZAKLAR: BuildRace binalari siler).
Paralel calisan T-087 (Race of the Day) `RaceFlowLogic`, `ResultsScreenView`, `UI/Home/**`, `ConfigAssetSeeder`'e yaziyor: sen onlara YAZMA.

## Kabul kriterleri
- [ ] Practice'te panel gorunur, diger etkinliklerde gorunmez
- [ ] Mesafe secimi kosucuyu basa alir ve bitisi secilen mesafeye koyar; Hurdles engelleri dizer / kaldirir
- [ ] Secimler kayitli; enerji ve ayakta cikis satirlari API gelene kadar pasif (needs: DECISION)
- [ ] EditMode testleri (secenek modeli, dizilim); PlayMode duman testi gercek tiklamayla

## Kapsam disi
- Malik'in `Scripts/Physical/**` kodu; Race of the Day (T-087)

## Calisma notu
1. Pasif seam'li panel: Meta/Practice (config, secenek modeli, kayit `practice` anahtari ISaveStore'da), Bridge/PracticeSession, PracticePanelView, UiPracticePrefabs, ScenePracticePanel.
2. Mesafe: RaceSessionAdapter.RequestStart secimi uygular; secim RaceFlow.RequestRestart ile yeniden baslatir (Practice biletsiz). Hurdles: RaceHurdles READY'de Practice dizilimini alir.
3. PracticeSession panele kendini Bind eder: installer satiri gerekmedi. BuildRace'e panel eklenmedi; sahne yamayla gelir (BuildRace yeniden kurulursa yama tekrar kosulur).
4. Lock/Reset energy ve Standing start: API yok -> gri; IPracticeEnergy / IPracticeStandingStart arayuzleri hazir. Referanstaki "saat GO beklemeden akar" yapilmadi (RaceController saatini adapter GO'da baslatiyor, Malik alani).
5. Hicbir sey derlenmedi/kosulmadi; kopyada derleme + EditMode + PlayMode gerekir. Sonuc ekraninin Practice'te gizlenmesi T-087/ResultsScreen alani, yapilmadi.
