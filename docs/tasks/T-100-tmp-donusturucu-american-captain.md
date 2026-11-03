---
id: T-100
title: TMP gecisi 1/2 - American Captain SDF font asset'i + prefab/sahnelerdeki legacy Text'i YERINDE TextMeshProUGUI'ye ceviren donusturucu + builder'larin elle duzenlenmis prefab'lari ezmemesi
status: merged
owner: ui-agent
depends_on: []
owns_paths:
  - "Assets/Speed-Staes/Tools/Editor/Migration/**"
  - "Assets/Speed-Staes/Tools/Editor/Builders/UiFontPaths.cs"
  - "Assets/Speed-Staes/Tools/Editor/Builders/Ui/UiPrefabBuilder.cs"
  - "Assets/Speed-Staes/Tools/Editor/Builders/Scene/SceneBuilder.cs"
  - "Assets/Speed-Staes/Tests/EditMode/Migration/**"
touches_scene: false
branch: agent/T-100-tmp-donusturucu
commits: [1346a514, 8d660f5d, 150a9cad, 933b4706]
tests:
  - "Assets/Speed-Staes/Tests/EditMode/Migration/"
analytics:
  - "yok: projede analytics yok"
needs:
  - tip: ASSET
    durum: closed
    not: "Supervizor: Packages/manifest.json'a com.unity.textmeshpro 3.0.7; American Captain TTF'i Assets/Speed-Staes/UI/Fonts/AmericanCaptain/AmericanCaptain.ttf (+ lisans metinleri yaninda). Kopyada TMP Essential Resources + TmpMigration (font asset, preset'ler, TMP Settings varsayilan font) calisir; ciktilar (Assets/TextMesh Pro, font asset/materyal/preset, .meta, packages-lock) commit edilir. -> ed926c5a + 933b4706"
  - tip: SCENE
    durum: closed
    not: "Supervizor kopyada TmpMigration donusturucusunu EN SON main'in prefab'lari ve Home.unity / Race.unity uzerinde kosar (Race YENIDEN KURULMAZ, yalniz legacy Text dokumanlari degisir); git diff ile yalniz Text/Outline dokumanlarinin ve ilgili override'larin degistigi kontrol edilir. -> 933b4706 (kopyada 03.11, kullanicinin 4f38e016 prefab'lari uzerinde)"
started: 2026-11-03
finished: 2026-11-03
blocked_reason:
---

## Hedef
Kullanici (03.11): "text fontlarini degistirmemiz lazim - American Captain'i (dafont) tum her yerde kullanabilir misin - ve
TextMesh Pro'ya gecebiliyormusuz". Karar: `docs/decisions/ADR-2026-11-03-textmeshpro.md` (CALISMA-KURALLARI §9'daki "TMP KULLANILMAZ"
satiri bu kararla KALKTI; kural dosyasi hook korumali, kullanici guncelleyecek - bu task TMP kullanir).
Kullanici prefab'lari ELLE duzenliyor (4f38e016: HomeScreen kart 708x575, HighScores kaydirma, odul kartlari, EntryGate...). Bu yuzden
prefab'lar builder'la YENIDEN URETILMEZ; legacy Text dosya duzeyinde yerinde donusturulur. Bu task araci yazar; T-101 (paralel) view
kodunu, testleri ve asmdef'leri TMP'ye cevirir.

**Sozlesme (T-101 ile ortak, degistirme):** donusen her legacy Text ayni GameObject'te, AYNI dokuman/fileID ile `TMPro.TextMeshProUGUI`
olur (script guid `f4688fdb7df04437aeb418b961361dc5`); view alanlarinin adi degismez, tipi `TMP_Text` olur, referans fileID'si ayni
kaldigi icin baglanti kopmaz. InputField'in textComponent'i ve placeholder'i olan Text'ler legacy KALIR (projede 2 InputField; Cince
ad girisi orada cihaz fontundan gorunur), yalniz fontlari American Captain TTF olur.

1) **Tek giris noktasi** `SpeedStars.Tools.Migration.TmpMigration.Run` (batchmode `-executeMethod`; idempotent, en son main'in
   prefab'lari uzerinde tekrar kosturulabilir; ikinci kosu hicbir dosyayi degistirmez). Gerekirse ayri adim `ImportEssentials`
   (paket import'u derleme/yeniden yukleme tetikler; ayri batchmode kosusu). Isaret satirlari: `TMP_MIGRATION_OK` / `TMP_MIGRATION_FAILED`
   + dosya basina donusen/atlanan sayisi, olusan preset'ler, kalan legacy Text listesi.
   a. TMP Essential Resources yoksa (`Assets/TextMesh Pro/Resources/TMP Settings.asset`) import eder (TMP 3.0.7:
      `TMPro.TMP_PackageResourceImporter.ImportResources(true, false, false)`; paket kaynagi okumak icin acik kopya:
      `C:\Users\Monster\AppData\Local\Temp\claude\d--Pundun-speed-starts\becd309f-c81c-420a-abc6-06ba3e3303fb\scratchpad\tmp307\package`).
   b. Font asset: `UiFontPaths.AmericanCaptainTtf` (= `Assets/Speed-Staes/UI/Fonts/AmericanCaptain/AmericanCaptain.ttf`, supervizor koyar)
      -> `AmericanCaptain SDF.asset` ayni klasorde: dinamik, SDFAA, ornekleme ~90, padding konturlar icin yeterli (~9), 1024x1024,
      multi-atlas acik. TMP Settings varsayilan font = bu asset; yedek (fallback) listesine DOKUNMA (Cince karari acik).
   c. **Donusturucu (dosya duzeyi, YAML):** kapsam `Assets/Speed-Staes/UI/**/*.prefab` + `Assets/Speed-Staes/Scenes/Menu/Home.unity` +
      `Assets/Speed-Staes/Scenes/Game/Race.unity` (bu iki sahnede yalniz `LoadingLabel` legacy Text; Race YENIDEN KURULMAZ). Dosyada legacy
      Text (script guid `5f7201a12d95ffc409449d95f23cf332`) disinda hicbir sey degismez. Neden dosya duzeyi: T-101'den sonra view alanlari
      `TMP_Text` tipinde; Unity prefab'i once yukleyip sonra bileseni degistirirse tip uyusmazligi referansi null yapar ve kaydedince
      baglanti kaybolur. Donusum dosyada yapilir, sonra Unity import + `AssetDatabase.ForceReserializeAssets` ile normallestirilir.
      Eslemeler (tek yerde, tablo gibi okunur): metin, renk, font -> American Captain SDF + materyali, boyut, BestFit -> auto size
      (min/max), TextAnchor (9) -> yatay/dikey hizalama, Horizontal/Vertical Overflow -> sarma/overflow modu, satir araligi, rich text,
      raycast target, maskable. FontStyle Bold/Italic -> Normal (American Captain zaten kalin ve duz; kullanici fontu duz onizlemeden secti).
      Ayni GameObject'teki `Outline` (guid `e19747de3f5aca642ab2be37e372fb86`, 23 adet) ve `ThickOutline` (RaceStart): bilesen
      kaldirilir (dokuman + GameObject'in m_Component satiri), yerine renk/kalinliga gore bir TMP materyal preset'i
      (`UiFontPaths` preset klasoru, her benzersiz renk+kalinlik icin bir tane, adinda renk/kalinlik); kalinlik effectDistance / font
      boyutundan yaklasik.
      Ic ice prefab override'lari (PrefabInstance m_Modifications) donusen bir Text'i hedefliyorsa yol cevrilir (`m_Text` -> TMP alani,
      `m_FontData.*`, `m_Color`); bugun 22 adet `m_Text` override'i `UI/Popups/SettingsPopup.prefab`'ta.
      InputField'a ait Text'ler (yukarida): donusmez, `m_Font` -> American Captain TTF.
   d. Rapor: donusmeyen legacy Text kalirsa dosya/yol ile listelenir (Malik/Serhat'in runtime'da kurdugu Text'ler kapsam disi).
2) **Builder korumasi:** `UiPrefabBuilder.BuildAll(overwrite: true)` VAR OLAN bir prefab'i artik ezmez (log + atla), yalniz eksik
   prefab'i uretir ve uretilen yeni prefab'a donusturucuyu uygular (cikti TMP). Ezmek icin acik bir zorlama bayragi (ornek komut satiri
   `-forceOverwriteHandEdits`); `SceneBuilder.BuildHome` de var olan Home.unity'yi ayni kuralla ezmez (Race zaten yasak, TUZAKLAR).
   Bu, ayni tuzaga ikinci dusus (Malik'in elle ayarlari T-091'de builder'a islenmisti; kullanicinin 4f38e016'daki ayarlari yok).
3) `UiFontPaths.cs` (Tools/Editor/Builders): TTF, SDF asset ve preset klasoru yollari - tek kaynak.
4) **Testler (EditMode):** donusumun saf kismi (YAML metni girer, metin cikar) ornek parcalarla: Text dokumani -> TMP dokumani
   (fileID, m_GameObject, m_Enabled ayni; alan eslemeleri), Outline kaldirilinca m_Component listesi, override yolu cevirisi,
   InputField'a ait Text atlanir, ikinci kosu degisiklik uretmez. Test asmdef'i editor koduna erisemez (TUZAKLAR 173/197): saf kismi
   ya ayri bir editor asmdef'ine koy ve test asmdef'inden referans ver ya da T-057'deki gibi reflection.
Prefab/sahne/asset dosyasina YAZMA, Unity/builder/test CALISTIRMA (merkezi dogrulama; supervizor kopyada kosar).

## Kabul kriterleri
- [ ] `TmpMigration.Run` tek kosuda: TMP Settings + American Captain SDF + preset'ler; kapsamdaki butun legacy Text TMP (InputField'a ait 2+2 haric); ikinci kosu sifir degisiklik
- [ ] Donusen bilesenlerin fileID'leri ayni; view referanslari (kodda `TMP_Text`) dolu kalir; Outline/ThickOutline yerine preset
- [ ] Ic ice override'lar (SettingsPopup'taki 22 `m_Text`) yeni yolda, kaybolmadi
- [ ] BuildAll var olan prefab'i ezmiyor, Home.unity'yi BuildHome ezmiyor (zorlama bayragi haric)
- [ ] EditMode testleri var (saf donusum); sabit denge sayisi yok

## Geri bildirim
GORSEL  : yazilar American Captain, konturlar TMP materyaliyle
SES     : yok
DOKUNSAL: yok
ZAMAN   : yok
KAMERA  : yok

## Kapsam disi
- View kodu, testlerin Text -> TMP_Text cevirisi, asmdef'ler (T-101); Cince yedek font (karar acik); italik/ boyut ayari (gozle bakildiktan sonra)
- Malik'in `LateStepFeedback` ve Serhat'in `AccessoryPhotoStudio` runtime Text'leri

## Calisma notu
1. Yapildi: saf YAML donusturucu (Tools/Editor/Migration/Yaml, kendi asmdef'i SpeedStars.Migration.Yaml) + Unity tarafi TmpMigration (Run / ImportEssentials / menu 'Tools/Speed Stars/Convert UI Text to TMP (American Captain)', Play ve kirli sahne/prefab stage'de reddeder) + UiFontPaths + builder korumasi (-forceOverwriteHandEdits yoksa var olan prefab/Home ezilmez; yeni uretilen prefab/Home hemen TMP'ye cevrilir) + testler (kendi asmdef'i SpeedStars.Tests.Migration: SpeedStars.Tests asmdef'i T-101'in).
2. Sira (kopyada): Packages/manifest.json'a TMP 3.0.7 + TTF -> batchmode `-executeMethod SpeedStars.Tools.Migration.TmpMigration.ImportEssentials` -> ayri kosuda `-executeMethod SpeedStars.Tools.Migration.TmpMigration.Run` (T-101'in view kodu birlesmeden once degil, sonra; Run en son main'in prefab'larinda tekrar kosulabilir) -> ikinci Run 'Nothing to change' demeli. Grep: TMP_MIGRATION_OK / TMP_MIGRATION_FAILED, 'Legacy Text left' (beklenen 2 InputField = 4 Text), 'Warnings:'.
3. Esleme tablosu LegacyTextConverter.cs basinda; FontStyle Bold/Italic -> Normal, BestFit -> auto size, line spacing (x-1)*100, dikey Truncate yalniz sarmada TMP Truncate. Outline kalinligi = mesafe/fontBoyu*10 (0.05 adimla, 0.05..0.8), preset adi 'AmericanCaptain SDF - Outline RRGGBBAA wNN' (UI/Fonts/AmericanCaptain/Presets). Gozle bakilacak: boyut/kontur gorunumu.
4. RISK/T-101: RaceStartView.wordOutline (ThickOutline) bilesen silinince prefab'ta {fileID: 0} olur; T-101 bu alani kaldirmali ya da null korumali (kod zaten != null kontrollu, ama ThickOutline.Configure/SetColor TMP'de calismaz; kontur kalinligi/rengi artik materyal preset'inde, fade yok). Override'lardan m_FontData.m_Font/Alignment/Style vb. karsiligi olmayanlar dusurulur ve rapora 'Warnings' olarak yazilir. Race'in BuildRace yolu degismedi.
5. Dogrulanmadi: hicbir sey derlenmedi/kosulmadi. En belirsiz yerler: TMP_FontAsset.CreateFontAsset sonrasi sub-asset kaydi (TMP'nin kendi creator mantigi izlendi), TMP dokumani alan listesi (kaynaktan dogrulandi, ForceReserialize normallestirir), nested asmdef'in Tests/EditMode altinda calismasi.
6. Duzeltme: stripped proxy Text'leri (ust prefab/sahnede kendi m_Script'i olan) kaynagi TMP olunca TMP'ye cevrilir (ProxiesSwitched). Batchmode'da ImportEssentials async: bunun yerine Unity -batchmode -quit -importPackage <proje>/Library/PackageCache/com.unity.textmeshpro@3.0.7/Package Resources/TMP Essential Resources.unitypackage.
- Supervizor 03.11: ImportEssentials batchmode'da bitmiyor (async import, Unity once kapaniyor) -> `-importPackage` ile; ilk kosuda ic ice prefab'larin stripped Text vekilleri legacy kaldi, SettingsPopup'ta 22 baglanti koptu (8d660f5d ile duzeldi). Son kosu: 243 donustu, ikinci kosu sifir; EditMode 1198, PlayMode 52/7, Zenject yesil; kopan referans 0.
