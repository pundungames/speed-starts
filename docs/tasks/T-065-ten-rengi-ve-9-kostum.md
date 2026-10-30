---
id: T-065
title: Kostum cekirdegi - ten rengi (kiyafetle birlikte kaydedilir, kosucuya uygulanir) ve 9 hazir kostum (Custom 1..9)
status: review
owner: systems-agent
depends_on: [T-035]
owns_paths:
  - "Assets/Speed-Staes/Scripts/Meta/Contracts/CosmeticContracts.cs"
  - "Assets/Speed-Staes/Scripts/Meta/Cosmetics/**"
  - "Assets/Speed-Staes/Scripts/Meta/Config/Cosmetic*.cs"
  - "Assets/Speed-Staes/Scripts/Meta/Persistence/PlayerProgress.cs"
  - "Assets/Speed-Staes/Scripts/Bridge/CosmeticApplier.cs"
  - "Assets/Speed-Staes/Scripts/Bridge/OutfitPlan*.cs"
  - "Assets/Speed-Staes/Tools/Editor/Builders/Scene/CosmeticCatalogSeed.cs"
  - "Assets/Speed-Staes/Tests/EditMode/Meta/Cosmetics/**"
  - "Assets/Speed-Staes/Tests/EditMode/Bridge/**"
  - "docs/decisions/**"
touches_scene: false
branch: agent/T-065-ten-rengi
commits: []
tests:
  - "Assets/Speed-Staes/Tests/EditMode/Meta/Cosmetics/CosmeticSkinTests.cs"
analytics:
  - "yok: projede analytics yok"
needs:
  - tip: ASSET
    durum: open
    not: "CosmeticCatalog asset: presetCount 3 -> 9 (elle); yeni alanlar eski asset'te kod varsayilaniyla gelir ama asset kaydedilince sabitlenir: defaultSkinColor 462B27, skinRendererName LowPoly Male, skinMaterialName ColorSkin_Toon, skinTones FFDFC4 F1C27D E0AC69 C68642 A9693D 8D5524 6B4226 462B27"

started: 2026-10-30
finished: 2026-10-30
blocked_reason:
---

## Hedef
Kullanici (30.10): "body yerine top yazicaz. body content ekle ve o da karakterin ten rengini degistirsin", "custom da 3 tane degil 9 tane olsun".
Bu task cekirdek; arayuz T-066 (paralel, bu sozlesmeye yazar).

1) **Ten rengi**: `CosmeticOutfit`'e ten rengi (`SkinColorHex`, bos = katalogdaki varsayilan ten), `WithSkinColor(string hex)`; esitlik/kopya
   bunu da kapsar. Hazir kostumlerle (preset) birlikte kaydedilir (`PlayerProgress` / `CosmeticOutfitEntry`; eski kayit bos ten ile acilir,
   kisa ADR). `CosmeticCatalog`'a: `defaultSkinColor` (bugunku kosucu ten rengi — prefab'taki ten materyalinin rengini oku, uydurma) ve
   `skinTones` (hazir ten ornekleri listesi, 8 adet makul aralik; supervizor karari, config'te degisir).
2) **Uygulama**: `CosmeticApplier` ten rengini kosucunun govde mesh'inin (Malik'in prefab'i `Prefabs/Character/HumanCharacterDummy_F.prefab`,
   ornek "LowPoly Male" SkinnedMeshRenderer) **ten materyal yuvasina** MaterialPropertyBlock `_Color` ile yazar; paylasilan materyale ve
   Malik'in prefab'ina dokunulmaz. Hangi renderer/yuva oldugu katalogda (yol + yuva indeksi ya da materyal adi) — prefab YAML'ini okuyarak bul,
   calisma notuna yaz. Ten bos ise varsayilan.
3) **9 hazir kostum**: `CosmeticCatalog.presetCount` tohumu 3 -> 9 (`CosmeticCatalogSeed`); mevcut asset icin `needs:` ASSET (presetCount 9).
   Kayitta 3 preset olan oyuncu 9'a genisler (yeni olanlar varsayilan kiyafetle), veri kaybi yok; testli.
4) Arayuz icin sozlesme (T-066 buna yazar, aynen uygula; isim catisirsa bildir): `ICosmeticService` uzerinden onizleme/kayit akisi
   degismeden `CosmeticOutfit.SkinColorHex`, `CosmeticOutfit.WithSkinColor(hex)`, `CosmeticCatalog.SkinTones` (`IReadOnlyList<string>` hex),
   `CosmeticCatalog.DefaultSkinColor` (hex).
Denge sayisi yok (ten ucretsiz). Unity calistirma yok; baglama/asset supervizorde (`needs:`).

## Kabul kriterleri
- [ ] EditMode: ten rengi kaydedilip geri gelir; eski kayit varsayilan tenle acilir; 3 presetli kayit 9'a genisler kayipsiz; OutfitPlan/applier
      ten yuvasini dogru renkle planlar (motor bagimsiz kisim testli)
- [ ] needs: ASSET (CosmeticCatalog presetCount 9, defaultSkinColor, skinTones) yazili

## Geri bildirim
GORSEL  : ten rengi aninda kosucuda (T-066 onizleme)
SES     : yok
DOKUNSAL: yok
ZAMAN   : yok
KAMERA  : yok

## Calisma notu
- Ten yuvasi: prefab kokunun dogrudan cocugu "LowPoly Male" (SkinnedMeshRenderer, tek materyal, yuva 0) = ColorSkin_Toon; _Color (0.2736, 0.1695, 0.1510) = varsayilan ten 462B27.
- CosmeticOutfit.SkinColorHex/WithSkinColor, katalog DefaultSkinColor/SkinTones/SkinRendererName/SkinMaterialName; OutfitPlan.Skin (SkinPlan); CosmeticApplier.ApplySkin MaterialPropertyBlock ile yazar.
- Kayit: CosmeticPreset.SkinColorHex (surum artmadi, ADR yazildi). Preset 3->9 icin yeni kod gerekmedi, servis zaten tembel dolduruyor; yalniz asset presetCount=9 ve tohum 9.
- Zorlandigim: yok. Unity/test kosulmadi. Yeni test dosyasinin .meta'si Unity import'unda uretilir.
- Bitmedi/riskli: skinTones degerleri benim makul secimim (supervizor karari); asset elle guncellenmeli.
