---
id: T-040
title: Emote ekrani + prosedurel emote'lar (menu karakteri) - var olan sanatla
status: todo
owner: gameplay-agent
depends_on: [T-026]
owns_paths:
  - "Assets/Speed-Staes/Scripts/Meta/Emotes/**"
  - "Assets/Speed-Staes/Scripts/Meta/Contracts/EmoteContracts.cs"
  - "Assets/Speed-Staes/Scripts/Meta/Persistence/PlayerProgress.cs"
  - "Assets/Speed-Staes/Scripts/Bridge/MenuEmote*.cs"
  - "Assets/Speed-Staes/Scripts/Bridge/MenuIdlePose.cs"
  - "Assets/Speed-Staes/Scripts/UI/Emotes/**"
  - "Assets/Speed-Staes/Scripts/UI/Home/HomeScreenView.cs"
  - "Assets/Speed-Staes/Tools/Editor/Builders/Ui/UiEmotePrefabs.cs"
  - "Assets/Speed-Staes/Tools/Editor/Builders/Ui/UiPrefabBuilder.cs"
  - "Assets/Speed-Staes/Tools/Editor/Builders/Ui/UiV2Prefabs.cs"
  - "Assets/Speed-Staes/Tools/Editor/Builders/UiPrefabPaths.cs"
  - "Assets/Speed-Staes/Tools/Editor/Builders/Scene/SceneWiring.cs"
  - "Assets/Speed-Staes/Tools/Editor/Builders/Scene/SceneHomeCharacter.cs"
  - "Assets/Speed-Staes/Tools/Editor/Builders/Scene/EmoteCatalogSeed.cs"
  - "Assets/Speed-Staes/Tools/Editor/Builders/Scene/ConfigAssetSeeder.cs"
  - "Assets/Speed-Staes/Tests/EditMode/Meta/Emotes/**"
  - "Assets/Speed-Staes/Tests/PlayMode/**"
touches_scene: true
branch: agent/T-040-emote
commits: []
tests:
  - "Assets/Speed-Staes/Tests/EditMode/Meta/Emotes/EmoteServiceTests.cs"
analytics:
  - "yok: projede analytics yok"
needs:
  - tip: WIRING
    durum: open
    not: "ProjectInstaller: EmoteCatalog + IEmoteService AsSingle"
started:
finished:
blocked_reason:
---

## Hedef
Kullanıcı (30.10): "emote ekranını kendin yap, bizi uğraştırma, eldekilerden kullan". Ana ekrandaki karakter kartının **Emote** butonu
(bugün pasif) Emote ekranını açar. Orijinal referans: `docs/eksik-ui/04-emote-secimi.png` (liste, kilitli/fiyatlı, karakterde önizleme,
satın al) — düzen bizim: **kostüm editörünün sanatı** (`Assets/Speed-Staes/UI/Art/Costume/`: wardrobe paneli, item kartı normal/seçili,
kilit, seçili tik, purchase butonu, back, kaynak kutuları) yeniden kullanılır; yeni sanat yok.

İçerik: projede emote animasyonu YOK (karakter prosedürel; `.anim` yok). Emote = **prosedürel kemik poz dizisi**: `MenuIdlePose`'un yaptığı
gibi `LateUpdate`'te kemik rotasyonları (B-upperArm/forearm/hand, B-spine/chest, B-head …) üzerine anahtar kareler (zaman, kemik, euler,
ease) — veri `EmoteCatalog` (ScriptableObject) içinde, kod sabiti değil. Tohum 6 emote (kendi setimiz): Wave, Hands Up, Flex, Number One,
Shrug, Salute; her biri ~1.5-2.5 s, sonra menü duruşuna yumuşak dönüş. İlk ikisi bedava, diğerleri Nakit fiyatlı (yer tutucu fiyat config'te;
PDF'te emote yok — ürün kararı süpervizörde).

Davranış: listede emote'a dokununca menü karakterinde önizleme oynar (kilitli olsa da); sahip olunan = "Equip" (kartta tik), kilitli =
kaynak satın alma butonu fiyatla (`IEconomyLedger` üzerinden Nakit, idempotent; yetersizse sallanma). Seçili (equip) emote kayıtta. Ana
ekranda karakter kartındaki Emote butonu ekranı açar; ekranda Back. Yarış içi emote bu task'ta YOK (IK ile çakışır; sonra).
Emote oynarken `MenuIdlePose` ile çakışmaz (tek yerde karışım: idle poz + emote ağırlığı).

## Kabul kriterleri
- [ ] `IEmoteService` (liste, sahiplik, TryPurchase → sonuç, Equip, Equipped, Changed) + `EmoteCatalog` + kayıt alanları; EditMode testli
      (sabit fiyat assert edilmez)
- [ ] Prosedürel oynatıcı (Bridge) saf kısmı (zaman → kemik açısı, ease, bitiş/dönüş) EditMode testli; MonoBehaviour yalnız uygular
- [ ] Ekran builder'dan; kostüm sanatı; dokunma alanları görünür bölge; legacy Text; DOTween çekirdek (TUZAKLAR)
- [ ] PlayMode: Home Emote butonu → Emote ekranı → bir emote'a dokun → Back (gerçek ışınlı Tap); ekran görüntüsü `Home_emote`
- [ ] needs listesinde open WIRING yok (WIRING süpervizörde)

## Geri bildirim
GORSEL  : karta dokununca punch + karakter emote'u oynar; satın alınca kilit kalkar + punch; yetersizse sallanma
SES     : yok
DOKUNSAL: yok
ZAMAN   : yok
KAMERA  : yok

## Kapsam disi
- Yarış içi emote, gerçek animasyon klipleri, Malik'in prefab/kodu

## Calisma notu
(Agent doldurur)
