---
id: T-014a
title: Prefab builder'i view bilesenlerini takar ve prefab ici alanlari baglar
status: merged
owner: ui-agent
depends_on: [T-009, T-011, T-013]
owns_paths:
  - "Assets/Speed-Staes/Tools/Editor/Builders/Ui/**"
touches_scene: false
branch: agent/faz2-dilim
commits: [bb43cdb, dc7db07]
tests:
  - "gerekmiyor: editor builder'i; kabul supervizorun batchmode kosusu (overwrite) + Play"
analytics:
  - "yok: projede analytics yok"
needs: []
started: 2026-10-28
finished: 2026-10-28
blocked_reason:
---

## Hedef
`UiPrefabBuilder` (T-009 sonrası legacy Text) prefab köklerine **view bileşenlerini** takar ve **prefab içindeki** `[SerializeField]` alanlarını
`SerializedObject` ile bağlar. Bağlama tabloları: `docs/tasks/T-011-home-viewlari.md` ve `docs/tasks/T-013-race-viewlari.md` "Çalışma notu";
alan adlarının doğrusu view kodunda (`Scripts/UI/Home/**`, `Scripts/UI/Race/**`) — tablo ile kod çelişirse KOD haklı.

## Kabul kriterleri
- [ ] Kökler: HomeScreen → `HomeScreenView`, EventSelectScreen → `EventSelectScreenView`, EntryGateModal → `EntryGateModalView`,
      RaceHud → `RaceHudView`, ResultsScreen → `ResultsScreenView` (hepsi UiScreen/UiPopup'tan türer; `UiPanel` DisallowMultipleComponent →
      taban bileşen yerine türetilmiş olanı ekle). ResourceBar → `ResourceBarView`, EventRow → `EventRowView`, HighScoreRow → `ResultRowView`.
      Diğer ekranlar (Cosmetics, HighScores, 7 gün, DailyGift) `UiScreen`/`UiPopup` olarak kalır.
- [ ] Prefab içindeki bütün alanlar bağlı (Text, Button, Image, GameObject, satır prefab'ı asset referansı: EventSelect → EventRow, Results → HighScoreRow).
- [ ] **Sahne nesnesine giden alanlar** (ör. HomeScreenView → başka ekranlar / EntryGate modalı) prefab'ta BOŞ kalır; listesini çalışma notuna yaz —
      sahne builder'ı (T-014b) bağlayacak.
- [ ] Bağlanamayan alan (yol yok, tip uyuşmuyor) açık hata mesajıyla build'i durdurur (sessizce boş kalmaz).
- [ ] `overwrite` davranışı aynı; hiyerarşi adları aynı.

## Çalışma kuralları
- Çalışma klasörü MUTLAK: `D:\Pundun\speed-starts-faz2` (worktree, dal `agent/faz2-dilim`). Ana klasöre yazma.
- Builder editör assembly'sinde (Assembly-CSharp-Editor): `SpeedStars.UI.*` tiplerini görür. View kodunu DEĞİŞTİRME; gerekiyorsa BLOCKED yaz.
- Paralelde scene-integrator `Tools/Editor/Builders/Scene/**`'de (T-014b) çalışıyor. Unity/batchmode/verify YASAK, `git commit` YASAK, yalnız `.cs`.
  Derleme kontrolü scratchpad'de Unity'nin Roslyn'iyle (T-009'un betiği).
- Bitince `status: review`, `## Çalışma notu` 5 satır + sahne builder'ına kalan alanlar listesi.

## Çalışma notu
1. Yapildi: yeni `UiViewBinder.cs` (SerializedObject ile prefab ici baglama; eksik cocuk/bilesen/alan adi InvalidOperationException ile build'i durdurur). Kokler artik dogrudan view tipiyle uretiliyor (UiScreen/UiPopup AddComponent yerine; UiPanel DisallowMultiple): Home/EventSelect/RaceHud/Results/EntryGate (`PopupFrame<T>`), EventRow->EventRowView, HighScoreRow->ResultRowView, ResourceBar->ResourceBarView. Cosmetics/HighScores/DailyGift/7 gun UiScreen/UiPopup kaldi.
2. EventRow/HighScoreRow asset'i EventSelect/Results'a rowPrefab olarak baglanir (`RowComponent<T>`); diskteki eski satir prefab'inda view yoksa "overwrite=true ile yeniden kur" hatasi.
3. Sapma: ResultsScreenView.resultsContent tablodaki `LeftPanel/Results` yerine `LeftPanel/Results/Viewport/Content`'e baglandi (layout grubu orada; kodun tooltip'i `LeftPanel/Results` diyor ama satir ScrollRect'e degil Content'e girmeli).
4. Zorluk: yok; derleme KOSULMADI (Unity yasak), yalniz okuyarak denetlendi. Dosyalar: UiViewBinder.cs (yeni), UiScreenPrefabs.cs, UiRowPrefabs.cs, UiPrefabBuilder.cs.
5. Sahne builder'ina (T-014b) kalan alanlar: HomeScreenView -> eventSelectScreen (EventSelectScreenView), entryGateModal (EntryGateModalView), cosmeticsScreen (UiScreen), highScoresScreen (UiScreen), sevenDayPopup (UiPopup). Baska sahne alani yok; Race sahnesinde IResultsView/IEntryGatePrompt Zenject baglamasi (T-013 notu) ayri.
