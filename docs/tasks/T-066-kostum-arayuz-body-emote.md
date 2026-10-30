---
id: T-066
title: Kostum arayuzu - "Body" sekmesi "Top" olur, yeni "Body" sekmesi ten rengi secer; Emote sekmesi emote ekranini acar; 9 hazir kostum listesi
status: todo
owner: ui-agent
depends_on: [T-065]
owns_paths:
  - "Assets/Speed-Staes/Scripts/UI/Costume/**"
  - "Assets/Speed-Staes/Tools/Editor/Builders/Ui/UiCostumePrefabs.cs"
  - "Assets/Speed-Staes/Tools/Editor/Builders/Scene/CostumeEditorConfigSeed.cs"
  - "Assets/Speed-Staes/Tests/EditMode/UI/Costume/**"
  - "Assets/Speed-Staes/Tests/PlayMode/**"
touches_scene: true
branch: agent/T-066-kostum-arayuz
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
