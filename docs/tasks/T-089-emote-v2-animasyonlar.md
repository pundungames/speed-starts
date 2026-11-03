---
id: T-089
title: Emote v2 - prosedurel 6 emote yerine Malik'in pisirdigi 9 animasyon (menu onizleme + yarista emote butonu/tusu + otomatik emote)
status: todo
owner: gameplay-agent
depends_on: [T-084]
owns_paths:
  - "Assets/Speed-Staes/Scripts/Meta/Emotes/**"
  - "Assets/Speed-Staes/Scripts/Meta/Contracts/EmoteContracts.cs"
  - "Assets/Speed-Staes/Scripts/Bridge/MenuEmotePlayer.cs"
  - "Assets/Speed-Staes/Scripts/Bridge/RaceEmotes.cs"
  - "Assets/Speed-Staes/Scripts/UI/Emotes/**"
  - "Assets/Speed-Staes/Scripts/UI/Race/RaceHudView.cs"
  - "Assets/Speed-Staes/Tools/Editor/Builders/Scene/EmoteCatalogSeed.cs"
  - "Assets/Speed-Staes/Tools/Editor/Builders/Scene/ConfigAssetSeeder.cs"
  - "Assets/Speed-Staes/Tools/Editor/Builders/Ui/UiEmotePrefabs.cs"
  - "Assets/Speed-Staes/Tools/Editor/Builders/Ui/UiRaceHudPrefabs.cs"
  - "Assets/Speed-Staes/Tests/EditMode/Meta/Emotes/**"
  - "Assets/Speed-Staes/Tests/EditMode/UI/Emotes/**"
  - "Assets/Speed-Staes/Tests/PlayMode/EmoteScreenSmokeTests.cs"
touches_scene: false
branch: agent/T-089-emote-v2
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
Kullanici (03.11): "emote'lar calisiyor mu, onlari da yapalim; atilan animasyonlari kullan, kendin yaptin". Bugun T-040'in 6 prosedurel emote'u
(`Meta/Emotes/*`: EmotePlayback/EmoteSampler/EmoteTrack, `Bridge/MenuEmotePlayer`) menu karakterinde oynuyor. Malik 30.10'da (eeb4b57a)
9 Mixamo animasyonunu ekledi ve pisirdi: `Assets/Speed-Staes/Animations/*.fbx` + `Animations/Baked/*.asset` (`RunnerEmoteData`), oynatici
`Scripts/Physical/RunnerEmote.cs` (kosu ustune ust govde; `RunnerInput` emote tusu). Yapilacak:
1) Emote katalogu bu 9 pisirilmis klibe baglanir (prosedurel parcalar kalkar ya da kullanilmaz hale gelir; kayitli secim/sahiplik bozulmaz,
   bilinmeyen eski id = "None"). Gorunen adlar Ingilizce ve genel (sarki/marka adi yok: ornegin "Gangnam Style" yerine kendi adimiz).
2) **Magaza (kullanici: "shop'taki emote kismina koy, unutma")**: kostum ekranindaki Emote sekmesinin actigi emote magazasi
   (`UI/Emotes/EmoteScreenView` + `UiEmotePrefabs` kart satiri) 9 klibi kart olarak listeler: ad, fiyat (Nakit), kilit / sahip / secili durumu,
   satin al (Nakit yetersizse pasif + titreme), sec; secilen kart karakterde onizlenir. Home karakteri de secilen klibi oynatir;
   tam govde (ya da RunnerEmote'un yaptigi gibi), onizleme dongusu.
3) Yaris: HUD'da emote butonu (+ mevcut emote tusu) secili emote'u oyuncunun `RunnerEmote`'u ile oynatir; GO'dan once/bitiste davranis
   referanstaki gibi (bitiste serbest). T-088 ayni anda `IPlayerSettings`'e ShowEmoteButton / AutoEmoteSelf / AutoEmoteCompetitors ekliyor;
   sen ona BAGLANMA: `EmoteContracts.cs`'e kendi seam'ini ac (`IEmoteRaceOptions { bool ShowEmoteButton; bool AutoEmoteSelf;
   bool AutoEmoteCompetitors; event Action Changed; }` + hepsi true donen varsayilan uygulama); supervizor T-088'den sonra IPlayerSettings'e
   baglayan adaptoru ekler (needs: WIRING). `PlayerProgress`'e yazma (T-088'in); emote secimi mevcut alanlarda. ShowEmoteButton kapaliysa buton gizli.
   Otomatik emote: `AutoEmoteSelf` ise oyuncu bitiste secili emote'u kendiliginden yapar; `AutoEmoteCompetitors` botlar icin - botlara
   erisim `RaceBots.cs` uzerinden gerekiyorsa (T-088 o dosyada calisiyor) YAZMA, needs: DECISION/WIRING ile ne gerektigini yaz.
4) Fiyat: "None" ucretsiz; 9 emote sirayla 3 x 2.500, 3 x 5.000, 3 x 7.500 Nakit (referans kademeleri; sira: kisa/sade -> uzun/gosterisli,
   gerekcesi calisma notunda). Seeder (`EmoteCatalogSeed` / `ConfigAssetSeeder` emote fiyat bolumu) buna gore; asset'e sen yazmazsin (needs: ASSET).
Malik'in dosyalari (`Scripts/Physical/**` - RunnerEmote, RunnerEmoteData, RunnerEmoteBaker dahil; `Scripts/Runner/RunnerInput.cs`) okunur,
yazilmaz; public API'leriyle kullanilir. RunnerEmote kosucu prefab'inda yoksa ya da API yetmiyorsa needs: DECISION (Malik'ten ne lazim).
UI kalici olarak kodla kurulmaz (builder). Installer'a yazma (needs: WIRING, tam satir). Once oku: docs/TUZAKLAR.md, docs/tasks/T-040-emote.md.

## Kabul kriterleri
- [ ] Emote magazasinda 9 klip kart olarak listelenir (fiyat, kilit/sahip/secili), secilen klip karakterde oynar; satin alma Nakit'le (fiyat config'ten)
- [ ] Yarista buton/tus secili emote'u oynatir; ayar kapaliyken buton yok; AutoEmoteSelf bitiste oynatir
- [ ] Eski kayit (prosedurel emote id'si) acilir, "None"a duser; testli
- [ ] EditMode testleri (katalog eslemesi, secim/sahiplik gocu, buton durumu); PlayMode duman testi gercek tiklamayla
- [ ] needs'te WIRING/SCENE/ASSET tam

## Kapsam disi
- Yeni animasyon uretimi; Malik'in kodu; ayar satirlari (T-088)

## Geri bildirim
GORSEL  : buton basisinda punch; emote adi kisa sure (RunnerEmote.showName varsa onu kullan)
SES     : mevcut buton Sfx
DOKUNSAL: mevcut hafif ton
ZAMAN   : yok
KAMERA  : yok

## Calisma notu
(Agent doldurur: 5 satir)
