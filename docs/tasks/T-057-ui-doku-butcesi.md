---
id: T-057
title: UI doku butcesi - Android'de UI sanati sikistirilmiyor (APK 450 MB, doku 238 MB); ASTC, tek dim, olcum
status: todo
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
tests: []
analytics:
  - "yok: projede analytics yok"
needs: []
started:
finished:
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
