---
id: T-069
title: Premium tema yazilari - kullanimdaki premiumda "IN USE" lavanta zeminde beyaz kaliyor; tema adi cerceveye tasiyor
status: todo
owner: ui-agent
depends_on: [T-068]
owns_paths:
  - "Assets/Speed-Staes/Scripts/UI/Themes/ThemeSelect*.cs"
  - "Assets/Speed-Staes/Tools/Editor/Builders/Ui/UiThemeSelectPrefabs.cs"
  - "Assets/Speed-Staes/Tests/EditMode/UI/Themes/**"
touches_scene: true
branch: agent/T-069-premium-tema-yazi
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
T-068 sonrasi goruntuler: `D:\Pundun\speed-starts\pipeline-logs\shots-int14-2026-10-30_145401\Home_themeselect_premium.png` (altin, USE) ve
`Home_themeselect_premium_inuse.png` (kirmizi, IN USE). Iki kusur:
1) Kullanimdaki premium temada "IN USE" etiketi beyaz/acik gri; premium dolgu acik lavanta oldugu icin okunmuyor. Premium dolgunun ustunde
   buton ve ad metni **lacivert** (UiArt.Navy) olmali — normal (premium olmayan) "IN USE" lacivert buton + beyaz yazi aynen kalir. Renk saf
   modelden gelsin (PremiumLook / snapshot), EditMode testli.
2) "Sky Islands" adi premium ad cercevesinin ic kenarlarina dayaniyor/tasiyor. Premium grupta metin alani ic lacivert cizginin icinde kalsin
   (ornek kenar payi), uzun adlar icin best fit ust/alt sinir ya da kucultme; premium olmayan temalarda gorunum ayni.
Kod kalici UI kurmaz; prefab/sahne uretimi supervizorde (`needs:` SCENE).

## Kabul kriterleri
- [ ] Goruntu: premium IN USE ve USE yazisi lacivert, net; ad cercevenin icinde, tasma yok
- [ ] EditMode: premium metin rengi kurali testli; var olan testler yesil
- [ ] needs: SCENE yazili

## Geri bildirim
GORSEL  : var olan
SES     : T-052
DOKUNSAL: T-052
ZAMAN   : yok
KAMERA  : yok

## Calisma notu
