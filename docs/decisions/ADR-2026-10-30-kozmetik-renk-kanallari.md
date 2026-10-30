# ADR 2026-10-30 — Kozmetik renk kanalları: kanal başına renk, şema 2 kalır

**Durum:** kabul (systems-agent, T-035)

## Bağlam
Bir öğenin 1-3 renk kanalı var (`Color1_Toon` .. `Color3_Toon`), ama kayıt ve kıyafet yalnız birincil kanalın tek rengini
tutuyordu (`SelectedCosmetic.colorHex`, `CosmeticSlot.ColorHex`). Kullanıcı 2./3. rengi seçip kaydetmek istiyor.

## Karar
- **Renkler öğenin kanal sırasına göre indekslenir** (0 = birincil kanal, 1, 2). Öğe tanımı (`CosmeticDefinition`) sıralı
  `channels` (birincil önce) ve aynı sırada `channelDefaultColors` (materyalin kendi rengi, RRGGBB) taşır; boşsa tek kanal
  olarak eski `primaryChannel` okunur. Tohum (`CosmeticCatalogSeed`) bunları prefab materyallerinden yazar; kanal listesi
  boş mevcut öğeler tohumda geri doldurulur (başka alana dokunulmaz).
- **Kayıt:** `SelectedCosmetic.colorHex` aynı anahtarla birincil kanalın rengi kalır; yeni `moreColorHexes` (liste) 2. ve
  3. kanalın rengini taşır (sondaki boşlar yazılmaz). Şema sürümü **2 kalır**: değişiklik yalnız eklemedir.
- **Göç:** eski kayıt (tek `colorHex`) olduğu gibi açılır ve indeks 0'a, yani öğenin birincil kanalına düşer — eski anlamı
  zaten buydu; `moreColorHexes` yoksa `FromJsonOverwrite` boş liste bırakır. Kod göç adımı gerekmez; testle kanıtlandı
  (`Migration_OldSingleColour_*`). Yeni kayıt yazınca eski bir derleme yalnız birincil rengi okur, diğerlerini yok sayar.
- **Uygulayıcı:** `OutfitPlan` her renkli kanal için (materyal adı, renk) üretir; `CosmeticApplier` her materyal yuvasına
  adıyla eşleşen kanalın rengini MaterialPropertyBlock ile yazar, renksiz kanal yuvaları kendi renginde kalır.
- Kanal sırası katalogda değişirse kayıtlı 2./3. renkler kayar; kanal listesi öğe başına bir kez tohumlanır, elle
  değiştirilmez.

## Sonuç
- Kayıt/ekonomi değişikliği: tam test turu gerekir (kayıt biçimine alan eklendi).
- Kod varsayılanı yok: kanal listesi ve varsayılan renkler `CosmeticDefinition` asset'lerindedir (tohum çalıştırılınca dolar).
