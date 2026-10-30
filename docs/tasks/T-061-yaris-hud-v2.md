---
id: T-061
title: Yaris HUD v2 - kullanicinin png.rar parcalari (kirpilmis, dogal boyut) + referans ekrana birebir yerlesim
status: todo
owner: ui-agent
depends_on: []
owns_paths:
  - "Assets/Speed-Staes/Tools/Editor/Builders/Ui/UiV2Prefabs.cs"
  - "Assets/Speed-Staes/Tools/Editor/Builders/Ui/UiViewBinder.cs"
  - "Assets/Speed-Staes/Tools/Editor/Builders/Ui/UiArt.cs"
  - "Assets/Speed-Staes/Scripts/UI/Race/RaceHud*.cs"
  - "Assets/Speed-Staes/Tests/EditMode/UI/Race/**"
  - "Assets/Speed-Staes/Tests/PlayMode/**"
touches_scene: true
branch: agent/T-061-yaris-hud-v2
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
Kullanici (30.10) `png.rar` + bir referans ekran (1284x720, yaris basi, kosucu yok) gonderdi: "bunlari attigim gorsele bakarak duzenle tekrar koy".
Parcalar supervizor tarafindan `Assets/Speed-Staes/UI/Art/RaceHud/` altina ASCII adlarla kondu (kirpilmis, **dogal boyut = kanvas birimi**,
1080 yukseklik): `telemetry_panel` 398x71 (ortada kendi bolme cizgisi), `counter_panel` 185x67, `cash_diamond_icon` 46x35, `plus_icon` 27x26,
`stopwatch_icon` 45x54, `speedometer_icon` 61x41, `energy_icon` 31x39, `energy_track` 388x27, `energy_fill` 381x21, `cadence_track` 382x27,
`cadence_scale` 376x21 (yesil-sari-kirmizi), `cadence_pointer` 12x39, `square_button_background` 103x98, `exit_icon` 61x66, `restart_icon` 69x76.
Bugunku HUD (`UiV2Prefabs.RaceHud`) eski buyuk tuval sanatini (`UI/Art/Race/*`, tanesi 1-2k px) oranlarla yerlestiriyor ve enerji/kadans
izlerini koddan renkle ciziyor. Yeni HUD bu parcalarla, referanstaki yerlesimle kurulur.

Referans olculeri (supervizor olctu; referans px x 1.5 = kanvas birimi, kenar/merkez cipasina gore):
- **Telemetri** (sol ust): sol 52, ust 48, boyut 398x71. Icinde: kronometre sol ~15 (dikey orta), bolme cizgisi panelin ~%50'sinde (sanatta),
  hiz gostergesi panelin ~%54-%69'u. Sure metni kronometre ile bolme arasi, hiz metni hiz gostergesinin sagi (referansta bos; bugunku
  "0.00" / "0.0 m/s" degerleri ayni gorunumle).
- **Para** (telemetrinin alti): sol 52, ust 138, 185x67. Elmas sol ~15 dikey orta (46x35), deger metni elmasin sagi (kalin, ~38 pt, lacivert),
  `+` sag ucta (27x26; bugunku gibi pasif buton, dokunma alani gorunur bolge).
- **Enerji** (ust orta cipali): ikon merkezden x -235..-204, ust ~50; iz merkezden x -195..+193, ust 55, 388x27; dolgu izin icinde ortali
  (381x21), `Image.Type.Filled` yatay soldan.
- **Kadans** (enerjinin alti): "Cadence" etiketi izin solunda (sag hizali, kalin ~24 pt, koyu lacivert), iz merkezden x -190..+192, ust 121,
  382x27; olcek izin icinde ortali (376x21); ibre 12x39 izin dikey ortasinda, x'i degerle iz boyunca kayar (bugunku `RaceHudView` mantigi).
- **Cikis / Yeniden baslat** (sag ust): ikisi de 103x98 `square_button_background`, ust 37; Restart sag kenardan 50, Exit sag kenardan 186
  (araliklar referanstan). Ikonlar dogal boyutta ortada (`preserveAspect`). Dokunma alani gorunur bolge.
Hepsi dogal boyutla (`SetNativeSize` esdegeri, sanat px = kanvas birimi); eski "tuval kenar payi" duzeltmeleri kalkar. `UiArt`'a `RaceHud(name)`
yol yardimcisi. Eski `UI/Art/Race/*` dosyalarini **silme** (baska ekranlar kullaniyor olabilir): bu task sonunda hangileri artik referanssiz,
grep ile calisma notuna yaz (supervizor siler).

`RaceHudView`'in alan sozlesmesi (sure/hiz/para metni, enerji dolgusu, kadans ibresi/izi, butonlar) korunur ya da gerekirse guncellenir;
bagli testler yesil kalir. Kod kalici UI kurmaz. Prefab/sahne uretimi supervizorde (`needs:` SCENE).

## Kabul kriterleri
- [ ] `Race_play` / `Race_countdown` goruntulerinde HUD referans yerlesimine uyuyor (panel/ikon/iz boyutlari dogal, konumlar yukaridaki)
- [ ] Enerji dolgusu ve kadans ibresi deger degisince dogru hareket ediyor (var olan EditMode/PlayMode testleri yesil; kadans ibresi konumu icin
      saf hesap varsa EditMode testi)
- [ ] needs: SCENE yazili; referanssiz kalan eski Race sanati calisma notunda

## Geri bildirim
GORSEL  : var olan
SES     : T-052
DOKUNSAL: T-052
ZAMAN   : yok
KAMERA  : yok

## Calisma notu
