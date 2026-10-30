---
id: T-068
title: Premium tema (Premium_Tema_UI_v1) - Sky Islands premium; tema adi ve islem butonu altin (normal) / kirmizi (kullanimda) cerceve, pasif ortu
status: todo
owner: ui-agent
depends_on: [T-027]
owns_paths:
  - "Assets/Speed-Staes/UI/Art/ThemeSelect/Premium/**"
  - "Assets/Speed-Staes/Scripts/UI/Themes/ThemeSelect*.cs"
  - "Assets/Speed-Staes/Scripts/Meta/Contracts/ThemeContracts.cs"
  - "Assets/Speed-Staes/Scripts/Meta/Themes/**"
  - "Assets/Speed-Staes/Tools/Editor/Builders/Ui/UiThemeSelectPrefabs.cs"
  - "Assets/Speed-Staes/Tools/Editor/Builders/Scene/ThemeCatalogSeed.cs"
  - "Assets/Speed-Staes/Tests/EditMode/Meta/Themes/**"
  - "Assets/Speed-Staes/Tests/EditMode/UI/Themes/**"
  - "Assets/Speed-Staes/Tests/PlayMode/**"
touches_scene: true
branch: agent/T-068-premium-tema
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
Kullanici (30.10): "sky map secerken sky premium olacak" + `Premium_Tema_UI_v1.zip` (acilmis: supervizor scratchpad'i
`C:\Users\Monster\AppData\Local\Temp\claude\d--Pundun-speed-starts\010558a9-f012-4284-b1f2-1285cf76ea87\scratchpad\premium-tema\`;
IMPLEMENTATION.md **baglayici**, layout.json, approved_preview.jpg, 8 PNG: theme_name_{fill,frame_gold,frame_red,disabled_overlay} 610x228,
theme_action_{fill,frame_gold,frame_red,disabled_overlay} 1130x280). PNG'leri `Assets/Speed-Staes/UI/Art/ThemeSelect/Premium/` altina kopyala
(ayni adlar; .meta'yi Unity uretir).

1) **Veri**: `ThemeCatalog`'da tema basina `premium` bayragi ve para birimi (Nakit | Mor Mucevher). Sky Islands (`sky_islands`) premium,
   para birimi **Mor Mucevher** (supervizor karari: premium = Mor Mucevher, kozmetikle ayni aile). **Fiyat config'te**; paket "kozmetigin 50'sini
   kopyalama" diyor ve kullanici fiyat vermedi -> tohum 0 (fiyatsiz = satin alinamaz, bugunku kural), `needs:` DECISION (fiyat). Editor/dev'de
   butun temalar zaten sahipli (T-023) — degismez. `IThemeService` satin alma yolu Mor Mucevher'i da destekler (EconomyLedger ile, idempotent).
2) **Gorunum** (tema secim ekrani, `UiThemeSelectPrefabs` + `ThemeSelectScreenView`): premium temada tema adi grubu ve alt islem butonu grubu
   paket katmanlariyla: normal = fill + frame_gold; kullanimdaki (aktif) premium = fill + frame_red (altin KAPALI); etkilesim yok = mevcut hal +
   disabled_overlay ve `Button.interactable = false`. Gold ve red asla birlikte degil. Premium olmayan temada bugunku gorunum aynen. Metin
   (tema adi, USE / IN USE / fiyat) ayri legacy Text, en ustte; fiyat ikonu mevcut para ikonu (Mor Mucevher) ayri child. Oklar ve dolasma
   aynen. Gruplar mevcut tema kontrolunun ebeveyn boyutunu alir, oran korunur (cerceve ezilmez).
3) Saf model (hangi katman, buton metni, interactable) EditMode testli; PlayMode: Sky Islands'a gelince premium gorunum, ekran goruntusu
   `Home_themeselect_premium`.
Paralel: T-067 ayni ekranin kaynak kutularini kaldiriyor — sen yalniz tema adi ve islem butonu bolgesine dokun. Kod kalici UI kurmaz;
DOTween yalniz cekirdek; prefab/sahne/asset uretimi supervizorde (`needs:`).

## Kabul kriterleri
- [ ] EditMode: premium normal = altin, aktif = kirmizi, ikisi asla birlikte; fiyatsiz premium = satin alinamaz; Mor Mucevher ile satin alma
      idempotent (yetersiz bakiye -> degisiklik yok)
- [ ] Goruntu: Sky Islands premium (altin cerceve), aktifken kirmizi
- [ ] needs: SCENE + ASSET (ThemeCatalog premium/para birimi) + DECISION (fiyat) yazili

## Geri bildirim
GORSEL  : secimde var olan punch korunur
SES     : T-052
DOKUNSAL: T-052
ZAMAN   : yok
KAMERA  : yok

## Calisma notu
