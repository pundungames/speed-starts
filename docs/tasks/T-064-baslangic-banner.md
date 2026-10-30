---
id: T-064
title: Yaris basi banner (Yaris_Baslangic_Banner_UI_v1) - READY/SET/GO dama bayrakli krem banner icinde, arkada lacivert dim
status: review
owner: ui-agent
depends_on: [T-063]
owns_paths:
  - "Assets/Speed-Staes/UI/Art/RaceStart/Banner/**"
  - "Assets/Speed-Staes/Scripts/UI/Race/RaceStart*.cs"
  - "Assets/Speed-Staes/Scripts/Core/Runtime/Race/RaceStartConfig.cs"
  - "Assets/Speed-Staes/Tools/Editor/Builders/Ui/UiRaceStartPrefabs.cs"
  - "Assets/Speed-Staes/Tools/Editor/Builders/Scene/RaceStartConfigSeed.cs"
  - "Assets/Speed-Staes/Tests/EditMode/UI/Race/**"
touches_scene: true
branch: agent/T-064-baslangic-banner
commits: []
tests:
  - "Assets/Speed-Staes/Tests/EditMode/UI/Race/RaceStartMotionTests.cs"
analytics:
  - "yok: projede analytics yok"
needs:
  - tip: SCENE
    durum: open
    not: "RaceStart prefab yeniden uretilsin (UiRaceStartPrefabs; Banner PNG'leri once Sprite olarak import edilir: Tools > UI art import): yeni BackdropRoot (StartDimScreen duz renk Image + StartVisualRoot[start_banner_background, start_banner_flag]) Group'un onunde; RaceStartView.backdropRoot = BackdropRoot, wordText yazi alani 540x137 x bannerScale. Race sahnesindeki RaceStart ornegi guncellensin; sonra Shots_RaceStartPhases 6 kare"
  - tip: ASSET
    durum: open
    not: "Data/Config/RaceStartConfig.asset: yeni alanlar kod varsayilaniyla gelir (bannerScale 1.39, bannerWordFontSize 132, readySetColor #061A41, dimColor 6/26/65/135, readySetOutline false) - elle bir sey gerekmez. Eski wordColor/wordFontSize artik okunmaz. groupScale 1.7 yalniz hiz cizgileri ve poz ofsetleri icin kalir"
started: 2026-10-30
finished: 2026-10-30
blocked_reason:
---

## Hedef
Kullanici (30.10) `Yaris_Baslangic_Banner_UI_v1.zip` gonderdi (acilmis: supervizor scratchpad'i
`C:\Users\Monster\AppData\Local\Temp\claude\d--Pundun-speed-starts\010558a9-f012-4284-b1f2-1285cf76ea87\scratchpad\banner-ui\`;
IMPLEMENTATION.md **baglayici**, layout.json, approved_design.jpg (uc asama), preview_no_text.jpg; PNG: `start_banner_background.png` 864x184,
`start_banner_flag.png` 864x184 (ayni tuval), `start_dimscreen.png` 1678x937 duz renk RGBA 6,26,65,135). PNG'leri
`Assets/Speed-Staes/UI/Art/RaceStart/Banner/` altina kopyala (.meta'yi Unity uretir). Dim icin T-057 kurali: PNG yerine duz renkli `Image`
(`UiArt.PaintDim` ya da ayni renk) — gorunum ayni, doku yok.

Katman: sahne/HUD -> StartDimScreen (tam ekran, raycast kapali) -> StartVisualRoot: banner background + bayrak (ayni 864x184 rect, merkez pivot,
grup birlikte olceklenir, ekranin orta bolgesi; onaylanan gorselde banner ekran genisliginin ~%50'si) -> mevcut hareketli kelime (TextRoot)
**ayri sibling** olarak ustte. Kelime banner'in yazi alanina ortalanir (yerel sol-ust 242,23, 540x137, grup olcegiyle); READY ve SET **lacivert**
dolgu, GO! **kirmizi** dolgu + lacivert kontur (T-059/T-063 ThickOutline; READY/SET icin kontur gerekmiyorsa kapat ya da ince — onaylanan
gorsele bak). Kelime boyutu yazi alanina sigar (T-063'teki buyuk boy banner'a gore yeniden ayarlanir; config'te). GO! buyurken maske ile
kesilmez. **Animasyon sureleri/easing/faz olaylari/sesler/girdi kilidi DEGISMEZ**; dim ve banner yalniz geri sayim gorunurken acik, gosterge
kapaninca/iptalde kapanir (mevcut temizlikte). Yeni zamanlayici yok. Butun grafiklerde raycastTarget kapali.
`Shots_RaceStartPhases` (T-059) 6 kareyle kontrol edilir; gerekiyorsa kareler banner'li haliyle ayni kalir.
Kod kalici UI kurmaz; DOTween yalniz cekirdek; prefab/sahne/asset uretimi supervizorde (`needs:`).

## Kabul kriterleri
- [ ] 6 karede banner + bayrak + dim gorunur, kelime yazi alaninda ortali; READY/SET lacivert, GO kirmizi; GO kesilmiyor
- [ ] Faz/sure davranisi ayni (var olan RaceStart testleri yesil); gorunurluk kurali (geri sayim disinda kapali) EditMode testli
- [ ] needs: SCENE (+ ASSET gerekiyorsa) yazili

## Geri bildirim
GORSEL  : bu task
SES     : T-052 korunur
DOKUNSAL: yok
ZAMAN   : degismez
KAMERA  : yok

## Calisma notu
Banner: BackdropRoot (dim + banner/bayrak) Group'un kardesi, altinda; RaceStartView Apply/Hide ile Group'la birlikte acip kapatir (yeni zamanlayici yok).
Kelime kutusu = yazi alani 540x137 x bannerScale (1.39 -> banner ekran genisliginin ~%50'si); banner, yazi alani Group merkezine gelecek sekilde kaydirildi.
READY/SET lacivert, kontur kapali (readySetOutline); GO! kirmizi + lacivert kontur. Sure/easing/faz/ses/girdi dokunulmadi; grup maskesi yok.
Kural RaceStartMotion.BackdropVisible (saf) ile EditMode testli; prefab/sahne uretimi supervizorde (needs).
Derlenmedi/test kosulmadi (kural). Dim PNG kopyalanmadi, config dimColor duz Image.
