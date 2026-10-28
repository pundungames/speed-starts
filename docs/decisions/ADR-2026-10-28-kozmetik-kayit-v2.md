# ADR 2026-10-28 — Kozmetik kaydı: şema 2, yarışçı ön ayarları

**Durum:** kabul (systems-agent, T-025)

## Bağlam
Kayıtta tek bir `selectedCosmetics` listesi vardı (kullanılmıyordu). Tasarım "Custom 1..N" yarışçı ön ayarları istiyor
(her ön ayar = kategori başına öğe + birincil renk).

## Karar
- `PlayerProgress` şema sürümü **2**. Yeni alanlar: `cosmeticPresets` (liste; her biri `slots` = `SelectedCosmetic` listesi,
  kategori enum ADIYLA) ve `selectedPresetIndex`. `ownedCosmeticIds` aynı kaldı (yalnız satın alınan premium'lar yazılır;
  fiyatsız normal öğeler kayıt gerektirmeden sahip sayılır).
- Göç 1 → 2 (`ProgressRepository.Load`): eski `selectedCosmetics` doluysa ve ön ayar yoksa ilk ön ayara taşınır, liste boşaltılır.
  Eksik ön ayarlar (sayı katalogdaki `presetCount`) `CosmeticService` tarafından katalogdaki varsayılan kıyafetle, kaydetmeden
  tembel doldurulur. Eski kayıt bozulmaz; yeni alanları olmayan JSON `FromJsonOverwrite` ile boş listelerle açılır.
- Kayıttaki bilinmeyen ya da kilitli öğe okunurken yok sayılır (oyuncu kilitli öğeyle görünemez).

## Sonuç
- Daha yeni sürümün kaydı (şema > 2) eskisi gibi korunur. Şema 2 kaydını açan eski bir derleme "NewerSchema" görür.
- Kayıt/ekonomi değişikliği: tam test turu gerekir.
