---
id: T-022
title: PlayMode ekran goruntusu testi - Home ve Race, gercek degerlerle (UI dahil)
status: merged
owner: test-agent
depends_on: [T-018]
owns_paths:
  - "Assets/Speed-Staes/Tests/PlayMode/**"
touches_scene: false
branch: agent/menu-karakter
commits: []
tests:
  - "Assets/Speed-Staes/Tests/PlayMode/ScreenshotTests.cs"
analytics:
  - "yok: projede analytics yok"
needs: []
started: 2026-10-28
finished: 2026-10-28
blocked_reason:
---

## Hedef
Süpervizör ekranı kopyada batchmode ile görüyor ama Play dışında (`Tools/Editor/Builders/ShotTool.cs`): view'lar enjekte olmadığı için sayılar Inspector
varsayılanı, menü karakterinin `MenuCharacter` pozu uygulanmamış. Bu task **PlayMode**'da Home'u ve (bilet verip) Race'i açıp birkaç kare sonra
UI dahil ekran görüntüsü alan bir test yazar (kanıt dosyası; test çekim başarısızsa kırmızı).

## Kabul kriterleri
- [ ] `[UnityTest]` (ör. `Shots_HomeAndRace`): Home yüklenir, ~1 sn beklenir → PNG; Play akışı (bilet ver, Use Entry Ticket) → Race, ~1 sn → PNG.
      Çekim: `Camera.main` + overlay canvas'ları geçici Screen Space - Camera (ShotTool'daki yöntem) + RenderTexture → ReadPixels, 2400×1080. Çekimden
      sonra canvas'lar eski moduna döner.
- [ ] Çıktı klasörü komut satırından: `-shotDir <klasör>` (`Environment.GetCommandLineArgs`); verilmemişse test **atlanır** (`Assert.Ignore`) — normal test
      koşusunda dosya üretmesin.
- [ ] Kayıt koruması T-018'deki gibi (kullanıcı/kopya kaydı geri yazılır).
- [ ] Mevcut 9 PlayMode testi bozulmaz.

## Çalışma kuralları
- Çalışma klasörü MUTLAK: `D:\Pundun\speed-starts-fix1` (worktree, dal `agent/menu-karakter`). Ana klasöre yazma.
- Önce oku: `Tests/PlayMode/**` (T-018), `Tools/Editor/Builders/ShotTool.cs` (yöntem), `docs/tasks/UI-v2-ISIMLER.md`.
- Unity/batchmode/verify YASAK, `git commit` YASAK, `.meta` yazma. Derleme kontrolü scratchpad'de Roslyn.
- Bitince `status: review`, `tests:` dolu, `## Çalışma notu` 5 satır.

## Çalışma notu
- ScreenshotTests.cs: Shots_HomeAndRace; -shotDir yoksa Assert.Ignore, varsa Home_play.png ve Race_play.png (2400x1080) yazar.
- Overlay canvas'lar gecici ScreenSpaceCamera, finally ile eski mod/kamera/planeDistance geri; kayit SaveFolderSnapshot + bellek alanlari finally'de geri.
- Test govdesi tek UnityTest (SetUp yok): -shotDir yokken sahne yuklenmez, kayda dokunulmaz.
- Roslyn (csc 4.3, C# 9) ile PlaySmokeSupport ile birlikte derlendi: 0 hata. Unity'de kosulmadi.
- Bilinen risk: batchmode'da -nographics olmamali; Race'te Camera.main yoksa test kirmizi.

