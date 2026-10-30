# Backlog

> Kapsam disi kalan her sey buraya. "Bu arada sunu da duzelttim" YOK -
> fark ettigin ilgisiz sorunu buraya yaz ve gec.

## Bicim
`- [ ] <konu> — <neden onemli> (fark edildigi task: T-XXX, <tarih>)`

## Teknik borc

## Icerik (GDD'den gelen, henuz siraya girmemis)

## Olu ayar supheleri
(config alani var ama okuyucusu bulunamadi - faz sonu taramasinda dogrula)

## Test kapsamasi bosluklari
- [ölü kod] T-024/T-033 sonrası kullanılmayan: `EventRowView.cs`, `UiRowPrefabs.EventRow`, `UiViewBinder.BindEventSelect`/`BindResults`, `UiScreenPrefabs.EventSelect`/`Results`, `ResultRowView`, eski `Rows/EventRow.prefab` (28.10)
- [builder] Her RebuildAll prefab içi fileID'leri yeniden üretiyor (her build binlerce satır diff) → var olan prefab'ı yerinde güncelleyen builder ya da sabit fileID; o zamana kadar sahne + prefab'lar aynı build'den commit'lenir (28.10)
- [kostüm] Renk örnek kutusu öğenin rengi yoksa lacivert; 'Watch Replay' etiketi iki satıra kırılıyor (28.10)
- [analiz] DNF (0 s / dev force-finish) serisi yine 0 s'de düz çizgi + '100 m / 0.00 s' balonu çiziyor; DNF seride çizgi ve balon olmamalı (IMPLEMENTATION.md: eksik örnek 0 sayılmaz) (30.10)
- [yaris basi] READY/SET/GO yazisi tasarimdakinden kucuk, SET gerilim cizgileri goruntude secilmiyor; olcek/alfa ince ayari (RaceStartConfig) (30.10)
