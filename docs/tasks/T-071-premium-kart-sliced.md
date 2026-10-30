---
id: T-071
title: Premium kostum karti hucreyi doldursun - 9-slice kenar + Image Sliced (oran koruma ve dar dikdortgen gorunum kalkar)
status: todo
owner: ui-agent
depends_on: [T-062]
owns_paths:
  - "Assets/Speed-Staes/Tools/Editor/Builders/Ui/UiCostumePrefabs.cs"
  - "Assets/Speed-Staes/Tools/Editor/Builders/Ui/UiArt.cs"
  - "Assets/Speed-Staes/Scripts/UI/Costume/CostumeItemCardView.cs"
  - "Assets/Speed-Staes/Tests/EditMode/UI/Costume/**"
touches_scene: true
branch: agent/T-071-premium-kart-sliced
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
Kullanici (30.10, kostum izgarasi goruntusuyle): "sprite editor'den editle, premium olanlar dikdortgen duruyor. image'i simple degil sliced kullan
ayarla". T-062 premium karti `AspectRatioFitter` (FitInParent, 736:807) ile hucreye sigdirdi; kart normal kartlardan dar, dikey dikdortgen
gorunuyor. Hedef: premium kart **normal kartla ayni dis boyutta** (hucreyi doldurur), cerceve kalinligi ve pahli koseler bozulmadan.

1) **9-slice kenar** (supervizor olctu, 736x807 ortak tuval): cerceve bandi kenarlarda ~8..77 px, pah koselerin kenardan ~100 px icine kadar,
   arka plan dolgusunun ic pahi ~140 px'e kadar. Uc sprite'a da (`UI/Art/Costume/Premium/premium_background.png`, `premium_frame.png`,
   `premium_selected_frame.png`) **ayni** border: sol/sag/ust/alt **150 px** (hizalama icin uc katman ayni olmali). Border import ayarinda
   (`TextureImporter.spriteBorder`) — `UiArt` import'una dosya bazli border tablosu ekle (ImportAll idempotent kalir; ayni ayarsa yeniden
   import etme). SpriteMeshType FullRect.
2) **Image Sliced**: bu uc katman `Image.Type.Sliced` (background `fillCenter = true`, cerceveler `fillCenter = false` — cerceve merkezi zaten
   saydam, ikisi de olur), `preserveAspect` kapali, `AspectRatioFitter` kalkar; `PremiumCard` grubu normal kartin dis rect'ini (hucre) kaplar.
   `pixelsPerUnitMultiplier`: cerceve bandi T-062'deki gorunen kalinlikta kalsin (o zaman kart yuksekligi hucre yuksekligine oturuyordu:
   carpan ~ sprite yuksekligi 807 / hucre yuksekligi; hesabi builder'da yap, sabit sayi koyma).
3) Premium ikon, lock (sag-alt) ve check (sag-ust) yeni grup rect'ine gore ayni oranlarla; ikon ic lacivert cizginin icinde kalir.
Kural (altin normal / kirmizi secili, ikisi asla birlikte) degismez. Kod kalici UI kurmaz; prefab/asset uretimi supervizorde (`needs:` SCENE + ASSET:
uc .meta'nin border'i builder import'u ile yazilir).

## Kabul kriterleri
- [ ] `Home_costume_premium_locked` / `_selected` goruntulerinde premium kart normal kartla ayni genislik/yukseklikte, koseler pahli ve esnememis,
      cerceve bandi esit kalinlikta
- [ ] Var olan PremiumCardStateTests yesil; import border'i icin (ya da multiplier hesabi icin) saf yardimci varsa EditMode testi

## Geri bildirim
GORSEL  : var olan
SES     : T-052
DOKUNSAL: T-052
ZAMAN   : yok
KAMERA  : yok

## Calisma notu
