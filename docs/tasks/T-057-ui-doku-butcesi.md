---
id: T-057
title: UI doku butcesi - Android'de UI sanati sikistirilmiyor (APK 450 MB, doku 238 MB); ASTC, tek dim, olcum
status: review
owner: ui-agent
depends_on: [T-056]
owns_paths:
  - "Assets/Speed-Staes/Tools/Editor/Builders/Ui/UiArt.cs"
  - "Assets/Speed-Staes/Tools/Editor/Builders/Ui/UiBuildKit.cs"
  - "Assets/Speed-Staes/Tools/Editor/Builders/Ui/*Prefabs.cs"
  - "Assets/Speed-Staes/Tests/EditMode/UI/**"
  - "docs/TUZAKLAR.md"
touches_scene: true
branch: agent/T-057-ui-doku-butcesi
commits: []
tests:
  - "Assets/Speed-Staes/Tests/EditMode/UI/UiTextureBudgetTests.cs"
analytics:
  - "yok: projede analytics yok"
needs:
  - tip: ASSET
    durum: open
    not: "Kopyada (aktif hedef Android) UiArt.ImportAll (Faz1Builder.RebuildAll icinde) calissin: UI/Art/**/*.png .meta'lari Android+iPhone ASTC 4x4 (max 2048; Race/counter_panel ve telemetry_panel 4096) ayariyla yeniden yazilir; .meta'lari kopyadan al. Yeni test dosyasi UiTextureBudgetTests.cs icin .meta uretilip alinsin (TUZAKLAR .meta'siz .cs)."
  - tip: SCENE
    durum: open
    not: "Faz1Builder.RebuildAll ile 7 builder (Analysis, EntryGate, EventSelect, Leaderboard, Results, Reward, Settings) prefab'lari yeniden uretilir: dim artik sprite'siz Image (rengi PNG'den okunur). Sonra yeni Android APK olculur (hedef: doku 238 MB'tan belirgin dusuk; once/sonra Build Report). Ek karar: Builders/Scene/SceneHomeDim.cs (benim yolum degil) Costume/dimscreen.png sprite'ini kullanmaya devam ediyor; o da Image rengine cevrilebilir (ASTC ile ~1.5 MB'a iner, tek dosya)."
started: 2026-10-30
finished: 2026-10-30
blocked_reason:
---

## Hedef
T-056'nin gelistirme APK'si 449.9 MB. Build Report ozeti (log worktree ile silindi): Textures 238.1 MB (%81), Meshes 51 MB (Serhat'in temalari, bizim degil).
En buyukler bizim UI sanatimiz: tam tuval boyutlu PNG'ler **tanesi 6.0 MB** (= genislik x yukseklik x 4: sikistirilmamis RGBA32), ornek
`UI/Art/Race/energy_icon.png` 1315x1197, `UI/Art/*/dimscreen.png` / `modal_dimscreen.png` 1678x937 (yedi klasorde ayni dim), Analysis/
Leaderboard/Results panelleri 1-2 MB. `UiArt.ImportAll` (UiArt.cs ~65-87) varsayilan platformda `Compressed` diyor ama Android'de NPOT ve
4'un kati olmayan boyutlar ETC2'ye girmeyip RGBA32'ye dusuyor (olasi sebep; dogrula). Ayrica `home` sanati bilerek `Uncompressed`.

Yapilacak (karar: supervizor):
1) `UiArt.ImportAll`: Android icin acik platform ayari **ASTC 4x4** (alfa var, keskin kenar; boyut kurali yok), mipmap kapali, max 2048
   (2048'i asan tek tek sanat varsa onlar icin 4096 kalir — hangileri, calisma notuna). iOS icin de ayni ASTC (zarar yok). `home`
   sanatinin `Uncompressed` olmasinin sebebini koddan/yorumdan bul; gecerli sebep yoksa o da ASTC 4x4 (yoksa yaz).
   Varsayilan (Standalone/Editor) ayari degismez.
2) **Dim'ler**: yedi tam ekran dim PNG yerine tek kucuk doku (ornek 4x4 ayni renk/alfa) ya da sprite'siz `Image` rengi; gorunum ayni
   (rengi ve alfayi PNG'den olc, calisma notuna yaz). Builder'lardaki dim kullanimlari tek yardimciya baglanir.
3) Olcum: `UiArt` icinde (editor) "Android'de beklenen doku boyutu" hesaplayan saf fonksiyon (genislik, yukseklik, format -> bayt;
   ASTC 4x4 = 1 bayt/piksel, 16 baytlik blok yuvarlamasi) + EditMode testi; buyuk/kucuk iliski assert eder, sabit MB degil.
Sanat dosyalarina (PNG) dokunulmaz; .meta degisiklikleri builder'in ImportAll'u ile kopyada uretilir (supervizor; `needs:` ASSET/SCENE).
Gorsel dogrulama: supervizor kopyada (Android hedefinde) ekran goruntuleri + yeni APK boyutu.

## Kabul kriterleri
- [ ] Android ayari kodda acik; Standalone ayari degismedi
- [ ] Dim tek kaynaktan; ekran goruntulerinde dim tonu ayni
- [ ] needs: ASSET (ImportAll ile .meta yeniden yazimi) + SCENE (prefab/sahne yeniden uretimi) yazili; supervizor yeni APK'yi olcer
      (hedef: doku kategorisi 238 MB'tan belirgin dusuk; sonuc calisma notuna)

## Geri bildirim
GORSEL  : ayni gorunum
SES     : yok
DOKUNSAL: yok
ZAMAN   : yok
KAMERA  : yok

## Calisma notu
1. Android+iPhone icin acik ASTC 4x4 platform ayari (UiArt.ApplyMobile; idempotent: GetPlatformTextureSettings uyusuyorsa reimport yok), max 2048; varsayilan platform degismedi (home Uncompressed, digerleri Compressed, max 4096).
2. 2048'i asan sanat (PNG basligindan okunur, o ikisi 4096 alir): Race/counter_panel.png 2098x749, Race/telemetry_panel.png 2172x724; baska yok.
3. Dim: 7 PNG 1678x937 duz renk. Olculen RGBA: Analysis (6,26,65,100); Costume (3,15,33,130); EventSelect (3,15,33,130); Results (3,15,33,140); Settings (3,15,33,150); Leaderboard/modal (6,26,65,150); Rewards/modal (6,26,65,135). UiArt.PaintDim rengi PNG'den okur (duz degilse hata), sprite'siz Image; 9 cagri (6 builder + Leaderboard PopupRoot) buna baglandi. Costume dim'i SceneHomeDim (baska yol) kullaniyor, needs'te.
4. `home` Uncompressed sebebi: kodda/yorumda/git gecmisinde (50e95cde) gecerli sebep yok, ilk UI v2 surumunde konmus; mobilde artik ASTC 4x4 (8 bit/piksel, en yuksek ASTC kalitesi); editor/varsayilan platform Uncompressed kalir, gorsel risk ince cizgide, supervizor ekran goruntusuyle bakar.
5. Olcum: UiArt.ExpectedTextureBytes (Rgba32 = 4 B/px, Astc4x4 = 16 B/4x4 blok) + EditMode testi (reflection ile; Assembly-CSharp-Editor test asmdef'ine acik degil). Calistirilmadi/derlenmedi (merkezi dogrulama). 1678x937 dim: RGBA32 6.29 MB -> ASTC ~1.57 MB, 7 dim prefab'tan kalkinca ~44 MB.
