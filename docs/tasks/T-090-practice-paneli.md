---
id: T-090
title: Practice paneli - referanstaki antrenman secenekleri (baslangic 100/200/300/400 m, engeller, enerji; ayakta cikis Malik'te)
status: todo
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
(Agent doldurur: 5 satir)
