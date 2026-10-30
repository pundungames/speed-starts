---
id: T-059
title: READY / SET / GO tasarima uysun - kalin italik font (Kanit Black Italic, OFL), kalin lacivert kontur, efekt cizgileri gorunur; 6 karenin ekran goruntusu
status: todo
owner: ui-agent
depends_on: [T-039, T-049]
owns_paths:
  - "Assets/Speed-Staes/Scripts/UI/Race/RaceStart*.cs"
  - "Assets/Speed-Staes/Scripts/UI/Framework/ThickOutline*.cs"
  - "Assets/Speed-Staes/Scripts/Core/Runtime/Race/RaceStartConfig.cs"
  - "Assets/Speed-Staes/Tools/Editor/Builders/Ui/UiRaceStartPrefabs.cs"
  - "Assets/Speed-Staes/Tools/Editor/Builders/Scene/RaceStartConfigSeed.cs"
  - "Assets/Speed-Staes/Tests/EditMode/UI/Race/**"
  - "Assets/Speed-Staes/Tests/PlayMode/**"
touches_scene: true
branch: agent/T-059-baslangic-yazilari
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
Kullanici (30.10): "sadece GO var, READY falan yoktu". Kopyada akis calisiyor (`pipeline-logs/shots-int9-2026-10-30_132302/Race_countdown.png`'de
SET var) ama gorunum tasarimdan cok uzak: yerlesik Arial-benzeri ince yazi, ince kontur; krem READY/SET acik gokyuzunun onunde kayboluyor,
yalniz kirmizi GO goze carpiyor. Tasarim: `docs`'ta degil, supervizor scratchpad'i
`C:\Users\Monster\AppData\Local\Temp\claude\d--Pundun-speed-starts\010558a9-f012-4284-b1f2-1285cf76ea87\scratchpad\baslangic-ui\Yaris_Baslangic_UI_v1\`
(storyboard.jpg = 6 kare: 01 IMPACT, 02 REBOUND, 03 COMPRESS, 04 HOLD, 05 RELEASE, 06 CLEAR; README.md, IMPLEMENTATION.md, timing.json,
layout.json, build.py). Ozet: READY/SET krem #FFFCEF dolgu, GO! kirmizi #FF202D, hepsi lacivert #061A41 **kalin** kontur (onizleme: 110 px
yaziya 7 px), ayni font, ayni merkez, kalin italik.

1) **Font**: `Assets/Speed-Staes/UI/Fonts/Kanit/Kanit-BlackItalic.ttf` (SIL OFL 1.1, lisans `OFL.txt` yaninda; supervizor ekledi, kullanim
   serbest). Uc kelime bu fontla (legacy `Text`, dinamik font; kelimeler yalniz Latin). Builder fontu yoldan yukler; font yoksa yerlesik
   fonta duser ve uyari yazar.
2) **Kalin kontur**: legacy `Outline` 4 kopya oldugu icin kalin degerde kosegen/blok gorunur. `UI/Framework/ThickOutline.cs`
   (`BaseMeshEffect`): kontur kalinligi (px) ve yon sayisi (ornek 12-16) ile glifleri cember uzerinde kopyalar, dolguyu en uste koyar.
   Kalinlik = yazi boyunun ~%6.5'i (config'te oran). Var olan `Outline`/`Shadow` kaldirilir.
3) **Olcek/konum/egim**: storyboard'daki gibi kelime ekranin ust-ortasinda buyuk; READY hafif yukari egik (storyboard'dan olc, config'e).
   Efekt PNG'leri (impact/tension/release) timing.json alfalariyla gorunur olsun; `holdTensionMinAlpha` vb. config'te.
4) **Ilk kare**: Race sahnesi yuklenince READY ilk gorunen karede ekranda olmali (gorunum gec baglanip Ready olayini kacirirsa mevcut faza
   yetisir — kodu dogrula, gerekiyorsa duzelt; `RaceSessionAdapter` Bridge'de, bizim degil: ona dokunma, gorunumde cozulur).
5) **Ekran goruntuleri**: `ScreenshotTests`'e `Shots_RaceStartPhases` (yalniz `-shotDir`): `Race_start_01_impact`, `_02_rebound`,
   `_03_compress`, `_04_hold`, `_05_release`, `_06_clear` — faz olaylarini (`IRaceStartSignal` / var olan sinyal) bekleyip config
   surelerinden hesaplanan ofsetlerde cek; var olan `Race_countdown` kalir.
Kod kalici UI kurmaz; DOTween yalniz cekirdek (TUZAKLAR). Prefab/sahne/asset uretimi supervizorde (`needs:`).

## Kabul kriterleri
- [ ] 6 karenin goruntusu storyboard'a benzer: kalin italik, kalin lacivert kontur, READY ve SET acik gokyuzunde net okunur
- [ ] ThickOutline icin EditMode testi (kopya sayisi = yon x glif dortgeni + dolgu; kalinlik 0 = degisiklik yok)
- [ ] Var olan RaceStart testleri yesil; needs: SCENE/ASSET yazili

## Geri bildirim
GORSEL  : bu task
SES     : T-052 (Ready/Set/Go sesleri) korunur
DOKUNSAL: yok
ZAMAN   : timing.json
KAMERA  : yok

## Calisma notu
