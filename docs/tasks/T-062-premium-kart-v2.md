---
id: T-062
title: Kostum premium karti v2 (Premium_Kart_Duzeltme_v2) - kalin altin cerceve, seciliyken altin yerine kirmizi; dikey sanat hucreye oranla sigar
status: todo
owner: ui-agent
depends_on: [T-044]
owns_paths:
  - "Assets/Speed-Staes/Tools/Editor/Builders/Ui/UiCostumePrefabs.cs"
  - "Assets/Speed-Staes/Scripts/UI/Costume/CostumeItemCardView.cs"
  - "Assets/Speed-Staes/Tests/EditMode/UI/Costume/**"
  - "Assets/Speed-Staes/Tests/PlayMode/**"
touches_scene: true
branch: agent/T-062-premium-kart-v2
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
Kullanici (30.10): "bu shoptaki hatali premium selected kart UI'lari" + `Premium_Kart_Duzeltme_v2.zip`. Paket (supervizor scratchpad'i
`C:\Users\Monster\AppData\Local\Temp\claude\d--Pundun-speed-starts\010558a9-f012-4284-b1f2-1285cf76ea87\scratchpad\premium-v2\`: IMPLEMENTATION.md
**baglayici**, layout.json, preview_composite.jpg) onceki Ses_Kapali_ve_Premium_UI_v1'in premium_background / premium_frame /
premium_selected_frame parcalarinin yerine gecer. Supervizor sanati ayni dosya adlariyla degistirdi (`UI/Art/Costume/Premium/`;
GUID'ler ayni): `premium_background.png` (acik lavanta dolgu), `premium_frame.png` (= premium_frame_gold: kalin altin serit, dista kalin
icte ince lacivert kontur, ortasi saydam), `premium_selected_frame.png` (= premium_frame_selected_red: ayni geometri, altin yerine kirmizi).
Ucu ayni **736x807** tuval, merkez pivot; kirpilmaz. Bugunku gorunum: `D:\Pundun\speed-starts\pipeline-logs\shots-int12b-2026-10-30_142607\Home_costume.png`
(premium kartlar ince altin cizgili; seciliyken kirmizi kontur altinin USTUNE biniyor — paket bunu yasakliyor).

1) **Katman kurali** (`PremiumCardLayers`): premium normal = Background + ikon + **GoldFrame** (+ lock/fiyat); premium secili = Background
   + ikon + **SelectedRedFrame** + check. Gold ve Red **asla birlikte** gorunmez (`PremiumFrame = premium && !selected`,
   `PremiumSelectedFrame = premium && selected`). `PremiumCardStateTests` bu kurala gore guncellenir (eski "premium hep PremiumFrame" assert'i
   yanlis artik); "ikisi birlikte yok" testi eklenir.
2) **Olcek**: dikey kart 176x164 hucreye **esnetilmez**: Background + Gold + Red ayni ebeveyn grupta (`PremiumCard` dugumu), grup hucreye
   **oran korunarak** (Fit) sigar ya da hucre bu orana uyarlanir (kostum izgarasi duzenini bozmayacak olani sec, calisma notuna yaz). Ucu ayni
   rect'i alir. Urun ikonu ic lacivert cercevenin icinde kalir. Eski 176x164 `premium_lock` / `premium_check` / `premium_disabled_overlay`
   yeni tuvale native basilmaz: kendi RectTransform'larinda grubun sag-alt (lock) / sag-ust (check) kosesine cipalanir; disabled overlay grubu kaplar.
3) **Ekran goruntusu**: `ScreenshotTests`'e `Shots_CostumePremium` (yalniz `-shotDir`): bir premium oge sahiplenilip (`ICosmeticService.GrantOwned`,
   kayit sonra geri yuklenir) secili iken `Home_costume_premium_selected`, sahipsiz/kilitli iken `Home_costume_premium_locked`; var olan
   `Home_costume` kalir.
Kod kalici UI kurmaz; dokunma alanlari gorunur bolge; DOTween yalniz cekirdek (TUZAKLAR). PNG/.meta'ya dokunma; prefab/sahne uretimi
supervizorde (`needs:` SCENE).

## Kabul kriterleri
- [ ] EditMode: secili premium = yalniz kirmizi, secili degil = yalniz altin, ikisi asla birlikte; normal (premium olmayan) kart degismez
- [ ] Goruntulerde premium kart esnememis (oran 736:807), ikon ic cerceve icinde, lock/check kosede
- [ ] needs: SCENE yazili

## Geri bildirim
GORSEL  : var olan secim punch'i korunur
SES     : T-052
DOKUNSAL: T-052
ZAMAN   : yok
KAMERA  : yok

## Calisma notu
