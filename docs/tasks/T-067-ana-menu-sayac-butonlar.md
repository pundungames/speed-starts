---
id: T-067
title: Ana menu - sol alttaki Upgrade ve Players butonlari kalkar; tema secimi ve emote ekraninda ana menunun sol sayaclari (kaynak cubugu) aynen kalir
status: merged
owner: ui-agent
depends_on: []
owns_paths:
  - "Assets/Speed-Staes/Scripts/UI/Home/**"
  - "Assets/Speed-Staes/Scripts/UI/Themes/**"
  - "Assets/Speed-Staes/Scripts/UI/Emotes/**"
  - "Assets/Speed-Staes/Tools/Editor/Builders/Ui/UiV2Prefabs.cs"
  - "Assets/Speed-Staes/Tools/Editor/Builders/Ui/UiThemeSelectPrefabs.cs"
  - "Assets/Speed-Staes/Tools/Editor/Builders/Ui/UiEmotePrefabs.cs"
  - "Assets/Speed-Staes/Tools/Editor/Builders/Ui/UiViewBinder.cs"
  - "Assets/Speed-Staes/Tools/Editor/Builders/Scene/SceneBuilder.cs"
  - "Assets/Speed-Staes/Tools/Editor/Builders/Scene/SceneWiring.cs"
  - "Assets/Speed-Staes/Tests/EditMode/UI/**"
  - "Assets/Speed-Staes/Tests/PlayMode/**"
touches_scene: true
branch: agent/T-067-ana-menu
commits: [a05e2170, ac120295, 4ee00888]
tests:
  - "Assets/Speed-Staes/Tests/PlayMode/HomeSharedResourceBarSmokeTests.cs"
analytics:
  - "yok: projede analytics yok"
needs:
  - tip: SCENE
    durum: closed
    not: "Builder yeniden calistirilir (HomeScreen, ThemeSelectScreen, EmoteScreen prefab uretimi + SceneWiring). Home: Upgrade/Players kalkti, HighScores sol alt koseye (upgrade slotuna) kaydi. ThemeSelect/Emote prefablarinda kopya kaynak kutulari (PurpleGem/EntryTicket/TimedTicket/Cash) kalmadi. Sahnede ResourceBar.alsoVisibleOn = [ThemeSelectScreen, EmoteScreen] (SceneWiring doldurur). Prefablarda fileID:0 kalan alan olmamali."
started: 2026-10-30
finished: 2026-10-30
blocked_reason:
---

## Hedef
Kullanici (30.10):
1) "ana menudeki sol alttaki 2 button kalkacak": sol altta uc buton var — Upgrade (mavi ok, pasif), Players (kosucu portresi, pasif), High Scores
   (madalya, calisiyor). **Upgrade ve Players kalkar** (builder `UiV2Prefabs.HomeScreen` `ArtButton(... "Upgrade" ...)`, `"Players"`;
   `HomeScreenView.upgradeButton/playersButton` alanlari ve `DisableUnbuilt` cagrisi). High Scores yerinde kalir (konumu ayni ya da sola
   kayar — sol alt koseye; hangisi daha duzgunse, calisma notuna).
2) "tema secim ekranina girerken ana menudeki soldaki statlar kapaniyor yeni statlar geliyor ama tamamen ayni. ana menudekini kullanalim.
   ve emots panel acildiginda da solda statlar dursun": Tema secim ekrani (`UiThemeSelectPrefabs` kendi dort kaynak kutusunu kuruyor:
   PurpleGem/EntryTicket/TimedTicket/Cash) ve emote ekrani acilinca ana menunun sol kaynak cubugu (`ResourceBar` widget'i, overlay kanvasta,
   `SceneBuilder`/`SceneWiring`) gizleniyor ya da yenisiyle ortuluyor. Hedef: **tek kaynak cubugu** — ana menudeki `ResourceBar` tema secimi ve
   emote ekraninda da ayni yerde gorunur kalir; tema ekranindaki kopya kutular kaldirilir (view alanlari + builder). Emote ekraninda da
   solda sayac kalir. Ekranlarin kendi sol ust ogeleri (Back vb.) sayacla cakismamali — cakisirsa Back'i kaydir, calisma notuna yaz.
   Kostum ekraninin kendi dikey sayac sutunu bu task'ta degismez (kullanici istemedi).
3) Var olan PlayMode testleri (tema secimi, emote, Home butonlari) guncellenir; ekran goruntuleri `Home_themeselect`, `Home_emote`, `Home_play`.

Paralel: T-068 (premium tema gorunumu) da `UiThemeSelectPrefabs`/`ThemeSelectScreenView`'a dokunacak — sen yalniz kaynak kutularini ve
sayac gorunurlugunu degistir, tema adi / USE butonu bolgesine dokunma. Kod kalici UI kurmaz; prefab/sahne uretimi supervizorde (`needs:` SCENE).

## Kabul kriterleri
- [ ] Home'da Upgrade/Players yok, High Scores calisiyor (gercek isinli Tap testi)
- [ ] Tema secimi ve emote ekraninda sol sayac ana menudekiyle ayni nesne, yerinde gorunur; tema ekraninda kopya kutu yok
- [ ] Var olan testler yesil; needs: SCENE yazili

## Geri bildirim
GORSEL  : yok (yerlesim)
SES     : yok
DOKUNSAL: yok
ZAMAN   : yok
KAMERA  : yok

## Calisma notu
1) Sebep: ResourceBarView yalniz Home ust ekranken gorunuyordu (CanvasGroup alpha 0); tema/emote kendi kopya kutularini cizdi. Cozum: ResourceBarView.alsoVisibleOn (UiScreen[]), SceneWiring tema+emote ile doldurur; kopya kutular view+builder dan silindi.
2) Home: Upgrade/Players builder, binder ve view alanlarindan kalkti; HighScores, Upgrade in eski sol-alt x konumuna kaydi (bosluk kalmasin).
3) Back sayacla cakismiyor (Back ustte, bar altinda; emote ta Back y 40-145, bar y 260+); kaydirma yapilmadi.
4) Test: HomeSharedResourceBarSmokeTests (Upgrade/Players yok, HighScores Tap, tema+emote ta bar alpha 1, kopya kutu yok). Calistirilmadi.
5) Prefab/sahne uretimi supervizorde (needs SCENE). ThemeSelectScreenView T-068 ile ayni dosya: yalniz kaynak alanlari ve Redraw sonu degisti.
