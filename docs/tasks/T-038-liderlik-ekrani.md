---
id: T-038
title: Faz 6 liderlik ekrani (Leaderboard_UI_v1) - filtreler (bolge+arama, donem, etkinlik), tablo + YOU + FIND ME, durumlar; Home ve Sonuc'tan acilir
status: merged
owner: ui-agent
depends_on: [T-037]
owns_paths:
  - "Assets/Speed-Staes/UI/Art/Leaderboard/**"
  - "Assets/Speed-Staes/Scripts/UI/Leaderboard/**"
  - "Assets/Speed-Staes/Scripts/UI/Home/HomeScreenView.cs"
  - "Assets/Speed-Staes/Scripts/UI/Race/ResultsScreenView.cs"
  - "Assets/Speed-Staes/Scripts/UI/Race/ResultsModel.cs"
  - "Assets/Speed-Staes/Tools/Editor/Builders/Ui/UiLeaderboardPrefabs.cs"
  - "Assets/Speed-Staes/Tools/Editor/Builders/Ui/UiPrefabBuilder.cs"
  - "Assets/Speed-Staes/Tools/Editor/Builders/UiPrefabPaths.cs"
  - "Assets/Speed-Staes/Tools/Editor/Builders/Scene/SceneWiring.cs"
  - "Assets/Speed-Staes/Tests/EditMode/UI/Leaderboard/**"
  - "Assets/Speed-Staes/Tests/EditMode/UI/Race/**"
  - "Assets/Speed-Staes/Tests/PlayMode/**"
touches_scene: true
branch: agent/T-038-liderlik-ekrani
commits: [bd5bad6, a3b1538, 26101f8, 8dfd876]
tests:
  - "Assets/Speed-Staes/Tests/EditMode/UI/Leaderboard/LeaderboardPresenterTests.cs"
analytics:
  - "yok: projede analytics yok"
needs:
  - tip: SCENE
    durum: closed
    not: "Prefab builder overwrite=true (UiPrefabBuilder.BuildAll): the old placeholder HighScoresScreen.prefab is kept otherwise; new Rows/LeaderboardRow, LeaderboardCountryRow, LeaderboardDistanceOption prefabs; then rebuild Home (HomeScreenView.highScoresScreen wired as before) and Race (SceneWiring.WireRace now places the HighScoresScreen instance and wires ResultsScreenView.highScoresScreen)"
started: 2026-10-30
finished: 2026-10-30
blocked_reason:
---

## Hedef
Ana ekrandaki High Scores (madalya) butonu ve yarış sonu ekranındaki High Scores butonu Leaderboard ekranını açar (bugün Home'da yer tutucu
`HighScoresScreen`; aynı `UiPrefabPaths.HighScoresScreen` yolu bu ekran olur; sonuç ekranındaki buton T-033'te pasif). Sanat:
`C:\Users\Monster\Downloads\Leaderboard_UI_v1.zip` (açılmış hâli süpervizör scratchpad'inde `leader-ui\`: README.md, **LEADERBOARD_MASTER.md
= bağlayıcı davranış**, layout.json, `PNG\` (125 sprite, 16 bayrak + unknown), 01_main_screen.jpg / 02_open_filters.jpg /
03_system_states.jpg / asset_overview.jpg; build.py alınmaz). Veri: T-037'nin `ILeaderboard` sözleşmesi — **`docs/tasks/T-037-liderlik-cekirdegi.md`'deki
kod bloğu aynen**; T-037 paralel yazılıyor, sen bu imzaya göre yaz (dosya entegrasyonda gelir; kendi worktree'nde derleme yok zaten).

Ekran (MASTER.md'ye göre): sol üst Nakit + Back; LEADERBOARD başlığı; 3 filtre alanı: bölge (GLOBAL / ülke; popup: arama alanı (legacy
InputField), GLOBAL + bayraklı ülke listesi, kaydırma, "NO COUNTRIES FOUND"), dönem (ALL TIME / THIS MONTH / THIS WEEK / TODAY popup, anında
uygular), etkinlik (modal: SPRINTS / HURDLES / RELAYS sekmeleri + mesafe seçenekleri config'ten — `EventSelectLayout` eşlemesi (T-024),
APPLY seçim yokken pasif, CANCEL/X değiştirmez, boş kategoride "NO EVENTS AVAILABLE"); tablo RANK / PLAYER (bayrak + ad) / TIME
(`10.42 s`) / SET ON (`28 OCT 2026`, UTC gün, İngilizce ay); satır prefab'ı + ScrollRect, sona yaklaşınca sonraki sayfa; altta sabit YOU
satırı (kırmızı kontur) + FIND ME (kendi satırına kaydırır; kayıt yoksa "NOT RANKED", süre/tarih "—", FIND ME pasif); durumlar: LOADING
(dönen gösterge), NO RESULTS YET / COMPLETE A RACE TO SET A TIME, COULDN'T LOAD LEADERBOARD + RETRY. Aynı anda tek popup; Back önce popup'ı
kapatır. Varsayılan: Global, All Time, açıldığı yerdeki yarış (Sonuç'tan geliyorsa o etkinlik; Home'dan seçili etkinlik), yoksa ilk
oynanabilir etkinlik. Ülke adı/bayrak listesi Ayarlar'daki bayrak listesiyle aynı kaynak (`SettingsConfig`), sprite'lar bu paketten.
"Sample data" yazısı konmaz.

**Sonuç ekranı kartları** (T-033'te "--"): WORLD POSITION = `TryGetStanding` → `#rank`; PERCENTILE = `TOP N%` (rank/total, yukarı yuvarla,
en az 1); BEST OF THE DAY = `TryGetBestOfDay` (ad + süre); veri yoksa bugünkü gibi "--" / boş durum.

## Kabul kriterleri
- [ ] Saf `LeaderboardPresenter` (filtre durumu, requestId ile eski yanıtı yok sayma, sayfalama birleştirme tekrar yok, YOU/FIND ME, durum
      seçimi, etiket metinleri, tarih/süre biçimi) EditMode testli (sahte ILeaderboard)
- [ ] Prefab'lar builder'dan; dokunma alanları görünür bölge (`UiArt.AlphaBounds`), dekor `raycastTarget` kapalı; legacy Text; DOTween çekirdek
- [ ] PlayMode: Home High Scores → Leaderboard → Back (gerçek ışınlı Tap; var olan `HighScores_Button_OpensScreen_AndBackReturnsHome`
      yeşil kalır) + ekran görüntüsü testine `Home_leaderboard`
- [ ] needs listesinde open WIRING/SCENE yok

## Geri bildirim
GORSEL  : filtre/popup açılışı UiMotionConfig; FIND ME kaydırması yumuşak; satır seçimi punch yok (MASTER: profil menüsü yok)
SES     : yok
DOKUNSAL: yok
ZAMAN   : yok
KAMERA  : yok

## Kapsam disi
- Liderlik verisi (T-037), sunucu, profil menüsü

## Calisma notu
- Saf cekirdek: LeaderboardPresenter (requestId, sayfa birlestirme, YOU/FIND ME), LeaderboardEventGroups/Picker, LeaderboardCountryList, LeaderboardFormat, ResultsLeaderboardCards; 30+ EditMode testi.
- Gorunum: LeaderboardScreenView (+Row/Choice/PopupPanel); region/period/event pencereleri navigator popup'i, Back once pencereyi kapatir (UiPanel closeButton).
- Builder: UiLeaderboardPrefabs (3 satir prefab'i + ekran), UiPrefabBuilder'da tek cagri; ekran HighScoresScreen yolunda. Sonuc ekrani: 3 kart ILeaderboard'dan, High Scores butonu ekrani Push eder; Race sahnesine SceneWiring yerlestirir.
- Zorlandigim: layout.json'da ekran konumu yok; konumlar 01-03 onizlemelerinden olculdu (builder'da sabitler), gorsel kontrol gerekir.
- Birakilan: Unity'de derleme/prefab uretimi yok (kurala gore); Home_leaderboard ekran goruntusu ve yeni PlayMode testi kosulmadi.
