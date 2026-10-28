---
id: T-019
title: Sahne builder'i - Home'a canli karakter (prefab ornegi + kaide + kamera), RaceSceneInstaller telemetri alani
status: merged
owner: scene-integrator
depends_on: [T-017]
owns_paths:
  - "Assets/Speed-Staes/Tools/Editor/Builders/Scene/**"
touches_scene: true
branch: agent/ui-v2
commits: []
tests:
  - "gerekmiyor: editor builder'i; kabul batchmode kosusu + ekran goruntusu + PlayMode (T-018)"
analytics:
  - "yok: projede analytics yok"
needs: []
started:
finished:
blocked_reason:
---

## Hedef
Kullanıcı (28.10): "karakteri canlı canlı ana menüye koyacağız ve customization çalıştıracağız" — `SceneBuilder.BuildHome` Home sahnesine
`Assets/Speed-Staes/Prefabs/Character/HumanCharacterDummy_F.prefab`'ın **prefab örneğini** (`PrefabUtility.InstantiatePrefab`, bağ kopmaz — tema sahnelerindeki
`Object.Instantiate` hatası tekrarlanmaz, `reports/denetim-2026-10-28.md`) koyar: ekranın ortasında, tasarımdaki gibi (`docs/gdd/referans-ekranlar/
tasarim-ana-ekran-v2.png`) bir kaide (silindir, basit materyal) üstünde, kameraya dönük, tam boy görünür. Kamera: karakteri ortalayan perspektif,
UI (sol sütun, sağ kart, alt sıra) karakteri örtmez. Arka plan şimdilik gökyüzü rengi (dünya Serhat'tan sonra).
Örnekte `SpeedStars.Bridge.MenuCharacter` (T-017) bileşeni + bağları (T-017 çalışma notundaki tablo); koşu/girdi bileşenleri sahne override'ıyla
kapalı (prefab'a dokunulmaz). Kozmetik Faz 5'te bu örneği giydirecek.

## Kabul kriterleri
- [ ] Home'da karakter prefab örneği (bağlı), kaide, kamera; karakter ekranın ortasında, tam boy, UI ile çakışmıyor.
- [ ] `MenuCharacter` eklendi ve bağlandı; RunnerInput/RunnerBodyHUD/RunnerReplay örnekte kapalı (override) — T-017'nin kararına göre RunnerBody/PoseDriver.
- [ ] Race: `RaceSceneInstaller`'a süpervizör `telemetry` alanı ekliyorsa (IRaceTelemetry = aynı RaceSession adaptörü) bağla; alan yoksa dokunma
      (süpervizör installer'da adaptörü hem IRaceSession hem IRaceTelemetry olarak bağlayacak, ek alan gerekmeyebilir).
- [ ] Malik'in dosyaları değişmez (prefab, PhysicalTest); `overwrite` tek akış.
- [ ] (İstersen) builder sonunda Home kamerasından 2400×1080 bir PNG çeker: `Temp/shots/home.png` (batchmode `-nographics` OLMADAN Camera.Render →
      RenderTexture → ReadPixels; UI Overlay canvas kameraya çizilmez, o yüzden yalnız 3B sahne görünür — yine de karakter yerleşimini gösterir).

## Çalışma kuralları
- Çalışma klasörü MUTLAK: `D:\Pundun\speed-starts-uiv2` (worktree, dal `agent/ui-v2`). Ana klasöre yazma.
- Önce oku: ana klasördeki `.claude/agents/scene-integrator.md`, mevcut `Tools/Editor/Builders/Scene/*.cs`, `Scripts/Bridge/**` (T-017 paralelde),
  Malik'in `Scripts/Physical/Editor/PhysicalSetup.cs` (kamera/pist kurulumu referans, salt okunur).
- Paralelde: ui-agent'lar (T-015, T-016), gameplay-agent (T-017), test-agent (T-018). Unity/batchmode/verify YASAK, `git commit` YASAK, yalnız `.cs`.
- Bitince `status: review`, `## Çalışma notu` 5 satır.

## Çalışma notu
- Yeni `Scene/SceneHomeCharacter.cs`: kaide (silindir + `Scenes/Menu/Materials/HomePedestal.mat`), `HumanCharacterDummy_F` prefab ornegi (InstantiatePrefab, yaw 180, +Z one bakar), RunnerInput/RunnerBodyHUD/RunnerReplay m_Enabled=false (sahne override), isik, perspektif kamera (FOV 30, mesafe 5, bakis y=1.0); `SceneBuilder.BuildHome` cagiriyor, kayittan sonra `Temp/shots/home.png` (GPU yoksa atlanir).
- MenuCharacter tip adiyla (reflection) bulunur: `SpeedStars.Bridge.MenuCharacter`; hedef alan once target/runner/runnerRoot/character/root, sonra tek GameObject/Component alani; alan tipi RunnerBody vb. ise koku o bilesene cevirir; bulunamazsa acik hata. T-017 henuz yazmamisti (tip yok): T-017 bitince alan adi dogrulanmali.
- RunnerBody/PoseDriver ornekte acik birakildi (T-017 karari MenuCharacter'a ait). Race: RaceSceneInstaller'da `telemetry` alani yok, dokunulmadi.
- Derleme KONTROL EDILMEDI (Unity yok, Roslyn referanslari yok); yerlesim sayilari (kamera/kaide) gorsel dogrulanmadi. Malik'in prefab/sahnesi degismedi.
