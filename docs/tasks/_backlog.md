# Backlog

> Kapsam disi kalan her sey buraya. "Bu arada sunu da duzelttim" YOK -
> fark ettigin ilgisiz sorunu buraya yaz ve gec.

## Bicim
`- [ ] <konu> — <neden onemli> (fark edildigi task: T-XXX, <tarih>)`

## Teknik borc
- [ ] [build] Magaza build metodu: IL2CPP + ARMv7/ARM64 + paket adi + surum kodu yalniz o build icin (PlayerSettings sonra geri), AAB; gunluk build Mono kalir (IL2CPP 9.5 dk, Mono 1 dk). IL2CPP+link.xml kopyada derlendi (71 MB APK); cihazda calisma denenmedi (30.10)
- [ ] [doku] Race/Leaderboard ikonlari tam tuval boyutlu (ASTC ile bile tanesi 1.5 MB): alfa sinirina kirpma ya da Sprite Atlas; SceneHomeDim hala Costume/dimscreen.png sprite kullaniyor -> duz renk (T-057, 30.10)
- [ ] [doku] Serhat temalari 51 MB mesh (APK toplam kullanici asset 140 MB): tema basina yukleme ya da Addressables - Serhat ile (T-057, 30.10)

## Icerik (GDD'den gelen, henuz siraya girmemis)

## Olu ayar supheleri
(config alani var ama okuyucusu bulunamadi - faz sonu taramasinda dogrula)

## Test kapsamasi bosluklari
- [builder] Her RebuildAll prefab içi fileID'leri yeniden üretiyor (her build binlerce satır diff) → var olan prefab'ı yerinde güncelleyen builder ya da sabit fileID; o zamana kadar sahne + prefab'lar aynı build'den commit'lenir (28.10)
- [emote] emote fiyatlari yer tutucu (Nakit); gercek fiyatlar karara bagli (30.10)
