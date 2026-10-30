---
id: T-044
title: Ses kapali ikonu + premium kostum karti durumlari (Ses_Kapali_ve_Premium_UI_v1)
status: todo
owner: ui-agent
depends_on: [T-031, T-041]
owns_paths:
  - "Assets/Speed-Staes/UI/Art/Home/sound_off_icon.png"
  - "Assets/Speed-Staes/UI/Art/Costume/Premium/**"
  - "Assets/Speed-Staes/Scripts/UI/Home/HomeScreenView.cs"
  - "Assets/Speed-Staes/Scripts/UI/Costume/CostumeItemCardView.cs"
  - "Assets/Speed-Staes/Scripts/UI/Costume/CostumeEditorScreenView.cs"
  - "Assets/Speed-Staes/Tools/Editor/Builders/Ui/UiV2Prefabs.cs"
  - "Assets/Speed-Staes/Tools/Editor/Builders/Ui/UiCostumePrefabs.cs"
  - "Assets/Speed-Staes/Tests/EditMode/UI/**"
  - "Assets/Speed-Staes/Tests/PlayMode/**"
touches_scene: true
branch: agent/T-044-ses-premium
commits: []
tests:
  - "Assets/Speed-Staes/Tests/EditMode/UI/Costume/PremiumCardStateTests.cs"
analytics:
  - "yok: projede analytics yok"
needs: []
started:
finished:
blocked_reason:
---

## Hedef
Sanat: `C:\Users\Monster\Downloads\Ses_Kapali_ve_Premium_UI_v1.zip` (açılmış hâli süpervizör scratchpad'inde `ses-ui\`: **IMPLEMENTATION.md
= bağlayıcı**, layout.json, premium_examples.jpg, sound_off_preview.jpg, 7 PNG).
1) **Ses kapalı**: ana ekran hoparlör butonu (T-031'de SFX aç/kapa, kapalıyken ikon soluk yer tutucu) → kapalıyken `sound_off_icon.png`, açıkken
   var olan `sound_icon`; aynı RectTransform/native size, yalnız sprite değişir; buton tıklanabilir kalır (disabled değil); açılışta kayıtlı
   `IPlayerSettings.SfxOn` ile eşitlenir. Soluk (alpha) yer tutucu kalkar.
2) **Premium kart** (kostüm editörü öğe kartı; bugün premium = altın tonlu yer tutucu): katman sırası background → gerçek öğe ikonu → frame →
   selected_frame → lock / check → disabled_overlay (yalnız gerçek disabled). Durumlar: Locked = normal + kilit (seçilip önizlenebilir);
   Owned = normal; Selected/giyili = normal + selected_frame + check. Normal (premium olmayan) kartlar değişmez. Hepsi 176×164 ortak tuval,
   kırpılmaz. Fiyat paneli (karakter altı satın alma) yerinde kalır.
Legacy Text; DOTween çekirdek; kod kalıcı UI kurmaz; dokunma alanları görünür bölge (TUZAKLAR: "frame" adlı PNG opak plaka olabilir — alfa'ya bak).

## Kabul kriterleri
- [ ] Saf durum eşlemesi (premium × sahip × giyili × kilit → görünür katmanlar) EditMode testli
- [ ] PlayMode yeşil (gerçek ışınlı Tap); ekran görüntüsü `Home_costume` premium kartları yeni görünümde, `Home_play` ses açık ikonu
- [ ] needs listesinde open WIRING/SCENE yok

## Kapsam disi
- Yeni ikon/kıyafet çizimi, fiyat yerleşimi değişikliği, ses sistemi

## Calisma notu
(Agent doldurur)
