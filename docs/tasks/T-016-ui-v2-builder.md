---
id: T-016
title: UI v2 prefab builder - sanat import, Home / ResourceBar / RaceHud yeniden giydirme (layout.json), kapat butonlari
status: merged
owner: ui-agent
depends_on: [T-014a]
owns_paths:
  - "Assets/Speed-Staes/Tools/Editor/Builders/Ui/**"
touches_scene: false
branch: agent/ui-v2
commits: []
tests:
  - "gerekmiyor: editor builder'i; kabul supervizorun batchmode kosusu (overwrite) + ekran goruntusu + PlayMode testi (T-018)"
analytics:
  - "yok: projede analytics yok"
needs: []
started:
finished:
blocked_reason:
---

## Hedef
Kullanıcının gönderdiği UI v2 sanatını prefab'lara giydir. **Sözleşme:** `docs/tasks/UI-v2-ISIMLER.md` (hiyerarşi yolları + view alanları; T-015
view'ları bu adlarla yazıyor). Sanat: `Assets/Speed-Staes/UI/Art/Home/` (+ `layout.json`, `README.txt`, `preview.jpg`), `UI/Art/Race/`.
Tasarım: `docs/gdd/referans-ekranlar/tasarim-ana-ekran-v2.png`, `tasarim-yaris-hud-v2.png` — Read ile bak.

## Kabul kriterleri
- [ ] Sanat import: builder `UI/Art/**/*.png` için `TextureImporter` ayarlar (Sprite (2D and UI), Single, mipmap yok, Clamp, alpha is transparency),
      yalnız ayar farklıysa yeniden import. `.meta` builder'ın çalışmasıyla Unity'de oluşur (sen `.meta` yazmazsın).
- [ ] **Home** (`HomeScreen` prefab'ı): `layout.json`'daki her bileşen sözleşmedeki yolda; katmanlar aynı rect'te üst üste Image (`preserveAspect` kapalı,
      rect = katman tuvali). Konum `layout.json`'dan okunur (koda koordinat gömülmez): ölçek 1080/937, sol yarıdakiler sol kenara, sağdakiler sağ kenara,
      race_select alt-orta anchor. Metinler Text (sayı/etiket). Butonlarda `ButtonFeedback`. Power/Stamina `Fill` Image Filled yatay.
      Karakter ortada, kartın solunda boş kalır (sahne karakteri T-019); HomeScreen kökü raycast'i karakter alanında engellemesin.
- [ ] **ResourceBar** prefab'ı sol dikey sütun (purple_gem, entry_ticket + ticket_timer, timed_ticket, diamond) — `layout.json` konumlarıyla.
- [ ] **RaceHud** prefab'ı `tasarim-yaris-hud-v2.png` düzeniyle (UI/Art/Race sprite'ları): Telemetry (sol üst), Cash (onun altı), Energy ve Cadence (üst orta),
      Exit ve Restart (sağ üst). Cadence Track yeşil→kırmızı gradyan (Image + bir gradyan sprite'ı ya da üç renkli şerit — yeni PNG gerekirse builder
      `Texture2D` üretip `UI/Art/Race/cadence_gradient.png` olarak kaydedebilir).
- [ ] View alanları `UiViewBinder` ile sözleşmedeki adlara bağlanır (T-015'in alan adları; derleme sırasında T-015 bitmemiş olabilir → alan adlarını
      sözleşmeden al, `FindProperty` null ise açık hata).
- [ ] **Kapat:** her ekran/popup'taki `Back`/`Close`/`ItemBack` → `UiPanel.closeButton` (taban alan, T-015). İstisna: EntryGateModalView, EventSelectScreenView
      (kendi alanları zaten bağlı).
- [ ] Diğer prefab'lar (Cosmetics, HighScores, Results, popups, rows) bu turda yalnız kapat bağlaması; görselleri sonra.
- [ ] `overwrite` davranışı aynı.

## Çalışma kuralları
- Çalışma klasörü MUTLAK: `D:\Pundun\speed-starts-uiv2` (worktree, dal `agent/ui-v2`). Ana klasöre yazma.
- Paralelde: ui-agent `Scripts/UI/**` (T-015), gameplay-agent `Scripts/Bridge/**` (T-017), test-agent `Tests/PlayMode/**` (T-018), scene-integrator
  `Tools/Editor/Builders/Scene/**` (T-019). Onlara yazma.
- Unity/batchmode/verify YASAK, `git commit` YASAK, yalnız `.cs` (ve gerekirse builder'ın ÇALIŞIRKEN ürettiği PNG). Derleme kontrolü scratchpad'de Roslyn.
- Bitince `status: review`, `## Çalışma notu` 5 satır.

## Çalışma notu
- Yapilan: UiArt.cs (importer ayarlari, cadence_gradient.png uretimi, layout.json okuyucu), UiV2Prefabs.cs (Home, ResourceBar, RaceHud); UiScreenPrefabs/UiRowPrefabs bunlara devreder; UiViewBinder sozlesme alanlari + BindClose (UiPanel.closeButton: Cosmetics Back, HighScores Back, SevenDay Window/Close; EventSelect muaf (EntryGate dahil: Window/Close -> taban closeButton), Results/DailyGift kapat butonu yok).
- Derleme: scratchpad Roslyn EXIT 0 (alan adlari T-015'in calisma kopyasindaki view'larla karsilastirildi, hepsi eslesiyor).
- Zorluk: Race PNG'lerinde seffaf kenar boslugu var, boyutlar (TelemetrySize vb.) tahmini; Home karti 491x486 ortak tuval, Power/Stamina fill tuval genisligine gore dolar (bar 40..453 px) -> gorunen oran bar oranindan hafif sapar.
- Kart butonlari (Previous/Next/EditRacer/Emote) ust uste bindigi icin alpha hit test; bu 4 PNG isReadable=true. ButtonFeedback tum kart tuvalini (pivot merkez) olcekler; gerekirse punchTarget Icon'a cekilir.
- Bos birakilan: metin slot konumlari (isim, boy, etiketler) layout.json'da yok, UiV2Prefabs'ta oran sabitleri; RedDot prefab'i uretilmeye devam ediyor ama Home artik yuvalamiyor; sahne builder'daki eski ResourceBarSlot/Home adlari (T-019) kontrol edilmeli.
