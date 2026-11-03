# ADR-2026-11-03-textmeshpro

Durum: kabul edildi
Tarih: 03.11.2026
Karari veren: kullanici

## Baglam
28.10'da TextMeshPro kaldirildi: oyun Cin pazarina gidecekti ve legacy `UnityEngine.UI.Text`'in dinamik fontu cihazda eksik
CJK harflerini isletim sistemi fontundan aliyordu. 03.11'de kullanici iki sey istedi: yazilarin fontu her yerde American Captain
olsun (dafont, "jersey" onizlemesi) ve TextMeshPro'ya gecelim ("textmesh pro ya gecebiliyormusuz"). O gun prefab'larda 29 dosyada
267 legacy Text, kodda 137 Text alani vardi; kullanici prefab'lari elle duzenlemeye baslamisti (4f38e016).

## Karar
Bizim UI (prefab'lar, Home/Race'teki bizim yazilar, view kodu, testler) TextMeshPro'ya gecer: bilesen `TMPro.TextMeshProUGUI`,
alan tipi `TMP_Text`, font American Captain SDF (yolu tek yerde, `UiFontPaths`). Gecis prefab'lari yeniden URETMEZ: dosya
duzeyinde yerinde donusturulur (bilesen kimligi/fileID ayni kalir, elle duzenlemeler ve referanslar korunur). Iki InputField
legacy kalir (yazisi American Captain TTF). Malik/Serhat'in runtime'da kurdugu legacy Text'ler onlarin alaninda kalir.

## Gerekce
Kullanici karari. TMP keskin yazi, ucuz kontur/golge ve tek font asset'i getirir. Yerinde donusum, kullanicinin elle yaptigi
yerlesimi korur; builder'la yeniden uretmek onu silerdi.

## Sonuclari
- Kolaylasti: font tek asset'ten degisir; kontur materyal preset'iyle.
- Zorlasti: Cince harf. TMP 3.0.7 cihaz fontuna dusemez, American Captain'de CJK yok; yedek font karari acik (oneri: Noto Sans SC,
  OFL, dinamik yedek, APK'ya ~8-10 MB). Karar gelene kadar Cince ad kutu gorunur (legacy kalan InputField'da gorunur).
- Lisans: American Captain'in ucretsiz surumu yalniz kisisel kullanim (EULA md.4: yazilima gomulup aliciya verilemez). Sirket ici
  gelistirme serbest; APK musteriye gitmeden ticari lisans (thefontry.com) ya da OFL bir benzer gerekir.
- Etkilenen: `docs/CALISMA-KURALLARI.md` §0 ve §9 (hook korumali, kullanici gunceller), T-100, T-101, builder'lar (mevcut prefab'lara
  bir daha kosmaz).

## Geri alma sarti
Cince yedek font cozumu cihazda calismazsa ya da APK boyutu kabul edilmezse yeniden acilir.
