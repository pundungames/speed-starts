---
id: T-018
title: PlayMode duman testleri - Home/Race buton akislari (ac/kapat, giris, bitis, ana menu)
status: merged
owner: test-agent
depends_on: [T-015, T-016]
owns_paths:
  - "Assets/Speed-Staes/Tests/PlayMode/**"
touches_scene: false
branch: agent/ui-v2
commits: []
tests:
  - Assets/Speed-Staes/Tests/PlayMode/HomeRaceButtonSmokeTests.cs
  - Assets/Speed-Staes/Tests/PlayMode/PlaySmokeSupport.cs
  - Assets/Speed-Staes/Tests/PlayMode/SpeedStars.PlayTests.asmdef
analytics:
  - "yok: projede analytics yok"
needs: []
started: 2026-10-28
finished: 2026-10-28
blocked_reason:
---

## Hedef
Kullanıcı: "sen test koşmuyor musun? açılan UI'lar kapat'a basınca kapanmıyor." EditMode testleri mantığı ölçüyor, tıklama akışını değil. Bu task
gerçek sahneleri (Build Settings: `Scenes/Menu/Home.unity`, `Scenes/Game/Race.unity`) PlayMode'da açıp butonlara basan duman testleri yazar.
Süpervizör bunları doğrulama kopyasında batchmode'da koşturur (`-runTests -testPlatform PlayMode`).

## Kabul kriterleri
- [ ] Ayrı asmdef `Assets/Speed-Staes/Tests/PlayMode/SpeedStars.PlayTests.asmdef` (PlayMode, `UNITY_INCLUDE_TESTS`, nunit, TestRunner; referans:
      SpeedStars.Core/Meta/UI, Zenject gerekirse). Asmdef'li test Assembly-CSharp'ı (Bridge, Installers) göremez → test sahne nesnelerini **UI tipleriyle**
      bulur (`Object.FindObjectOfType<HomeScreenView>(true)` testte serbest; ürün kodunda değil).
- [ ] Testler (`[UnityTest]`, `SceneManager.LoadScene` + bir-iki kare bekleme; buton `onClick.Invoke()`; panel açık/kapalı `gameObject.activeSelf` +
      animasyon süresi kadar bekleme ya da `UiPanel` durumu):
      1. Home açılır, HomeScreen aktif, konsolda hata yok (`LogAssert.NoUnexpectedReceived`).
      2. 7 gün (`DailyReward`) → popup açılır → `closeButton` → kapanır.
      3. HighScores ve EditRacer (Cosmetics) ekranı → açılır → Back → Home'a döner.
      4. RaceSelect → EventSelect açılır → Back → kapanır.
      5. Play → (bilet yoksa) EntryGate açılır → Close → kapanır. Bilet testi için kayıt: test başında `persistentDataPath/save` altında test kaydı
         kullanılmaz — `ProgressRepository`/`IEntryTickets` Zenject ProjectContext'ten alınıp test içinde bilet verilebilir (`ProjectContext.Instance.Container`).
      6. Bilet varken Play → Use Entry Ticket → Race sahnesi yüklenir; Race'te Results ekranı açılması için RaceFlow'un Finished yolunu test etmek
         Bridge'e (Assembly-CSharp) bağlı → `IRaceSession`'ı Container'dan alıp sahte bitiş yoksa bu adımı **atla ve nedenini yaz**; Exit butonu → Home.
- [ ] Testler kullanıcının gerçek kaydını SİLMEZ/EZMEZ (kopya ayrı productName ile koşar ama yine de test kendi verisini temizler).
- [ ] İsim sözleşmesi: `docs/tasks/UI-v2-ISIMLER.md` (hiyerarşi ve alan adları).

## Çalışma kuralları
- Çalışma klasörü MUTLAK: `D:\Pundun\speed-starts-uiv2` (worktree, dal `agent/ui-v2`). Ana klasöre yazma.
- Paralelde T-015 (view'lar), T-016 (builder), T-017 (Bridge), T-019 (sahne builder) yazılıyor; onların koduna yazma, API'yi oku.
- Unity/batchmode/verify YASAK, `git commit` YASAK, `.meta` yazma. Derleme kontrolü scratchpad'de Roslyn ile yapılabilir.
- Bitince `status: review`, `tests:` dolu, `## Çalışma notu` 5 satır (atlanan adımlar ve nedeni).

## Çalışma notu
- Yazildi: SpeedStars.PlayTests asmdef (PlayMode) + 9 [UnityTest] (Home acilis, 7 gun, HighScores, EditRacer, EventSelect, Play biletsiz, Play biletli->Race, Race Exit, Race bitis->Results->MainMenu). Gercek sahneler Home/Race.
- View alanlari private SerializeField oldugu icin reflection ile adla baglanir (sozlesme adlari + UiPanel.closeButton/backButton); T-015 henuz yoktu, mevcut kodla Roslyn derlemesi temiz.
- Buton once gorunur+interactable olana dek beklenir, sonra onClick.Invoke: bagli olmayan/basilamayan Close burada kirmizi verir.
- Kayit korumasi: Save klasoru bayt bayt geri yazilir, bellekteki bilet/zaman alanlari ve secili etkinlik geri konur; sabit denge sayisi yok (bilet 0/1, katalogdan bilet isteyen etkinlik secilir).
- Atlanan: Results'ta Race again + biletsiz (EntryGate prompt'u RaceSceneInstaller'da bagli degil); Race sahnesi konsol hatasi denetimi (Malik kodu yanlis alarm verebilir); EventSelect satir sayisi. Race bitisi DevForceFinish'e reflection ile bagli (editor/dev build).

