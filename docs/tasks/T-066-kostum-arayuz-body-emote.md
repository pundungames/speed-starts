---
id: T-066
title: Kostum arayuzu - "Body" sekmesi "Top" olur, yeni "Body" sekmesi ten rengi secer; Emote sekmesi emote ekranini acar; 9 hazir kostum listesi
status: merged
owner: ui-agent
depends_on: [T-065]
owns_paths:
  - "Assets/Speed-Staes/Scripts/UI/Costume/**"
  - "Assets/Speed-Staes/Tools/Editor/Builders/Ui/UiCostumePrefabs.cs"
  - "Assets/Speed-Staes/Tools/Editor/Builders/Scene/CostumeConfigSeed.cs"
  - "Assets/Speed-Staes/Tests/EditMode/UI/Costume/**"
  - "Assets/Speed-Staes/Tests/PlayMode/**"
touches_scene: true
branch: agent/T-066-kostum-arayuz
commits: [629aec56, b74b1007, fd590f6d]
tests:
  - "Assets/Speed-Staes/Tests/EditMode/UI/Costume/CostumeEditorModelTests.cs"
  - "Assets/Speed-Staes/Tests/PlayMode/CostumeScreenSmokeTests.cs"
  - "Assets/Speed-Staes/Tests/PlayMode/ScreenshotTests.cs (Shots_CostumeBodyAndPresets: Home_costume_body, Home_costume_presets)"
analytics:
  - "yok: projede analytics yok"
needs:
  - tip: SCENE
    durum: closed
    not: "UiBuilder overwrite ile uc prefab uretilir: Rows/CostumeSkinSwatch.prefab (YENI), Rows/CostumePresetRow.prefab degismez ama CosmeticsScreen.prefab YENIDEN uretilir (overwrite): sekme sutunu 10 sekme (Body en ustte, Top=eski Body etiketi, hepsi ~%10 kucuk), preset listesi kaydirmali (5 satir), yeni alanlar swatchPrefab + dropdown.listScroll. Builder cikti: UiCostumePrefabs.BuildAll. Overwrite kullanicinin prefab'taki elle duzenlemelerini ezer: once sor."
  - tip: WIRING
    durum: closed
    not: "CostumeEditorScreenView.emoteScreen (CosmeticsScreen sahne ornegi) -> sahnedeki EmoteScreen nesnesi (HomeScreenView.emoteScreen ile ayni nesne). Prefab sahne nesnesine baglanamaz; sahne builder'i ya da Inspector. Bos kalirsa Emote sekmesi gri kalir. Zenject: ConstructCostumeEditor artik ScreenNavigator + CosmeticCatalog da ister (ikisi de bagli)."
  - tip: ASSET
    durum: closed
    not: "custom_dropdown_list_background.png.meta: Sprite Border L/B/R/T = 20/20/20/20 (liste Sliced cizilir, 9 satirda kenar cizgisi esnemesin; sinir yoksa Sliced=Simple gibi dolar, calisir ama esner). T-065 ASSET maddeleri (presetCount 9, skinTones...) ayrica gerekli; PlayMode testi presetCount >= 9 ister."
started: 2026-10-30
finished: 2026-10-30
blocked_reason:
---

## Hedef
Kullanici (30.10, kostum paneli goruntusuyle): "karakter edit ekraninda body yerine top yazicaz. body content ekle ve o da karakterin ten rengini
degistirsin. alt kisimdaki emote kismi calismiyor orasi da calismali. custom da 3 tane degil 9 tane olsun. shop kismindan da emots a girebilelim".

1) **Top**: bugunku "Body" sekmesi (Tops kategorisi) etiketiyle "Top" olur; ikon ayni (kirmizi tisort). `CostumeTab` enum degerleri
   serilestirilmis config'i bozmayacak sekilde (deger 2 = Top kalir; yeni sekme yeni deger).
2) **Body (ten)**: yeni sekme; liste yerine ten secimi: `CosmeticCatalog.SkinTones` hazir ornekleri (kart/kutu satiri) + var olan renk secici
   (kostumdeki Color paneli) ile serbest ten rengi; secim `CosmeticOutfit.WithSkinColor(hex)` ile onizlemeye aninda, Save ile kayda gider
   (sozlesme T-065'te: `SkinColorHex`, `WithSkinColor`, `SkinTones`, `DefaultSkinColor` — T-065 paralel yaziliyor, imzaya gore yaz). Ikon:
   var olan sanattan (ornek Home'daki kosucu/beden silueti); yeni sanat yok. Sekme sutununda yeri: en ustte ya da Shoes ile Socks arasindaki bos
   yuvada — gorunume en az dokunan secenegi sec, calisma notuna yaz.
3) **Emote**: sutunun altindaki Emote sekmesi bugun pasif (`hasContent = false`). Dokununca **emote ekranini** (T-040 `EmoteScreen`, Home'da
   ayni ekran) acar; emote ekranindan Back kostum ekranina doner. Kostumdeki kaydedilmemis degisiklik kaybolmaz (ya da var olan "kaydetmeden
   cik" kurali neyse o). Emote sekmesi gorunumde aktif.
4) **9 hazir kostum**: ust dropdown "Custom 1..9" listeler (servis `PresetCount`'u kullanir; liste uzarsa kaydirmali), secim/kayit ayni.
Kod kalici UI kurmaz; dokunma alanlari gorunur bolge; DOTween yalniz cekirdek (TUZAKLAR). Prefab/sahne/asset uretimi supervizorde (`needs:`).
Ekran goruntuleri: `Home_costume` (Top etiketi), yeni `Home_costume_body` (ten secimi acik), `Home_costume_presets` (dropdown acik, 9 satir).

## Kabul kriterleri
- [ ] EditMode: sekme modeli (Top=Tops, Body=ten, Emote=ekran acar), ten secimi onizlemeye ve kayda gider (sahte servis)
- [ ] PlayMode: Body sekmesi -> ten ornegi Tap -> kosucu rengi degisir (renderer MPB okunarak); Emote sekmesi -> emote ekrani acilir -> Back;
      dropdown'da 9 satir (gercek isinli Tap)
- [ ] needs: SCENE (+ CostumeEditorConfig ASSET) yazili

## Geri bildirim
GORSEL  : secimde var olan punch; ten aninda kosucuda
SES     : T-052
DOKUNSAL: T-052
ZAMAN   : yok
KAMERA  : yok

## Calisma notu
- Sekmeler: enum Body(2)->Top(2) (degerler korundu), yeni Body=9; davranis `CostumeTabRules` (Items/Skin/Emote) kodda, config'te degil: eski asset'te Emote hasContent=false olsa da sekme ekrandaki emoteScreen'e bakar. Acilis sekmesi yine Face.
- Sutun yeri: Body en ustte; 10 sekme ayni sutuna sigsin diye hepsi orantili ~%10 kuculur (sag kenar sabit), bos yuva yoktu (Shoes-Socks arasi bosluk yok). Ikon: Costume/stats_height_icon (insan silueti), zemin Face sekmesinin; yeni sanat yok.
- Ten: Body sekmesinde ayni izgarada ten kartlari (katalog SkinTones, CostumeSkinSwatch) + sol renk paneli tek kutu ile teni boyar; model `SelectSkin/SetSkinColor` -> `Outfit.WithSkinColor`; Random sadece kiyafet (ten kalir). Emote: Push(emoteScreen); kaydedilmemis kiyafet/ten dondugunde `_keepSession` ile korunur (Back geri alir).
- Presetler: dropdown `PresetCount` kadar satir, liste 5 satirlik pencere + ScrollRect, acilista secili satira kayar. Zorlandigim: prefab/sahne yazma yok, her sey builder + needs; Unity/test kosulmadi. Risk: Sliced sinir meta'si ve sekme kuculme gorunumu supervizor ekran goruntusuyle onaylamali.
