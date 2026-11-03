---
id: T-089
title: Emote v2 - prosedurel 6 emote yerine Malik'in pisirdigi 9 animasyon (menu onizleme + yarista emote butonu/tusu + otomatik emote)
status: review
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
tests:
  - "Assets/Speed-Staes/Tests/EditMode/Meta/Emotes/EmotePlaybackTests.cs"
  - "Assets/Speed-Staes/Tests/EditMode/Meta/Emotes/EmoteCatalogMigrationTests.cs"
  - "Assets/Speed-Staes/Tests/EditMode/Meta/Emotes/RaceEmoteRulesTests.cs"
  - "Assets/Speed-Staes/Tests/EditMode/Meta/Emotes/EmotePriceLadderTests.cs"
  - "Assets/Speed-Staes/Tests/PlayMode/EmoteScreenSmokeTests.cs"
analytics:
  - "yok: projede analytics yok"
needs:
  - tip: ASSET
    durum: open
    not: "Data/Config/EmoteCatalog.asset must be rewritten to the v2 set (None + 9 baked clips, prices 3 x 2500 / 3 x 5000 / 3 x 7500): run SpeedStars.Tools.Builders.EmoteCatalogSeed.ApplyEmoteV2 (parameterless, also menu 'Tools/Speed Stars/Build/Apply emote v2'). Until then the old asset has no clipName: every emote acts as None. ConfigAssetSeeder.ApplyEconomyV2 keeps the same ladder (ConfigAssetSeeder.EmotePrice)."
  - tip: SCENE
    durum: open
    not: "Race HUD prefab needs the Emote button: run SpeedStars.Tools.Builders.UiRaceHudPrefabs.PatchRaceHudPrefab (parameterless; adds RaceHud/Emote + Emote/Name and binds RaceHudView.emoteButton/emoteNameText). Also add ONE line to UiV2Prefabs.RaceHud after UiViewBinder.BindRaceHud(root): UiRaceHudPrefabs.AddEmoteButton(root); so a full UI rebuild (EconomyUiRebuild) keeps the button. Race scene: add SpeedStars.Bridge.RaceEmotes to the PLAYER runner root (as SceneCosmetics does for RaceRunnerOutfit), field bots <- the scene's RaceBots; playerEmote stays empty (found on the runner)."
  - tip: WIRING
    durum: open
    not: "RaceSceneInstaller -> [SerializeField] private RaceEmotes raceEmotes; in InstallSceneBindings: if (raceEmotes != null) Container.Bind<IRaceEmoteControl>().FromInstance(raceEmotes).AsSingle(); (usings SpeedStars.Bridge, SpeedStars.Meta.Contracts); scene field raceEmotes <- the component above. RaceHudView injects IRaceEmoteControl optionally: without it the button stays hidden."
  - tip: WIRING
    durum: open
    not: "ProjectInstaller (or RaceSceneInstaller) -> Container.Bind<IEmoteRaceOptions>().To<DefaultEmoteRaceOptions>().AsSingle(); (all three switches true). After T-088 is merged, replace DefaultEmoteRaceOptions by an adapter over IPlayerSettings (ShowEmoteButton / AutoEmoteSelf / AutoEmoteCompetitors + Changed). RaceEmotes and RaceHudView already fall back to the default when nothing is bound."
started: 2026-11-03
finished: 2026-11-03
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
- Yapildi: katalog v2 (EmoteDefinition = id/ad/fiyat/clipName; prosedurel anahtar kareler silindi), "None" + 9 pisirilmis klip; MenuEmotePlayer klibi RunnerEmote.clips'ten ada gore bulup dongu onizleme oynatir; RaceEmotes (Bridge) secili klibi oyuncunun RunnerEmote'una verir (tus + HUD butonu + bitis otomatigi + botlar cizgiyi gecince); RaceHudView butonu; seed + fiyat merdiveni; testler.
- Siralama/fiyat (uzunluga gore, kisa-sade -> uzun-gosterisli): Hip Wave 1.6s, Taunt 4.5s, Silly Dance 4.9s (2.500) | Surprise 5.4s, Hip Hop 7.1s, Air Guitar 8.5s (5.000) | Shuffle 10.2s, Horse Trot 16.8s, Cart Dance 21.7s (7.500); None ucretsiz. Eski id (wave, flex...) katalogda yok -> None.
- Zorluk: RunnerEmote "siradaki klibi" oynatir, adla secemiyor; Malik'in koduna dokunmadan clips dizisine yalniz secili klibi koyuyoruz (GO'dan once bos: tus da calismaz). OnGUI adi dosya adini (sarki adi) gosterdigi icin showName kapatildi, HUD gorunen adi gosterir.
- Bot otomatik emote'u RaceBots'a yazmadan: RunnerRoots + RunnerBody.Sim.Distance okunur (karar gerekmedi). Oyuncu bitisi sonraki karede oynatilir (RunReset ayni karede RunnerEmote'u durdurur).
- Birakilan: ASSET/SCENE/WIRING (needs); Unity calistirilmadi, hicbir test kosulmadi; yeni .cs dosyalarinin .meta'si Unity'den gelecek.
