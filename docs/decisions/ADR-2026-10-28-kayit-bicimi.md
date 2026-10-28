# ADR 2026-10-28 — Kayıt dosyası biçimi

**Durum:** kabul (süpervizör, T-003)

## Bağlam
Ekonomi (biletler, Mor Mücevher, Nakit) PlayerPrefs'e emanet edilmez (MD §14). Yarım kalan yazım ya da elle oynanan dosya
oyuncunun ilerlemesini silmemeli ya da hediye bilet üretmemeli.

## Karar
- `FileSaveStore` (`Scripts/Meta/Persistence/`): `persistentDataPath/save/<anahtar>` altında. Yazım: `.tmp` (diske flush) →
  eski ana dosya `.bak` → `.tmp` ana dosya. Her adımda bir sağlam kopya vardır.
- Dosya: ilk satır `SSAVE1 <FNV-1a 64-bit hex>`, ardından UTF-8 JSON (`JsonUtility`, `PlayerProgress`). Hash tutmazsa, dosya
  kesikse ya da sıfırla doluysa `.bak` okunur. Bozuk ana dosya `.bak`'ın üstüne yazılmaz.
- `PlayerProgress` şema sürümü 1. Alan adları JSON anahtarıdır: **alan yeniden adlandırılırsa eski kayıtlardaki değer düşer**
  (göç kodu yazılmadan ad değiştirilmez). Zamanlar UTC ticks, 0 = yok / sayaç çalışmıyor.
- Daha yeni bir sürümün kaydı ya da okunamayan kayıt: oyun yeni ilerlemeyle açılır, `Save()` false döner ve dosyaya dokunmaz
  (kurtarılabilir kalır).

## Sonuç
- Kayıt/ekonomi kodu değişince tam test turu, tek başına (`docs/CALISMA-KURALLARI.md` §4).
- Risk: `JsonUtility`'nin `long` alanları birebir yazması; kopyadaki test turu doğrular (gerekirse ticks string'e çevrilir).
- Hash hile koruması değildir, yalnız bozulma tespitidir.
