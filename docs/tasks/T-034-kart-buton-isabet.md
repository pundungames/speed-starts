---
id: T-034
title: Karakter karti butonlari tum karti kapliyor (Emote tiklamayi yutuyor) + testte gercek isin kontrolu
status: todo
owner: ui-agent
depends_on: [T-031]
owns_paths:
  - "Assets/Speed-Staes/Tools/Editor/Builders/Ui/**"
  - "Assets/Speed-Staes/Scripts/UI/Costume/CostumeEditorScreenView.cs"
  - "Assets/Speed-Staes/Tests/PlayMode/PlaySmokeSupport.cs"
  - "Assets/Speed-Staes/Tests/PlayMode/HomeRaceButtonSmokeTests.cs"
  - "Assets/Speed-Staes/Tests/PlayMode/ScreenshotTests.cs"
touches_scene: true
branch: agent/T-034-kart-isabet
commits: []
tests:
  - "Assets/Speed-Staes/Tests/PlayMode/HomeRaceButtonSmokeTests.cs"
analytics:
  - "yok: projede analytics yok"
needs: []
started:
finished:
blocked_reason:
---

## Hedef
Kullanıcı (28.10): "karakter edit yapamıyorum". Sebep: `UiV2Prefabs.BuildCharacterCard` kartın butonlarını (Previous, Next, EditRacer, Emote)
kartın tam tuvaline yayıyor (`CardButton` → stretch; sanat katmanları ortak kart tuvalinde). En son kurulan pasif **Emote** en üstte ve
tüm kartın tıklamasını yutuyor; tişört ve oklar gerçek dokunuşta çalışmıyor. PlayMode testleri `onClick.Invoke()` ile bastığı için yakalamadı.
1) Kart butonlarının tıklama alanı kendi görünür bölgesi olsun: katman PNG'sinin alfa sınırlarından (T-024 `UiEventSelectPrefabs` mesafe
   butonlarında aynı yöntem var — onu ortak yardımcıya taşı ya da yeniden kullan) hesaplanan bir hit rect; tam tuval katman görselleri
   `raycastTarget = false`. Aynı sorun başka "ortak tuval" düğmelerinde var mı tara (kostüm, tema, ayarlar, sonuç builder'ları) ve düzelt.
2) `PlaySmokeSupport.Tap` gerçek dokunuşu doğrulasın: butonun ekran merkezinden `EventSystem.RaycastAll` ile en üstteki hedef butonun kendisi
   ya da çocuğu değilse test "X üstünü kapatıyor" diye düşsün; sonra tıklama (Invoke ya da ExecuteEvents.pointerClick).
3) `CostumeEditorScreenView._motion` taban `UiPanel._motion` ile aynı ad (Editor uyarısı "same field name serialized multiple times",
   TUZAKLAR) → türevdekini yeniden adlandır.

## Kabul kriterleri
- [ ] Gerçek ışınlı Tap ile duman testleri yeşil (tişört → kostüm editörü, oklar, diğer bütün butonlar)
- [ ] Editor.log'da `_motion` uyarısı yok
- [ ] Builder'da başka ortak tuval düğmesi kalmadı (çalışma notunda liste)

## Kapsam disi
- Yeni özellik

## Calisma notu
(Agent doldurur)
