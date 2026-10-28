---
id: T-027
title: Tema secimi ekrani (Tema_Secimi_UI_v1) - oklarla onizleme, fiyat / USE / IN USE, Back onizlemeyi iptal eder
status: todo
owner: ui-agent
depends_on: [T-023]
owns_paths:
  - "Assets/Speed-Staes/UI/Art/ThemeSelect/**"
  - "Assets/Speed-Staes/Scripts/UI/Themes/**"
  - "Assets/Speed-Staes/Tools/Editor/Builders/Ui/UiThemeSelectPrefabs.cs"
  - "Assets/Speed-Staes/Tools/Editor/Builders/Ui/UiPrefabBuilder.cs"
  - "Assets/Speed-Staes/Tools/Editor/Builders/UiPrefabPaths.cs"
  - "Assets/Speed-Staes/Tools/Editor/Builders/Ui/UiViewBinder.cs"
  - "Assets/Speed-Staes/Tests/EditMode/UI/Themes/**"
touches_scene: true
branch: agent/T-027-tema-secimi
commits: []
tests:
  - "Assets/Speed-Staes/Tests/EditMode/UI/Themes/ThemeSelectPresenterTests.cs"
analytics:
  - "yok: projede analytics yok"
needs:
  - tip: WIRING
    durum: open
    not: "Home: HomeScreenView.themeSelectScreen = ThemeSelectScreen prefab'i (UiPrefabPaths.HomeScreens listesine); view IThemeService + IWallet + IEntryTickets alir"
started:
finished:
blocked_reason:
---

## Hedef
Ana ekranın sağ üstündeki environment (resim) butonu tema seçimi ekranını açar. Sanat: `C:\Users\Monster\Downloads\Tema_Secimi_UI_v1.zip`
(açılmış hâli süpervizör scratchpad'inde `tema-ui\`; `BEHAVIOR.md` = davranış tablosu, `button_states.json` = aksiyon butonu durumları,
`layout.json` referans 1678×937, `design_preview.jpg` / `preview_use_state.jpg` kontrol görselleri). Ekranda karakter ve kaide yok
(T-023'ün `MenuThemeView`'i tarama sırasında gizler); yalnız dünya + UI. Sol üst Back, solda 4 kaynak kutusu (Mor Mücevher, Adet Bileti,
Süreli Bilet, Nakit; sayılarıyla), sağ altta önceki/sonraki okları + tema adı + aksiyon butonu.
- Açılış: `BeginBrowse`, aktif tema gösterilir. Oklar `Preview` ile sıradakini gösterir (para yok, kayıt yok); uçlarda ok pasif (sarma yok).
- Aksiyon butonu `ThemeActionState`'e göre (T-023): satın alınabilir = fiyat + Nakit ikonu, tık = `TryPurchase`; yetersiz bakiye = gri,
  pasif; sahip/aktif değil = "USE", tık = `Use`; aktif = "IN USE", lacivert, pasif; fiyatsız (0) = pasif. Bir anda tek zemin açık.
- Back: `CancelBrowse` (kayıtlı tema geri gelir), ekran kapanır.
- Metin legacy Text, İngilizce (tema adı katalogdaki görünen addan).

## Kabul kriterleri
- [ ] Sanat `UI/Art/ThemeSelect/`'e PNG olarak; prefab builder `layout.json`'u var olan `UiArt` ile okur; prefab Home ekran listesinde
- [ ] Saf sunucu sınıfı (ok pasifliği, buton durumu → zemin/metin/ikon/etkileşim eşlemesi) EditMode testli; DOTween/Zenject tipi yok
- [ ] Back kayıtlı temayı geri getirir; USE sonrası buton IN USE olur; satın alma sonrası USE görünür
- [ ] needs listesinde open WIRING/SCENE yok (WIRING süpervizörde)

## Geri bildirim
GORSEL  : ok/aksiyon butonu ButtonFeedback; satın alma başarılı → punch; yetersiz bakiye → buton pasif gri (sallanma yok, pasif)
SES     : yok (ses sistemi yok)
DOKUNSAL: yok
ZAMAN   : yok
KAMERA  : yok (kamera sabit)

## Kapsam disi
- Tema servisi ve sahnedeki tema/karakter gizleme (T-023), tema fiyatları (asset'te; kullanıcı vermedi)

## Calisma notu
(Agent doldurur)
