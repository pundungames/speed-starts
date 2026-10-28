---
id: T-033
title: Yaris sonu ekrani v1 (Oyun_Bitis_UI_v1) + kisisel rekor kaydi
status: review
owner: ui-agent
depends_on: [T-029]
owns_paths:
  - "Assets/Speed-Staes/UI/Art/Results/**"
  - "Assets/Speed-Staes/Scripts/UI/Race/Results*.cs"
  - "Assets/Speed-Staes/Scripts/UI/Race/RaceFlow*.cs"
  - "Assets/Speed-Staes/Scripts/UI/Race/RaceViewContracts.cs"
  - "Assets/Speed-Staes/Scripts/Meta/Records/**"
  - "Assets/Speed-Staes/Scripts/Meta/Contracts/RecordContracts.cs"
  - "Assets/Speed-Staes/Tools/Editor/Builders/Ui/UiResultsPrefabs.cs"
  - "Assets/Speed-Staes/Tools/Editor/Builders/Ui/UiPrefabBuilder.cs"
  - "Assets/Speed-Staes/Tests/EditMode/Meta/Records/**"
  - "Assets/Speed-Staes/Tests/EditMode/UI/Race/**"
  - "Assets/Speed-Staes/Tests/PlayMode/HomeRaceButtonSmokeTests.cs"
touches_scene: true
branch: agent/T-033-yaris-sonu
commits: []
tests:
  - "Assets/Speed-Staes/Tests/EditMode/Meta/Records/PersonalBestServiceTests.cs"
  - "Assets/Speed-Staes/Tests/EditMode/UI/Race/ResultsModelTests.cs"
analytics:
  - "yok: projede analytics yok"
needs:
  - tip: WIRING
    durum: open
    not: "ProjectInstaller: IPersonalBests AsSingle. Exact line, next to the other Meta services (needs ProgressRepository, already bound): Container.Bind<IPersonalBests>().To<SpeedStars.Meta.Records.PersonalBestService>().AsSingle();  -- RaceFlow takes it with [InjectOptional], so the Race scene runs without it (PERSONAL BEST shows '--', nothing is saved) until bound."
  - tip: SCENE
    durum: open
    not: "Rebuild UI prefabs with overwrite=true (UiPrefabBuilder.BuildAll): new Rows/ResultsTableRow.prefab + rebuilt Screens/ResultsScreen.prefab (same GUID). The Race scene's ResultsScreen instance (RaceSceneInstaller.results) keeps its GUID but may carry overrides of the old fields (LeftPanel/..): check it after the rebuild, re-run the scene builder if needed. Not open for any further hand work."
started: 2026-10-28
finished: 2026-10-28
blocked_reason:
---

## Hedef
Yarış bitince açılan sonuç ekranı yeni sanatla (`C:\Users\Monster\Downloads\Oyun_Bitis_UI_v1.zip`; açılmış hâli süpervizör scratchpad'inde
`bitis-ui\`: README.md = katmanlar + davranış, `layout.json` referans 1678×937, `approved_design.jpg`, `preview_no_text.jpg`; build.py
alınmaz). Kullanıcının attığı görüntü = approved_design. Var olan `ResultsScreenView` (Race sahnesi, `UiPrefabPaths.ResultsScreen`,
T-029'un "+N Mor Mücevher" satırı) bu sanata taşınır; sonuç akışı (`RaceFlow`/`RaceFlowLogic`) korunur.

Kararlar (README'deki "veri yoksa uydurma" kuralına uygun; süpervizör kararı):
- **Sol üst**: Nakit bakiyesi (`IWallet.Cash`).
- **PERSONAL BEST**: yeni `IPersonalBests` (Meta): yarış bitince etkinlik başına en iyi süre kaydı (`PlayerProgress.bestTimes` zaten var,
  şimdiye kadar kimse yazmıyordu); aynı yarış iki kez yazılmaz (payoutId ile); "yeni rekor" yalnız gerçekten iyileşince (ilk koşu dahil)
  → kart punch + "NEW RECORD" etiketi. Kart: süre + etkinlik adı.
- **WORLD POSITION / PERCENTILE**: liderlik verisi yok (Faz 6) → değer "—", kartlar görünür. Sahte sayı yok.
- **Sonuç tablosu**: yalnız mevcut yarış; bugün tek koşucu var → yalnız oyuncu satırı (sıra `RaceOutcome.Rank`, ad
  `IPlayerSettings.PlayerName`, yoksa "YOU"; süre). İlk üçte kupa (altın/gümüş/bronz), oyuncu satırı vurgulu. Satırlar satır prefab'ı +
  content parent; rakip satırları mekanik koşucu listesi verince dolacak (Malik / Faz 7) — kod bunu bekleyecek şekilde liste alır.
- **BEST OF THE DAY**: kaynak veri yok → panel boş durum metni ("No races today" gibi kısa İngilizce) ya da gizli; sahte isim yok.
- **Butonlar**: Race Again (var olan giriş kapısı akışı, bilet kuralı aynı), Main Menu (Home'a dönüş, var olan çıkış akışı) çalışır;
  Watch Replay (tekrar sistemi yok), Stats (ekran yok), Practice (`training` oynanamaz), High Scores (Faz 6) pasif (`nav_disabled_background`).
- Mor Mücevher ödülü (T-029) varsa ekranda kalır (uygun bir yere "+N" satırı).
- Dimscreen sahnenin üstünde, UI'ın altında.

## Kabul kriterleri
- [ ] Sanat `UI/Art/Results/`'a PNG; prefab builder `layout.json`'u `UiArt` ile okur; kod kalıcı UI kurmaz; satır prefab'ı ayrı
- [ ] `IPersonalBests` + saf sonuç modeli (kart metinleri, satırlar, yeni rekor) EditMode testli; sabit süre assert edilmez
- [ ] PlayMode duman testi (yarışa gir, bitir, sonuç, Race Again / çıkış) yeşil; gerekirse alan adları güncellenir
- [ ] Legacy Text, İngilizce, best fit; DOTween yalnız çekirdek (TUZAKLAR)
- [ ] needs listesinde open WIRING/SCENE yok

## Geri bildirim
GORSEL  : ekran açılışta kartlar sırayla gelir (UiMotionConfig stagger), yeni rekorda kart punch
SES     : yok (ses sistemi yok)
DOKUNSAL: yok
ZAMAN   : yok
KAMERA  : yok

## Kapsam disi
- Liderlik tablosu / dünya sırası (Faz 6), tekrar sistemi, istatistik ekranı, rakip koşucular (Malik)

## Calisma notu
- Yapilan: `IPersonalBests` + `PersonalBestService` (PlayerProgress.BestTimes uzerinde, race id ile tek yazim, centisecond karsilastirma); saf `ResultsModel` (kart metinleri, satirlar, kupa, yeni rekor); `ResultsScreenView` + `ResultsTableRowView` yeniden yazildi (stagger acilis, yeni rekorda kart punch, 4 pasif buton sprite-swap disabled); `UiResultsPrefabs` builder'i layout.json'dan (tablo/daily extras dahil) screen + row prefab kurar; 41 PNG `UI/Art/Results/`'e kopyalandi.
- `RaceFlowLogic/RaceFlow`: IPersonalBests, IWallet, IPlayerSettings opsiyonel (null = o kart bos/--); `IResultsView.Present(ResultsData)` (RaceResult kaldirildi). WORLD POSITION / PERCENTILE / BEST OF THE DAY gercek veri yok: `--` / "No races today".
- Zorlandigim: yeni .cs/.png dosyalarinin .meta'lari yok (yazma yetkisi yok): kopyada derlenip .meta GeriAl ile alinmali (TUZAKLAR .meta maddesi). Prefab'ta alan baglari builder ile; `fileID: 0` grep'i prefab uretildikten sonra yapilmali (burada prefab yok).
- Birakilan: eski `UiScreenPrefabs.Results` + `UiViewBinder.BindResults` + `HighScoreRow` ile `ResultRowView` olu kod (bu task'in disinda dosyalar; HighScores ekrani hala kullaniyor olabilir) -- temizlik backlog. Rakip satirlari `ResultsInput.Others` listesinde hazir (bos).
- Testler yazildi, KOSULMADI (merkezi dogrulama): PersonalBestServiceTests, ResultsModelTests, RaceFlowLogicTests guncel + 2 yeni test.
