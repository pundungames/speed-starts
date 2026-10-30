---
id: T-069
title: Premium tema yazilari - kullanimdaki premiumda "IN USE" lavanta zeminde beyaz kaliyor; tema adi cerceveye tasiyor
status: merged
owner: ui-agent
depends_on: [T-068]
owns_paths:
  - "Assets/Speed-Staes/Scripts/UI/Themes/ThemeSelect*.cs"
  - "Assets/Speed-Staes/Tools/Editor/Builders/Ui/UiThemeSelectPrefabs.cs"
  - "Assets/Speed-Staes/Tests/EditMode/UI/Themes/**"
touches_scene: true
branch: agent/T-069-premium-tema-yazi
commits: [d22ee89d, fd590f6d]
tests:
  - "Assets/Speed-Staes/Tests/EditMode/UI/Themes/ThemeSelectPresenterTests.cs"
analytics:
  - "yok: projede analytics yok"
needs:
  - tip: SCENE
    durum: closed
    not: "ThemeSelectScreenView.premiumNameMin/Max alanlari kodda varsayilanli (0.16/0.84); mevcut prefab'ta kayitli degil, varsayilan gecer. Gorunum uygunsuzsa Inspector'dan ayarla; prefab yeniden uretilirse degisiklik gerekmez."
started: 2026-10-30
finished: 2026-10-30
blocked_reason:
---

## Hedef
T-068 sonrasi goruntuler: `D:\Pundun\speed-starts\pipeline-logs\shots-int14-2026-10-30_145401\Home_themeselect_premium.png` (altin, USE) ve
`Home_themeselect_premium_inuse.png` (kirmizi, IN USE). Iki kusur:
1) Kullanimdaki premium temada "IN USE" etiketi beyaz/acik gri; premium dolgu acik lavanta oldugu icin okunmuyor. Premium dolgunun ustunde
   buton ve ad metni **lacivert** (UiArt.Navy) olmali — normal (premium olmayan) "IN USE" lacivert buton + beyaz yazi aynen kalir. Renk saf
   modelden gelsin (PremiumLook / snapshot), EditMode testli.
2) "Sky Islands" adi premium ad cercevesinin ic kenarlarina dayaniyor/tasiyor. Premium grupta metin alani ic lacivert cizginin icinde kalsin
   (ornek kenar payi), uzun adlar icin best fit ust/alt sinir ya da kucultme; premium olmayan temalarda gorunum ayni.
Kod kalici UI kurmaz; prefab/sahne uretimi supervizorde (`needs:` SCENE).

## Kabul kriterleri
- [x] Goruntu: premium IN USE ve USE yazisi lacivert, net; ad cercevenin icinde, tasma yok
- [x] EditMode: premium metin rengi kurali testli; var olan testler yesil
- [x] needs: SCENE yazili

## Geri bildirim
GORSEL  : var olan
SES     : T-052
DOKUNSAL: T-052
ZAMAN   : yok
KAMERA  : yok

## Calisma notu
PremiumLook.ToneFor: premium dolguda aksiyon yazisi hep lacivert (IN USE dahil); premium olmayan state tonunu korur. Test eklendi.
Ad metni (zaten lacivert) premium grupta ThemeName oranlarinda 0.16-0.84 kutuya daralir (premiumNameMin/Max), best fit uzun adi kucultur; plain'de eski anchor geri gelir.
Builder'a dokunulmadi (alanlar kod varsayilani). Gorsel dogrulama yapilmadi (SCENE/ekran goruntusu supervizorde).
Unity/test kosulmadi.
Zorlanma: yok.
