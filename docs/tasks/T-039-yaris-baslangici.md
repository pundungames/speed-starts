---
id: T-039
title: Yaris baslangici READY / SET / GO! (Yaris_Baslangic_UI_v1) - kopru basla fazlarini yonetir, girdi GO'da acilir, UI tepki verir
status: todo
owner: gameplay-agent
depends_on: []
owns_paths:
  - "Assets/Speed-Staes/Scripts/Core/Runtime/Race/**"
  - "Assets/Speed-Staes/Scripts/Bridge/RaceSessionAdapter.cs"
  - "Assets/Speed-Staes/Scripts/Bridge/Dev/**"
  - "Assets/Speed-Staes/Scripts/UI/Race/RaceStart*.cs"
  - "Assets/Speed-Staes/Scripts/UI/Race/RaceHud*.cs"
  - "Assets/Speed-Staes/UI/Art/RaceStart/**"
  - "Assets/Speed-Staes/Tools/Editor/Builders/Ui/UiRaceStartPrefabs.cs"
  - "Assets/Speed-Staes/Tools/Editor/Builders/Ui/UiPrefabBuilder.cs"
  - "Assets/Speed-Staes/Tools/Editor/Builders/UiPrefabPaths.cs"
  - "Assets/Speed-Staes/Tools/Editor/Builders/Scene/SceneWiring.cs"
  - "Assets/Speed-Staes/Tools/Editor/Builders/Scene/RaceStartConfigSeed.cs"
  - "Assets/Speed-Staes/Tools/Editor/Builders/Scene/ConfigAssetSeeder.cs"
  - "Assets/Speed-Staes/Tests/EditMode/**/RaceStart*.cs"
  - "Assets/Speed-Staes/Tests/PlayMode/**"
  - "docs/TEMEL-SOZLESME.md"
touches_scene: true
branch: agent/T-039-yaris-baslangici
commits: []
tests:
  - "Assets/Speed-Staes/Tests/EditMode/Core/RaceStartSequenceTests.cs"
analytics:
  - "yok: projede analytics yok"
needs:
  - tip: DECISION
    durum: open
    not: "Malik: RaceController sureyi ILK BASISTA baslatiyor (sim.LastPressTime); gercek kurala gore sure GO aninda baslamali (tepki suresi). Bizim taraf GO'da girdiyi acar; saatin GO'dan baslamasi Malik'in kodu"
started:
finished:
blocked_reason:
---

## Hedef
Yarış sahnesi açılınca READY → SET → GO! geri sayımı. Sanat ve hareket: `C:\Users\Monster\Downloads\Yaris_Baslangic_UI_v1.zip`
(açılmış hâli süpervizör scratchpad'inde `baslangic-ui\Yaris_Baslangic_UI_v1\`: README.md, **IMPLEMENTATION.md = bağlayıcı hareket ve
olay kuralı**, layout.json, timing.json, storyboard.jpg, motion_preview.gif, `PNG\` 13 efekt katmanı 1024×576; build.py alınmaz).

Kural (IMPLEMENTATION.md "Start sinyali"): yarış durumunun tek sahibi başlangıç olaylarını yayınlar, **UI animasyon sonundan yarış başlatmaz**.
Bizde sahip = `RaceSessionAdapter` (Bridge; Malik'in `RaceController`/`RunnerBody` public API'sini okur/yazar, Malik'in koduna yazılmaz):
- `IRaceSession`'a başlangıç fazı: `RaceStartPhase { Idle, Ready, Set, Go }` + `event Action<RaceStartPhase> StartPhaseChanged` (+ iptal:
  Restart/sahne değişimi → Idle). `docs/TEMEL-SOZLESME.md`'deki IRaceSession bölümünü güncelle.
- Sahne açılışında ve her `RequestStart`'ta (Restart dahil): koşucu girdisi kapalı (`body.acceptInput = false`) → Ready → (config süresi) →
  Set → (config: rastgele bekleme min..max, `System.Random` dışarıdan) → Go: girdi açık (`acceptInput = true`), `Started` olayı (var olan)
  bu anla tutarlı. Zamanlama saf bir `RaceStartSequence` sınıfında (EditMode testli; sahte zaman), adapter yalnız uygular.
- Süreler `RaceStartConfig` (ScriptableObject) içinde; tohum: READY 0.8 s, SET bekleme 0.6–1.4 s (yer tutucu, oyun hissine göre ayarlanır).
- `DevForceFinish` geri sayım sırasında da çalışır (PlayMode testleri buna dayanıyor).

UI (`RaceStartView`, Race sahnesinde HUD'un üstünde, raycast kapalı): olaylara tepki verir — READY giriş + darbe/sekme (impact_01..03),
SET sıkışma + gerilim çizgileri (tension_*), HOLD hafif titreşim, GO! kırmızı büyüme + release_01..06 dışa çizgiler, sonra gizlen;
değerler IMPLEMENTATION.md'deki süre/ölçek/ease'ler (config'e). Metin legacy `Text` + `Outline` (TMP yok; kalın italik), renkler
#FFFCEF / #061A41 / #FF202D. Erken faz gelirse eski tween'i öldür, yenisini hemen göster; Restart'ta çift GO yok. DOTween yalnız çekirdek
(TUZAKLAR: modül uzantıları SpeedStars.UI'da yok). Ses yok (ses sistemi yok) — tetik noktaları kodda yorum olarak.

## Kabul kriterleri
- [ ] Yarış sahnesinde koşucu GO'dan önce dokunuşla hareket etmez, GO'da hareket eder; Restart'ta geri sayım baştan
- [ ] EditMode: sıra (Ready→Set→Go), rastgele bekleme config aralığında, iptal/Restart, erken çağrı; sabit süre assert edilmez
- [ ] PlayMode duman testleri yeşil (yarışa gir/bitir/sonuç/Race Again); ekran görüntüsü testine `Race_countdown` (SET ya da GO anı)
- [ ] Prefab builder'dan; efekt katmanları ortak 1024×576 tuval, merkez pivot (README); raycast kapalı
- [ ] needs: DECISION (Malik) açık kalır — bizim işin parçası değil

## Geri bildirim
GORSEL  : tasarımdaki darbe / sıkışma / patlama hareketleri
SES     : yok (ses sistemi yok; READY / SET / GO tetik yerleri işaretli)
DOKUNSAL: yok
ZAMAN   : yok
KAMERA  : yok (README: sürekli kamera sallanması yok)

## Kapsam disi
- Malik'in RaceController'ı (saatin GO'da başlaması), yanlış çıkış kuralı, rakip koşucular, ses

## Calisma notu
(Agent doldurur)
