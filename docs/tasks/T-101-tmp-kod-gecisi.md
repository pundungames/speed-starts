---
id: T-101
title: TMP gecisi 2/2 - bizim view kodu, testler ve asmdef'ler legacy Text yerine TMP_Text; prefab testleri elle duzenlemeye dayanikli
status: todo
owner: ui-agent
depends_on: []
owns_paths:
  - "Assets/Speed-Staes/Scripts/UI/**"
  - "Assets/Speed-Staes/Scripts/Core/**"
  - "Assets/Speed-Staes/Scripts/Meta/**"
  - "Assets/Speed-Staes/Scripts/Bridge/**"
  - "Assets/Speed-Staes/Tools/Editor/Builders/**"
  - "Assets/Speed-Staes/Tests/**"
touches_scene: false
branch: agent/T-101-tmp-kod
commits: []
tests:
  - "Assets/Speed-Staes/Tests/EditMode/ (Text kullanan butun testler TMP_Text'e)"
  - "Assets/Speed-Staes/Tests/PlayMode/ (Text kullanan butun testler TMP_Text'e)"
analytics:
  - "yok: projede analytics yok"
needs: []
started:
finished:
blocked_reason:
---

## Hedef
Kullanici (03.11): fontlar her yerde American Captain ve TextMesh Pro'ya gecis. Karar: `docs/decisions/ADR-2026-11-03-textmeshpro.md`
(CALISMA-KURALLARI §9'daki "TMP KULLANILMAZ" satiri bu kararla KALKTI; kural dosyasi hook korumali, kullanici guncelleyecek - bu task
TMP kullanir). T-100 (paralel) prefab/sahnelerdeki legacy Text'i dosya duzeyinde yerinde `TextMeshProUGUI`'ye ceviren araci yazar;
bu task KODU cevirir. Supervizor ikisini birlestirip kopyada paketi ekler, donusturucuyu kosar ve dogrular.

**Sozlesme (T-100 ile ortak, degistirme):** donusen her legacy Text ayni GameObject'te, AYNI fileID ile `TMPro.TextMeshProUGUI` olur.
View alanlarinin ADI DEGISMEZ (serilestirilmis referans fileID ile durur), tipi `TMP_Text` olur. InputField'in textComponent'i ve
placeholder'i olan Text'ler legacy KALIR (projede 2 InputField): bunlara bakan alanlar `Text` tipinde kalir, InputField alanlari da.

1) **Runtime kod** (Scripts/UI ve gerekirse Core/Meta/Bridge; bugun ~39 view dosyasi): `[SerializeField] private Text x` -> `TMP_Text x`
   (ad ayni; Tooltip'ler kalir, "legacy Text" diyenler guncellenir). API: `fontSize` int -> float, `alignment` TextAnchor ->
   TextAlignmentOptions, `resizeTextForBestFit` -> `enableAutoSizing`, `font` (Font) -> TMP_FontAsset, `preferredWidth` /
   `cachedTextGenerator` -> TMP karsiligi, `horizontalOverflow` -> sarma/overflow. Kontur rengini runtime'da degistiren kod
   (ornek RaceStartView + `ThickOutline`, Outline.effectColor) TMP materyal ozelliklerine (outlineColor / outlineWidth; paylasilan
   materyali bozmayacak sekilde `fontMaterial`) gecer. `ThickOutline.cs` dosyasi SILINMEZ (donusumden sonra kullanilmiyorsa
   calisma notuna yaz). `**/Dev/**` altindaki runtime Text'ler dokunulmadan kalabilir (yalniz gelistirme).
2) **asmdef'ler:** `SpeedStars.UI` (ve TMP kullanan baska bizim asmdef), `SpeedStars.Tests`, `SpeedStars.PlayTests` -> `Unity.TextMeshPro`
   referansi. TMP 3.0.7 kaynagi okumak icin acik kopya: `C:\Users\Monster\AppData\Local\Temp\claude\d--Pundun-speed-starts\becd309f-c81c-420a-abc6-06ba3e3303fb\scratchpad\tmp307\package`
   (runtime asmdef adi `Unity.TextMeshPro`; tipler `TMPro` ad alaninda).
3) **Builder kodu (Tools/Editor/Builders):** yalniz view tipleri degisince DERLEME bozuluyorsa duzelt; builder'lar artik mevcut prefab'lara
   kosmayacak (T-100 korumasi), uretim mantigini TMP'ye cevirmek kapsam disi. `UiPrefabBuilder.cs`, `Scene/SceneBuilder.cs`,
   `UiFontPaths.cs` T-100'un: YAZMA.
4) **Testler:** Text kullanan butun EditMode/PlayMode testleri `TMP_Text` (InputField'a ait olanlar haric). Prefab okuyan testler
   (Builders/*Tests) YAPI ve BAGLAMA dogrular, kullanicinin elle degistirebilecegi geometriyi (boyut/konum sayisi) SABITLEMEZ: bugun
   `HomeCardBuilderTests.Card_GrowsToTheLeftOnly_RightEdgeAndHeightUnchanged` kullanicinin 4f38e016'daki kart ayariyla KIRMIZI
   ("height": layout.json yuksekligi bekliyor, prefab 574.5). Iliski assert et (saga cakili, pivot sagda, Content'ten genis, Content sagda
   cakili...), layout sayilarini degil. Baska geometri sabitleyen prefab testi gorursen ayni sekilde.
   `SafeAreaTests.cs` ve `SafeAreaPlayTests.cs` T-099'un, `Tests/EditMode/Migration/**` T-100'un: YAZMA.
Malik'in (`Scripts/Physical/**`, `Scripts/Runner/RunnerInput.cs`, `SideFollowCamera.cs`) ve Serhat'in (`Scripts/Runner/**` geri kalani,
`Scripts/Themes/**`) dosyalari okunur, YAZILMAZ (`LateStepFeedback`, `AccessoryPhotoStudio` legacy Text'te kalir).
Prefab/sahne/asset dosyasina YAZMA; Unity/test CALISTIRMA (merkezi dogrulama). Derleme kopyada yapilacak: ozellikle API cevirilerini
TMP kaynagina bakarak yaz, tahminle yazma.

## Kabul kriterleri
- [ ] Bizim kodda (Dev ve InputField'a ait olanlar haric) legacy `Text` alan/tip kullanimi kalmaz; davranis ayni
- [ ] asmdef'ler TMP'yi goruyor; test asmdef'leri TMP tiplerini kullanabiliyor
- [ ] Prefab testleri geometri sabitlemiyor; HomeCardBuilderTests kullanicinin kart ayariyla yesil olacak sekilde
- [ ] Builder kodu derleniyor (uretim mantigi degismedi)
- [ ] Malik/Serhat dosyasi degismedi

## Geri bildirim
GORSEL  : yazilar TMP ile (font donusumu T-100)
SES     : yok
DOKUNSAL: yok
ZAMAN   : yok
KAMERA  : yok

## Kapsam disi
- Prefab/sahne donusumu ve font asset'i (T-100), guvenli alan (T-099), Cince yedek font (karar acik), builder uretim mantiginin TMP'ye cevrilmesi

## Calisma notu
