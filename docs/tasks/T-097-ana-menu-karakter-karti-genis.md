---
id: T-097
title: Ana menu karakter karti - sola genisler, mağazadaki Power / Stamina / boy ikon + yazilari solda; Edit racer / Emote kucuk, altinda oyuncu adi
status: todo
owner: ui-agent
depends_on: [T-087]
owns_paths:
  - "Assets/Speed-Staes/Tools/Editor/Builders/Ui/UiV2Prefabs.cs"
  - "Assets/Speed-Staes/Tools/Editor/Builders/Ui/UiViewBinder.cs"
  - "Assets/Speed-Staes/Scripts/UI/Home/HomeScreenView.cs"
  - "Assets/Speed-Staes/Scripts/UI/Home/HomeTexts.cs"
  - "Assets/Speed-Staes/Scripts/UI/Home/RacerCardRules.cs"
  - "Assets/Speed-Staes/Tests/EditMode/UI/Home/**"
  - "Assets/Speed-Staes/Tests/EditMode/UI/Builders/HomeCardBuilderTests.cs"
touches_scene: false
branch: agent/T-097-karakter-karti
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
Kullanici (03.11, ekran goruntusu: ana menudeki kart "Custom 4" secici + turuncu/yesil cubuk + "Height: 5'9" + gomlek / emote butonu):
1) **Kart sola dogru genislesin.** Bugun `UiV2Prefabs.BuildCharacterCard` karti `layout.json` "character_card" dikdortgenine koyuyor ve
   butun katmanlar 491x486 ortak tuvale gerili (Kit.Stretch). Mevcut parcalar (secici, oklar, cubuklar, Height, butonlar) BUGUNKU yerinde ve
   olcusunde kalir; kart yalniz SOLA uzar (sag kenar ve yukseklik ayni). Arka plan (`character_card_background`) gerilip bozulmasin:
   9-slice (Image.Type.Sliced, kenar = cercevenin kalinligi; sprite border'i import ayarinda yoksa builder'da `TextureImporter.spriteBorder`
   ile ver - .meta degisir, needs: ASSET yaz) ya da mevcut parcalari bir "Content" alt dugumune alip arka plani ayri genislet. Genislik:
   yeni sol serit, magazadaki Stats panelinin ikon + yazi sutunu kadar (asagida), en fazla ~%45 ek; ekranda solda baska bir dugmeyle
   (High Scores, Race select vb.) cakisirsa calisma notuna yaz ve cakismayan en genis degeri sec.
2) **Magazadaki panelin sol kismi kopyalanir.** Kostum ekraninin sol altindaki "Stats" paneli (`UiCostumePrefabs.Stats`, ~465. satir):
   her satirda ikon (`Costume/stats_power_icon`, `stats_stamina_icon`, `stats_height_icon`) + "Power" / "Stamina" yazisi (`CostumeTexts`,
   ayni boyut/renk/font) + cubuk. Ana menu kartinin yeni sol seridine ayni ikon + yazilar, kartin KENDI cubuk satirlarinin (PowerBar,
   StaminaBar, HeightField) hizasinda konur; Height satirinda yalniz ikon (magazadaki gibi). Kartin cubuklari ve Height kutusu kalir (kopya cubuk YOK).
   UiCostumePrefabs'a YAZMA (sahibi baska task olabilir): ikon/yazi yerlesimini kendi dosyanda kur; ortak bir yardimci gerekiyorsa
   UiV2Prefabs icinde yaz.
3) **Alttaki gomlek (EditRacer) ve emote (Emote) butonlari kuculur** (~%75; ikisi ortalanmis, aralari esit) ve altlarina **oyuncunun adi**
   yazilir (`IPlayerSettings.PlayerName`, Ayarlar'daki Player Name; ad degisince guncellenir - IPlayerSettings'in degisiklik olayini kullan;
   ad bossa satir bos/gizli). Yazi kartin lacivert rengi, mevcut kart fontu, tek satir, sigmazsa kucul (BestFit) ya da kes.
4) Race of the Day secilince (T-087, `HiddenForRacerOfTheDay`): yeni ikon/yazilar cubuklarla birlikte, oyuncu adi butonlarla birlikte gizlenir.
UI kodla kalici kurulmaz (builder). Prefab/sahne dosyasina YAZMA; builder'i calistirma (merkezi dogrulama, supervizor kopyada calistirir).

## Kabul kriterleri
- [ ] Kart sola genis; sagdaki parcalar ayni yerde, arka plan gerilmemis (kose/kenar kalinligi ayni)
- [ ] Sol seritte Power / Stamina ikon+yazi ve boy ikonu, kart cubuklarinin hizasinda; magazadaki gibi gorunur
- [ ] EditRacer / Emote kucuk, altinda oyuncu adi; Ayarlar'da ad degisince ana menude guncellenir
- [ ] Race of the Day'de yeni parcalar dogru gizlenir
- [ ] EditMode: ad satiri kurali (bos ad, degisim) + builder testi (uretilmis HomeScreen'de yeni dugumler var ve view alanlari bagli; kartin sag kenari ve yuksekligi degismemis)
- [ ] needs'e SCENE: supervizor kopyada UI prefab'lari + Home yeniden uretir (Race YENIDEN KURULMAZ)

## Geri bildirim
GORSEL  : yok (statik yerlesim); ad degisince yazi aninda yenilenir
SES     : mevcut buton Sfx
DOKUNSAL: mevcut
ZAMAN   : yok
KAMERA  : yok

## Kapsam disi
- Power / Stamina degerlerinin anlami (bugun sabit dolum), kostum ekrani, Race sahnesi

## Calisma notu
