---
id: T-063
title: READY/SET/GO cilasi - kelimeler buyuk ve ekranin ortasinda, efekt cizgileri kelimeyle olceklenir, GO sonerken lacivert lekeye donmez
status: todo
owner: ui-agent
depends_on: [T-059]
owns_paths:
  - "Assets/Speed-Staes/Scripts/UI/Race/RaceStart*.cs"
  - "Assets/Speed-Staes/Scripts/UI/Framework/ThickOutline*.cs"
  - "Assets/Speed-Staes/Scripts/Core/Runtime/Race/RaceStartConfig.cs"
  - "Assets/Speed-Staes/Tools/Editor/Builders/Ui/UiRaceStartPrefabs.cs"
  - "Assets/Speed-Staes/Tools/Editor/Builders/Scene/RaceStartConfigSeed.cs"
  - "Assets/Speed-Staes/Tests/EditMode/UI/Race/**"
touches_scene: true
branch: agent/T-063-baslangic-cila
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
T-059 sonrasi 6 kare (`D:\Pundun\speed-starts\pipeline-logs\shots-int12b-2026-10-30_142607\Race_start_01_impact.png` .. `_06_clear.png`,
ozet `C:\Users\Monster\AppData\Local\Temp\claude\d--Pundun-speed-starts\010558a9-f012-4284-b1f2-1285cf76ea87\scratchpad\start6.jpg`)
storyboard'a (`...\scratchpad\baslangic-ui\Yaris_Baslangic_UI_v1\storyboard.jpg`) gore dort farkla kaldi:
1) **Boyut**: READY ekranin ~%15'i genisliginde (2400 tuvalde ~380 birim). Hedef: READY ~900-1000 birim genislik (ekranin ~%40'i); SET ve
   GO ayni puntoda (storyboard'da uc kelime ayni font boyunda, SET kisa oldugu icin dar). Kontur orani (%6.5) ayni kalir.
2) **Konum**: kelimeler HUD'un (enerji/kadans) hemen altinda, ustte sikisik. Hedef: yatayda ekran ortasi, dikeyde ustten ~%38 (kosucunun
   bas hizasi; HUD'a degmez). Config'te.
3) **Efektler**: impact cizgileri, SET gerilim cizgileri ve GO ok/isinlari kelimeye gore cok kucuk. Kelimeyle birlikte olceklenir (ayni grup
   olcegi), konumlari storyboard'daki oranlarla (layout.json content_bounds 1024x576 tuvale gore).
4) **GO sonerken lacivert leke** (`_06_clear`): ThickOutline'in 16 kontur kopyasi saydamlasirken alfalari ust uste biniyor, lacivert dolgunun
   onune geciyor. Storyboard 06: GO buyuk ve **kirmizi** kalarak dagilir. Duzeltme: soner/kaybolma sirasinda kontur kopyalarinin alfasi dolgudan
   hizli dussun (ornek kontur alfa = dolgu alfa^n, n config'te) ya da kontur kalinligi azalsin; hangisi daha temizse. ThickOutline'a testli
   saf hesap.
Zamanlama sayilarina (timing.json) dokunma. `Shots_RaceStartPhases` var (T-059); ayni 6 kareyle kontrol edilir. Kod kalici UI kurmaz;
DOTween yalniz cekirdek. Prefab/sahne/asset uretimi supervizorde (`needs:`).

## Kabul kriterleri
- [ ] 6 karede: READY ekranin ~%40'i genisliginde, ortada, HUD'a degmiyor; efektler kelimeye orantili; 06'da GO kirmizi, lacivert leke yok
- [ ] ThickOutline soner-alfa hesabi EditMode testli; var olan testler yesil
- [ ] needs: SCENE (+ gerekiyorsa ASSET: RaceStartConfig yeni alanlar) yazili

## Geri bildirim
GORSEL  : bu task
SES     : T-052 korunur
DOKUNSAL: yok
ZAMAN   : timing.json
KAMERA  : yok

## Calisma notu
