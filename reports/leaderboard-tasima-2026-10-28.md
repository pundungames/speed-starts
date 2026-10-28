# Leaderboard taşıma raporu — Sticker Patch → Speed Stars — 2026-10-28

1. **Alınabilirlik: orta.** UI parçaları (satır, liste, isim/bayrak penceresi) ve sanat (68 bayrak, sprite'lar, Nunito) temiz alınır. Ama "sahte leaderboard" aslında bir giriş listesi değil. Sıra, toplam Brain Score'dan hesaplanan bir sayı (`rank = 4685 − skor/50`), botların skoru da oyuncunun skoruna göre üretiliyor. Etkinlik ve dönem anahtarlı, süre tabanlı bir tabloya bu çekirdek uymuyor, yeniden yazılmalı.
2. **En büyük iki bağımlılık:** (a) `StickerPatchBrainProfile` ve `StickerPatchPuzzleResult` (skor, seviye adı, `Load()`), (b) statik kayıt ve yükleme katmanı (7 PlayerPrefs anahtarı, Resources/AssetDatabase yükleyicileri). Bizim kurala aykırı olan beş yerde UI çalışma zamanında koddan kuruluyor (§2.4).
3. **Öneri: 5 task** (§4).

**Kısaltmalar:** `SP/` = `D:\Pundun\shikakuflow\Assets\Game\Scripts\StickerPatch\`, `LB/` = `D:\Pundun\shikakuflow\Assets\Game\StickerPatch\Resources\Leaderboard\`. Satır numaraları `wc -l` / `grep -n` ile dosya bazında alındı.
**Yöntem:** 18 leaderboard dosyasının hepsi okundu. Üç prefab'ın GUID'leri .meta taramasıyla yol adına çözüldü. İki proje arasında GUID çakışması tarandı. Dışarıdan kullanım `grep` ile bulundu. GDD'nin PDF metni ve gömülü ekran görüntüleri incelendi. **Hiçbir dosya değişmedi** (yazılan tek dosya bu rapor). Unity açılmadı, git yalnız okundu.

## 1. Modülün yapısı

### 1.1 Dosyalar — çalışma zamanı: 14 dosya / 3 204 satır, editor: 4 dosya / 864 satır

| Dosya | Satır | Görev (kanıt) | Karar |
|---|---|---|---|
| `IStickerPatchLeaderboardBackend.cs` | 10 | Arayüz: `SubmitDaily` / `GetDailyTop` / `GetPlayerStanding`, hepsi `dayKey` alıyor ve senkron (:5-9) | Uyarla |
| `StickerPatchLeaderboardTypes.cs` | 168 | `Entry` puzzle alanlarını taşıyor (:6-49). `Row` (:52), `Standing` (:63), `SubmitResult` (:84), `Page` (:94). `DayStore` (:108) ve `Comparer` (:113, önce skor büyükten küçüğe, sonra yardım cezası, hata, süre) hiçbir yerde kullanılmıyor (grep) | Row/Standing/SubmitResult/Page **al**, Entry/Comparer yeniden yaz, DayStore at |
| `StickerPatchLeaderboard.cs` | 189 | Statik cephe. Kimlik PlayerPrefs'te (:5-7, :49-50, :73, :95). `Backend` setter'ı var (:14-26), ama gönderim arayüzü atlayıp `LocalBackend.UpdateStanding`'i doğrudan çağırıyor (:128). Okuma `Backend` üzerinden (:154-155) | Servis olarak yeniden yaz |
| `StickerPatchLeaderboardLocalBackend.cs` | 66 | `SubmitDaily` boş (:5-7). Her okumada `StickerPatchBrainProfile.Load()` çağrılıyor (:60) | At |
| `StickerPatchFakeLeaderboard.cs` | 379 | Sabitler (:7-11). Sıra skordan hesaplanıyor (:34-37). 4 PlayerPrefs anahtarı (:13-16). Sayfa: 1–3. sıra + oyuncunun ±8 çevresi (:191-234). Bot skoru oyuncuya göre (:137-141). Bot kimliği sıraya bağlı tohumla (:347-358). İsimler JSON'dan (:17, :332). Tırmanma kaydı (:51-82, :99-135) | Fikri al, kodu yeniden yaz |
| `StickerPatchLeaderboardCountryFlags.cs` | 171 | 68 ülke kodu (:19-31) ve normalizasyon (:75-84). Sprite editörde AssetDatabase'ten, build'de Resources'tan yükleniyor (:133-170). `D:/Pundun/SmashTheBall` mutlak yolu (:9-10) | Kod listesini al, yükleyici yerine SO |
| `StickerPatchLeaderboardPanelView.cs` | 449 | Panel. İsimle ve buton yazısıyla arama yapıyor (:9-21, :170-226, :404-437). Resources yedeği (:159, :335), fabrika yedeği (:343), `AddComponent` (:272). Özette Brain Score ve harf seviyesi (:131-144) | Uyarla |
| `StickerPatchLeaderboardRowView.cs` | 126 | Satır: sıra, bayrak, isim, skor, isim düzenleme butonu. Her `Bind`'de sprite, font, renk ve boyutu kodla basıyor (:60-110) | **Al**, stili prefab'a taşı |
| `StickerPatchLeaderboardScrollList.cs` | 692 | Elle yerleşimli liste: ilk 3 + nokta ayracı + pencere. Oyuncuyu ortalıyor (:455-470), tırmanma animasyonu coroutine ile (:177-237, :472-488). `FakeLeaderboard`'a yazıyor (:173, :191, :232-235) | Uyarla |
| `StickerPatchLeaderboardTopThreeView.cs` | 125 | Podyum, çalışma zamanında kuruluyor (:67-103). **Ölü kod:** prefab'ta yok (`LB/LeaderboardPanel.prefab:7940 topThreeView: {fileID: 0}`), panel onu gizliyor (PanelView:99, :108). İlk 3 aslında listenin ilk üç satırı | At |
| `StickerPatchLeaderboardTypography.cs` | 121 | Nunito yolları ve renkler (:6-10), AssetDatabase/Resources ile yükleme (:100-116) | At (font ve renk prefab'ta) |
| `StickerPatchLeaderboardUiSprites.cs` | 96 | Sprite yükleyici (`Leaderboard/Ui/`, editörde `ThisProject/Leaderboard`) (:9-11, :47-59) | At (`[SerializeField]`) |
| `StickerPatchLeaderboardFlagPickerView.cs` | 375 | Bayrak seçici. Mevcut hiyerarşiyi isimden okuyor (:130-179), yoksa kendisi kuruyor (:51-127). Katalog farklıysa hücreleri silip yeniden kuruyor (:185-241, :274-322) | Uyarla (hücre prefab'ı + factory) |
| `StickerPatchLeaderboardNameEditDialogFactory.cs` | 237 | İsim penceresinin tamamını kodla kuruyor (:10-43). Ad sınırı 16 karakter | At (sınırı al) |
| `Editor/…UiPrefabsBuilder.cs` | 419 | Row/Panel prefab'larını koddan Resources'a yazıyor (:9, :15, :26), `CollectionScrollBuilder`'ı kullanıyor (:284) | At |
| `Editor/…NameEditDialogBuilder.cs` | 229 | Diyalog prefab'ını kurup panele iç içe bağlıyor. Rebuild eski prefab'ı siliyor (:140) | At |
| `Editor/…CountryFlagsBuilder.cs` | 154 | Bayrakları SmashTheBall'dan kopyalıyor, fazlalarını siliyor (:65, :82-104) | At (bayraklar bir kez kopyalanır) |
| `Editor/…UiInstaller.cs` | 62 | Sticker Patch sahnesine kuruyor: `FindObjectOfType`/`GameObject.Find` (:22-23, :30), eskisini `DestroyImmediate` (:33) | At |

### 1.2 Bugünkü akış ve sahte bot üretimi
- **Gönderim:** `StickerPatchBrainApp.cs:2413-2415` → `UpdateStanding` → `FakeLeaderboard.UpdateFromProfile`: sıra = `clamp(4685 − totalBrainScore/50, 1, 4685)`. Bu sonuç PlayerPrefs'e (`BestRank`, `PendingClimbFrom`) yazılıyor.
- **Okuma:** `GetCurrentPage` → `BuildPageForProfile` → `BuildPage`. Sayfada 3 + 17 satır var ve `climbFromRank` doluysa pencere genişliyor (Fake:216-223).
- **"Günlük" sadece isimde:** `dayKey` sayfaya yazılıyor (Fake:185) ama hiçbir hesabı etkilemiyor. Tablo hiç sıfırlanmıyor ve gerçek bir giriş listesi yok.
- **Botlar:** toplam 4692, oyuncu 4685. sıradan başlıyor (Fake:7-8). Bot kimliği `tohum = rank·1009+17`: 1000 isimlik JSON + 40 ülkelik havuz (Fake:24-30, :347-358), `playerId = "ai-<rank>"` (:172). Bot skoru `oyuncuSkoru + (oyuncuRank − rank)·50` (:137-141). Yani oyuncu ilerledikçe botların skoru da kayıyor.

### 1.3 Prefab'lar ve sanat
| Prefab | Satır | İçerik ve dış bağımlılık |
|---|---|---|
| `LB/LeaderboardRow.prefab` | 723 | RowView, `ThisProject/Leaderboard/leader-others.png`, NunitoSans-Bold SDF, kalem ikonu `GUI-CasualFantasy/.../Icon_Pencil.Png` |
| `LB/NameEditDialog.prefab` | 9 480 | FlagPickerView, `ThisProject/Popup/popup-box`, `popup-in-box`, `popup-use-button`, `Settings/close-*`, Nunito Bold/ExtraBold, **GUI PRO Kit'ten 21 bayrak + edit ikonu** |
| `LB/LeaderboardPanel.prefab` | 25 246 | PanelView + ScrollList. İçinde NameEditDialog örneği var: 21 nesne silinmiş (:24872-24893), 68 bayrak hücresi eklenmiş (`Flags/*.png`). Ayrıca `top-box`, `leader-backbox`, `seperator`, `rank`, `brain-score-star`, `Part 1/Gameplay/settings.png`, `part 2/01_leaderboard_title_icon.png`, GUI PRO Kit `Label_BasicLabel02_White.png` ve `ThisProject/Leaderboard/image (30).png` (309 KB, **şüpheli:** referans ekran görüntüsü) |

- Panel, Sticker Patch sahnesinde prefab örneği olarak duruyor (`StickerPatchPrototype.unity:70700-70707`). Alan boşsa BrainApp onu Resources'tan yaratıyor (`StickerPatchBrainApp.cs:698-706`).
- Sanat: 68 bayrak (529 KB) ve 12 UI sprite'ı (ThisProject/Leaderboard, 452 KB, aynılarının `LB/Ui/` kopyası da var). `medal-gold/silver/bronze` ve `star-icon` prefab'larda kullanılmıyor (şüpheli).
- Nunito Bold/ExtraBold SDF dinamik atlas kullanıyor (`m_AtlasPopulationMode: 1`), TTF dosyası da gerekiyor. `Resources/Fonts`'ta farklı GUID'li ikinci bir kopya var. Prefab'lar `UI/NunitoSans-Medium/` altındakini kullanıyor.
- Kaynak dikey: CanvasScaler 1080×2400 (`StickerPatchPrototype.unity:2503`), satır 1000×108 (RowView:8-9).

## 2. Bağımlılıklar

### 2.1 StickerPatch'e özgü tipler
- **`StickerPatchBrainProfile`** (`Load`, `totalBrainScore`, `lastSessionBrainScore`, `GetBrainLevelName`): LocalBackend:41, :60 · Fake:51-61, :246-258 · PanelView:89, :127, :135, :141 · Leaderboard.cs:121, :147.
- **`StickerPatchPuzzleResult`** (`completed`, `isDaily`, `brainScoreGain`, `elapsedSeconds`, `mistakes`, `hint/undo/erase`, `dailyDayKey`): Types:19-42 · Leaderboard.cs:99-115, :136.
- **`StickerPatchDailySchedule.GetDayKey`** (`yyyyMMdd`, **yerel saat** `DateTime.Now`): Leaderboard.cs:137, :154 · Fake:185.
- **GameAnalytics:** `StickerPatchAnalytics.TrackLeaderboardSubmitted` çağrılıyor (Leaderboard.cs:139). İçi yalnız `Debug.Log` (`SP/StickerPatchAnalytics.cs:175-190`), ama dosyada `using GameAnalyticsSDK;` (:2) olduğu için derleme bağımlılığı doğuruyor. Çağrı atılacak.
- **UI yardımcıları:** `StickerPatchUiClickFeedback` (PanelView:69, :80, :253 · RowView:35, :39 · FlagPicker) → `StickerPatchGameSettings.PlayUiClick` + `StickerPatchButtonPressFeedback`, yaklaşık 780 satırlık zincir. `StickerPatchUiSprites.ApplyPanelImage` çalışmada 1×1 beyaz doku üretiyor (ScrollList:643, FlagPicker:284). `StickerPatchPopupUiSprites` (FlagPicker:74, Factory:25). Yerlerine T-005'in buton geri bildirimi ve prefab'ta atanmış sprite gelecek.
- **Dışarıdan kullananlar (taşınmaz):** BrainApp (:22, :689-718, :2008-2025, :3107-3117, :3211), HomeBottomNav, MenuFeatureTutorialSteps (:48, `"Leaderboard Scroll View"` adıyla hedef alıyor), editor kurulumcuları.

### 2.2 Kayıt yeri
Yalnız **PlayerPrefs, 7 anahtar**, dosyaya kayıt yok:
- `StickerPatchLeaderboard.PlayerId` / `DisplayName` / `CountryCode` (Leaderboard.cs:5-7)
- `StickerPatchFakeLeaderboard.BestRank` / `Score` / `PendingClimbFrom` / `LastSeenRank` (Fake:13-16)

Bizde karşılığı T-006 `SaveService` (JSON) ve GDD §14 `BestTimesByEvent`.

### 2.3 Statik sınıflar ve Find kullanımı
- **8 statik sınıf:** cephe (statik, dışarıdan atanabilen `Backend` :14-26), FakeLeaderboard, CountryFlags (sprite önbelleğiyle), Typography (font önbelleğiyle), UiSprites, Comparer, NameEditDialogFactory. Dolaylı olarak UiClickFeedback ve GameSettings. `Instance` singleton yok.
- **Çalışma zamanında arama:** PanelView isim listesiyle derin arama yapıyor (:9-18, :176), bütün Canvas'ı tarıyor (:202-213), butonları üzerindeki yazıdan buluyor: "Confirm/Ok/Cancel/Later" (:20-21, :223-224, :404-437). Ayrıca `GetComponentInChildren` yedekleri (:156-158). ScrollList `content.Find("Row Layout")` (:292, :619). FlagPicker hücreleri adlarından okuyor: `"Flag XX"`, `"Border"`, `"Inner/Flag"` (:153-159, :254).
- Bizim kurallarda (§9) bunların hepsi yasak. Hepsi `[SerializeField]` ve `[Inject]` olacak.

### 2.4 Çalışma zamanında UI kurma — kural §3.1 İHLALİ
1. `NameEditDialogFactory.Create` pencerenin tamamını kuruyor (Factory:10-43). PanelView prefab bulamayınca bunu çağırıyor (:343).
2. `FlagPickerView.EnsureBuilt` kaydırma alanını ve 68 hücreyi kuruyor (:51-127). Katalog farklıysa hücreleri `DestroyImmediate` edip yeniden kuruyor (:185-241, :274-322). PanelView:272 bileşeni `AddComponent` ile ekliyor.
3. `TopThreeView.EnsureBuilt` podyumu ve ayracı kuruyor (:67-103). Bu kod zaten ölü.
4. ScrollList `"Row Layout"`u (:301-308) ve nokta ayracını 3 noktasıyla (:626-645) kuruyor. Content'teki VerticalLayoutGroup ve ContentSizeFitter'ı kapatıp satırları elle yerleştiriyor (:542-558).
5. **Stil ezme:** `RowView.Bind` her satırda sprite, font, renk ve boyutu yeniden yazıyor (:60-110). Kullanıcının Inspector'da yaptığı düzenleme oyunda ezilir.

İzinli olan: satırların prefab'tan `Instantiate` edilmesi (ScrollList:496). Bizde bu Zenject factory olur.

**Diğer:** Her şey (prefab, font, sprite, bayrak, isim JSON'u) Resources ile yükleniyor ve editör ile build farklı yol izliyor (`#if UNITY_EDITOR` + AssetDatabase: CountryFlags:133-150, Typography:100, UiSprites:47). Yani editörde çalışıp build'de bozulabilir. Namespace ve asmdef yok. Metinler `TextMeshProUGUI`; bizdeki `TMP_Text` bunun üst sınıfı, serialize bağları kopmaz.

## 3. Taşıma planı

### 3.1 Olduğu gibi alınacaklar
- **Sanat (.meta ile):** 68 bayrak, `ThisProject/Leaderboard` sprite'ları (`image (30).png` hariç), `ThisProject/Popup/*`, `Settings/close-*`, Nunito Bold/ExtraBold SDF + TTF (yalnız `UI/NunitoSans-Medium` kopyası), `FakeLeaderboardNames.json` (1000 isim).
- **Veri şekilleri:**
  - `Row`: rank, displayName, countryCode, isPlayer, playerId
  - `Standing`: rank, total, percentile
  - `SubmitResult`: accepted, improvedPB, previousRank, newRank
  - `Page`: rows, playerRank, windowFirst/Last, climbFromRank
- **Algoritmalar:** ilk 3 + oyuncu çevresi penceresi (Fake:191-234), tohumlu bot kimliği (:347-358), oyuncuyu ortalayan kaydırma hesabı (ScrollList:455-470), tırmanma sırası (:177-237), ülke kodu normalizasyonu (CountryFlags:75-84), isim kırpma (Leaderboard.cs:66-75), 16 karakter sınırı.
- **Prefab'lar başlangıç noktası:** Row ve NameEditDialog doğrudan kullanılabilir. Panel yalnız görsel referans (§3.5-6).

### 3.2 Uyarlanacaklar
- **Entry:** `brainScoreGain/mistakes/undo/erase/hint/brainLevelName` yerine `eventId` (T-004 `RaceEventCatalog` id'si), `timeMs` (int: float karşılaştırma ve JSON yuvarlama derdi olmasın), `achievedAtUtcTicks`, playerId/displayName/countryCode.
- **Karşılaştırma:** önce süre (küçük iyi), sonra erken tarih, sonra playerId. Types:113-167'nin yerini alır.
- **Anahtar:** `dayKey` yerine `(eventId, period, periodKey)`.
  - Günlük: `yyyyMMdd`, **UTC**
  - Haftalık: ISO hafta `YYYY-Www`, UTC
  - Tüm zamanlar: `all`

  Kaynak yerel saati kullanıyordu. Bizde saat `ClockService`'ten gelir (T-006).
  Not: PDF §1.3 orijinalde günlük/haftalık/sezonluk sıralama olduğunu söylüyor (ödül vermiyorlar). "Sezonluk" sekmesi açık karar.
- **Gönderim:** Yarış bitince tek çağrı üç döneme birden yazar. Her dönemde yalnız kişisel rekor (PB) güncellenir.
- **Botlar:** Sıranın skordan hesaplanması ve botların skorunun oyuncuya göre kayması (Fake:34-37, :137-141) süreyle çalışmaz. Yerine her dönem için sabit bir bot listesi:
  - Tohum: `hash(eventId, period, periodKey)`.
  - N bot, süreleri config'teki dağılımdan (etkinlik başına alt sınır, medyan, üst sınır).
  - Oyuncunun sırası = kendisinden hızlı bot sayısı + 1.
  - N ve dağılım **denge sayısı**. `LeaderboardConfig` SO'da durur, değerleri kullanıcı verir (DECISION). 4692/4685 gibi sabitler koda girmez.
- **Panel özeti:** "Brain Score + harf + Daily Challenge" (PanelView:131-144) yerine etkinlik seçici (100m, 200m, 400m, 60mH …), dönem sekmesi (Günlük / Haftalık / Tüm zamanlar) ve "En iyi sürem". Skor metni `displayScore.ToString()` (RowView:101) yerine "9.58" gibi süre biçimi (testli biçimleyici).
- **Kimlik ve tırmanma kaydı:** PlayerPrefs'ten SaveService'e geçer. Son görülen sıra `(eventId, period)` başına tutulur. Varsayılan "Player"/"US" (Leaderboard.cs:63, :85) için karar gerekiyor.
- **Animasyon:** coroutine (ScrollList:177-237, :472-488) yerine DOTween, süreler `UiMotionConfig`'te.
- **Arayüz:** Online'a hazır olsun diye backend asenkron (Task ya da callback) kurulmalı. Bugün senkron. **Şüpheli:** kaynakta buna dair bir şey yok, bu bir öneri.

### 3.3 Atılacaklar
LocalBackend, FakeLeaderboard'un skor matematiği, TopThreeView, NameEditDialogFactory, Typography, UiSprites, CountryFlags'in yükleyici kısmı, emoji bayrak, `CanSubmitDaily`/`TrySubmitDaily`/`GetTodayTop`/`FormatSubmitLine` (kaynakta çağıranı yok ya da kendi dosyası dışında yok), `DayStore`, Analytics çağrısı, 4 editor dosyası, GUI PRO Kit ve CasualFantasy referansları, kullanılmayan medal/star sprite'ları, Resources klasörü kullanımı.

### 3.4 Zenject'e çevirme
Alan kodu `SpeedStars.Meta.Leaderboard` (`Scripts/Meta/Leaderboard/`), view'lar `SpeedStars.UI.Leaderboard` (`Scripts/UI/Leaderboard/`). İkiye bölme kural §1'deki klasör ayrımına uyduğu için öneriliyor. İstenirse hepsi Meta'da kalabilir.

| Sticker Patch | Speed Stars | Bağlama (süpervizör, installer) |
|---|---|---|
| `IStickerPatchLeaderboardBackend` | `ILeaderboardBackend` (Submit / GetPage / GetStanding, sorgu = eventId + period) | — |
| LocalBackend + Fake | `LocalLeaderboardBackend` (`ISaveService`, `IClock`, `LeaderboardBotRoster`) | `Bind<ILeaderboardBackend>().To<LocalLeaderboardBackend>().AsSingle()`. Online gelince yalnız bu satır değişir |
| Fake'in bot kısmı | `LeaderboardBotRoster` (saf C#, tohumlu) | AsSingle |
| `StickerPatchLeaderboard` (statik) | `LeaderboardService` (`SubmitRaceResult(eventId, timeMs)`, `GetPage`, tırmanma) + `PlayerIdentityService` | AsSingle |
| `GetDayKey` | `LeaderboardPeriod` enum + `PeriodKeys` (saf) | — |
| CountryFlags | `CountryFlagCatalog` ScriptableObject (kod → Sprite, sıralı) | BindInstance |
| Fake sabitleri | `LeaderboardConfig` ScriptableObject | BindInstance |
| PanelView / RowView / ScrollList / FlagPicker / Dialog | `HighScoresPanelView`, `LeaderboardRowView`, `LeaderboardListView`, `FlagPickerView` + `FlagCellView`, `NameEditDialogView`, `PeriodTabsView`, `EventSelectorView` | Sahne installer'ında `BindFactory<…>().FromComponentInNewPrefab(rowPrefab)` |

**Yarış sonucu:** `RaceController.LastRaceTime`'ı okuyan adaptör Assembly-CSharp'ta olmalı. Bunun iki sebebi var: asmdef'li bir assembly Assembly-CSharp'a referans veremez (`docs/TEMEL-SOZLESME.md:119`), ve bugün "bitti" olayı yok (`TEMEL-SOZLESME.md:277`).

### 3.5 Prefab ve asset taşıma yolu
1. **Önce hedefe TMP Essential Resources alınmalı.** Hedefte `Assets/TextMesh Pro` yok. Nunito materyali `TMP_SDF-Mobile.shader` GUID'ine bağlı (`fe393ace9b354375a9cb14cdbbc28be4`, `NunitoSans-Bold SDF.asset:2693`). TMP Essentials bu GUID'i sabit getirdiği için bağ kendiliğinden tutar.
2. **Sanatı .meta'sıyla birlikte dosya olarak kopyala.** GUID korunur, kopyalanan prefab'ların sprite ve font bağları kopmaz. Kaynaktaki ilgili 329 asset GUID'inden hedefte çakışan **0** (2026-10-28 taraması). Hedef klasörler: `Assets/Speed-Staes/UI/Leaderboard/Art/` ve `Assets/Speed-Staes/UI/Fonts/`. Resources'a konmamalı.
3. **Prefab'ları .meta'sıyla kopyala, içlerindeki 4 script GUID'ini yeni script'lerin GUID'ine çevir** (metin değiştirme, scene-integrator yapar):
   - PanelView `ada98cfa508d9e14aabce19cf9ef4ad8`
   - ScrollList `e8f239846366d5145bd48a1ace390a7f`
   - FlagPicker `74fcfbd3fbba8e049adf8d920dcce07c`
   - RowView `a0346711e7a8bf947b15c8a83dc9be58`

   Alan adı değişen yerde `[FormerlySerializedAs]` kullanılır.
4. **Ücretli paket referansları** (Row'da 1, Dialog'da 22, Panel'de 2): kopyalamadan önce ya lisans teyit edilmeli ya da sprite'lar ThisProject sprite'larıyla değiştirilmeli. **Şüpheli:** lisans projeye ya da kişiye bağlı olabilir. `Part 1/…/settings.png` ve `part 2/…title_icon.png` Sticker Patch'e özel sanat.
5. **Panel'deki iç içe diyalog düzleştirilmeli** (21 silme + 68 ekleme override'ı). Bayrak hücresi ayrı bir `FlagCell.prefab` olur ve ızgara factory ile dolar. Kaynaktaki diyalogda 24 "Flag" hücresi var, 21'i silinmiş. Kalan 3'ünün durumu **şüpheli**, okunmadı.
6. **Yön değişiyor:** kaynak dikey 1080×2400, hedef yatay 1920×1080 (`docs/YOL-HARITASI.md:35` T-001; PDF ekran görüntüleri de yatay). Panel yeniden yerleşmeli. `RowWidth 1000` sabiti kaldırılmalı (liste zaten prefab'tan ölçüyor, ScrollList:648-681).
7. Editor builder'ları taşınmaz. Prefab'lar bundan sonra elle düzenlenir.

## 4. Tahmini iş parçaları ve riskler

| # | Sahip | Tek cümle | owns_paths önerisi | Bağımlı |
|---|---|---|---|---|
| L-1 | systems-agent | Alan tipleri, yerel backend, tohumlu bot listesi, dönem anahtarları, `LeaderboardService` ve `PlayerIdentityService`. EditMode testleri: süre sırası, tohum tekrarlanabilirliği, sıra/pencere, UTC gün ve ISO hafta sınırı, PB'nin yalnız iyileşmesi, süre biçimi | `Assets/Speed-Staes/Scripts/Meta/Leaderboard/**`, `Assets/Speed-Staes/Tests/EditMode/Meta/Leaderboard/**` (test yeri T-007'de kesinleşir) | T-004, T-006, T-007 |
| L-2 | scene-integrator | TMP Essentials, Nunito, bayraklar, UI sprite'ları ve isim JSON'u .meta'larıyla taşınır; `CountryFlagCatalog.asset` ve boş `LeaderboardConfig.asset` kurulur | `Assets/TextMesh Pro/**`, `Assets/Speed-Staes/UI/Fonts/**`, `Assets/Speed-Staes/UI/Leaderboard/Art/**`, config yeri T-004'e göre (şüpheli) | T-004 |
| L-3 | ui-agent | Panel, satır, liste (DOTween tırmanma ve kaydırma), bayrak seçici + hücre, isim penceresi, dönem sekmesi ve etkinlik seçici view'ları. Find yok, çalışmada hiyerarşi kurma yok, stil prefab'ta | `Assets/Speed-Staes/Scripts/UI/Leaderboard/**` | L-1 (arayüz), T-005 |
| L-4 | scene-integrator | Row, FlagCell, NameEditDialog ve yatay HighScoresPanel prefab'ları kurulur (kopya + script GUID değişimi + yatay yerleşim), panel Home sahnesine konur ve bağlanır | `Assets/Speed-Staes/UI/Leaderboard/*.prefab`, `Assets/Speed-Staes/Scenes/Menu/Home.unity` | L-2, L-3, T-003, T-008 |
| L-5 | supervisor (+ systems-agent) | Installer bağlamaları, Home ve Results "High scores" butonları, yarış sonucu adaptörü (`LastRaceTime` → `SubmitRaceResult`) | `Assets/Speed-Staes/Scripts/Core/Installers/**`, adaptör Assembly-CSharp tarafında (yer şüpheli) | L-1..L-4, Faz 7 "bitti" olayı |

**Riskler**
1. **"Direkt alınacak" beklentisi:** UI ve sanat alınır, ama sıra, bot ve kayıt çekirdeği yeniden yazılır. Kabaca işin yarısı yeni kod (şüpheli tahmin).
2. **Kural temizliği:** 5 kural dışı UI kurma yeri ve isim/yazıyla aramalar temizlenmezse hook ve denetimde takılır.
3. **Lisans:** GUI PRO Kit ve GUI-CasualFantasy sprite'ları prefab'larda 25 yerde referanslı (şüpheli).
4. **Yön:** panel dikey tasarlanmış. Yatay yerleşim kullanıcının gözle onayını ister.
5. **TMP Essentials yoksa** fontlar pembe ya da boş görünür. Atlas dinamik olduğu için TTF de taşınmalı.
6. **Bot dağılımı denge sayısı:** kullanıcı vermeden konamaz. Yanlış dağılım oyuncuyu ya hep 1. ya hep son yapar.
7. **Saat:** cihaz saati geri alınınca günlük/haftalık sekme eski döneme döner. ClockService'e bağlı (T-006).
8. **"Bitti" olayı yok** (`TEMEL-SOZLESME.md:277`): gönderim adaptörü kırılgan kalır. Results → High scores bu yüzden bekleyebilir.
9. **Sahte oyuncular gerçek gibi görünebilir** ("#1 worldwide" Fake:270, 4692 oyuncu). Mağaza politikası açısından şüpheli. Online gelene kadar etiket kararı gerekiyor.
10. **GUID:** bugün çakışma yok. Aynı asset ileride başka yoldan (ör. Nunito'yu yeniden indirerek) gelirse çift kopya olur. Tek kopya kuralı konmalı.

Kapsam dışı: "Players" butonu (çok oyunculu, `YOL-HARITASI.md:101`). PDF'in ana ekranında "Players" ve "High scores" butonları yan yana duruyor (PDF s.3 ve s.5'teki görseller).
