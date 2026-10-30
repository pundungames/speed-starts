# ADR-2026-10-30-kozmetik-ten-rengi-ve-9-preset

Durum: kabul edildi
Tarih: 30.10.2026
Karari veren: kullanici

## Baglam
Kullanici kosucunun ten rengini kiyafetle birlikte secmek ve hazir kostum sayisini 3'ten 9'a cikarmak istedi (T-065).

## Karar
`CosmeticPreset` kayda `skinColorHex` alani (RRGGBB, bos = katalogdaki varsayilan ten) eklenir. Kayit surumu ARTMAZ (2 kalir): alan
eklemeli, JsonUtility eski kayitta eksik alani bos birakir; `EnsureValid` null'u bosa cevirir. Preset sayisi katalogdaki `presetCount`
(9) ile artar; eksik presetler servis tarafindan varsayilan kiyafetle ve varsayilan tenle doldurulur, mevcutlar aynen kalir.

## Gerekce
`moreColorHexes` ile ayni yol (ADR-2026-10-30-kozmetik-renk-kanallari): eski kayit gecis adimi istemeden acilir, yeni kayit eski surumde de okunur
(bilinmeyen alan yok sayilir). Ten ayri bir kayit alani yerine preset icinde durur ki kostumle birlikte degissin.

## Sonuclari
- Eski kayit varsayilan tenle acilir; 3 presetli kayit 9'a kayipsiz genisler (testli: CosmeticSkinTests).
- Ten rengi materyale degil MaterialPropertyBlock ile kosucunun ten yuvasina yazilir; paylasilan materyal ve Malik'in prefab'i degismez.
- Etkilenen: PlayerProgress.cs, CosmeticService.cs, CosmeticContracts.cs, CosmeticCatalog.cs, OutfitPlanner.cs, CosmeticApplier.cs.

## Geri alma sarti
Ten rengi preset'ten bagimsiz tek bir oyuncu ayari olacaksa (kayit alani tasinir, gecis gerekir).
