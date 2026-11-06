# Backlog

> Kapsam disi kalan her sey buraya. "Bu arada sunu da duzelttim" YOK -
> fark ettigin ilgisiz sorunu buraya yaz ve gec.

## Bicim
`- [ ] <konu> — <neden onemli> (fark edildigi task: T-XXX, <tarih>)`

## Teknik borc
- [ ] [kostum] Yeni Body (ten) sekmesinin ikonu (stats_height_icon) sekmede neredeyse gorunmuyor; daha belirgin bir ikon ya da olcek (T-066, 30.10)
- [ ] [build] Magaza build metodu: IL2CPP + ARMv7/ARM64 + paket adi + surum kodu yalniz o build icin (PlayerSettings sonra geri), AAB; gunluk build Mono kalir (IL2CPP 9.5 dk, Mono 1 dk). IL2CPP+link.xml kopyada derlendi (71 MB APK); cihazda calisma denenmedi (30.10)
- [ ] [doku] Leaderboard ikonlari tam tuval boyutlu: alfa sinirina kirpma ya da Sprite Atlas (Race HUD T-061 ile kirpilmis sanata gecti); SceneHomeDim hala Costume/dimscreen.png sprite kullaniyor -> duz renk (T-057, 30.10)
- [ ] [builder] Malik'in 03.11 elle ayarlari builder'da yok, bir sonraki yeniden uretimde silinir: EventModal ve SevenDayCheckInPanel Close butonu pencerenin sag kenarina sabit (Home sahnesinde override; UiLeaderboardPrefabs + 7 gun builder'ina), ResultsScreen.prefab `replaySkin` = UI/Widgets/ReplayControlsSkin.asset (UiResultsPrefabs'a) (merge e7d269f2, 03.11)
- [ ] [race of the day] RaceOfTheDayService.TryBuildField alan id'sini elle kuruyor (eventId + ':' + DayKey); Leaderboard'daki DailyBotField.FieldId ayni bicimi uretiyor -> servis onu kullansin, tekrar biter (test 'liste birincisi == Daily WR' kaymayi yakalar) (T-093, 03.11)
- [ ] [test] FrameRateToleranceTests 'sik basma' takvimi kosucuyu 6. sn'de durduruyor (2.89 m, hiz 0): gercek sik basmayi (Malik olcumu: saniyede 10 -> 3. sn ~2.7 m/s) temsil eden takvime cevir (T-096, 03.11)
- [ ] [doku] Serhat temalari 51 MB mesh (APK toplam kullanici asset 140 MB): tema basina yukleme ya da Addressables - Serhat ile (T-057, 30.10)

## Icerik (GDD'den gelen, henuz siraya girmemis)

## Olu ayar supheleri
(config alani var ama okuyucusu bulunamadi - faz sonu taramasinda dogrula)

## Test kapsamasi bosluklari
- [builder] Her RebuildAll prefab içi fileID'leri yeniden üretiyor (her build binlerce satır diff) → var olan prefab'ı yerinde güncelleyen builder ya da sabit fileID; o zamana kadar sahne + prefab'lar aynı build'den commit'lenir (28.10)
- [emote] emote fiyatlari yer tutucu (Nakit); gercek fiyatlar karara bagli (30.10)
- [ ] [gorsel] Results `GemReward/Label` ("+3 Purple Gems"): zeminsiz, kontur 0.75, Nakit karti gibi okunmuyor olabilir; `PersonalBest` zemini + kontursuz yazi + `purple_gem_icon` (T-106 kalibi; MCP ile 1 dk) (04.11)
- [ ] [input] Butun Selectable'lar Navigation=Automatic: basilan dugme secili kalir, ok/Enter tuslari UI'da gezer; dokunmatik oyunda Navigation=None dusunulebilir (04.11)
- [ ] [test] EditMode kırmızı (04.11, IK dışı): `BuilderGuardTests.NoBuilderWritesAPrefabOrSceneOutsideTheGuard`, `HomeCardBuilderTests` x3 (`HomeScreen has no CharacterCard/...`): HomeScreen elle değişmiş olabilir, prefab mı test mi yanlış bak
- [ ] [test] `ReferenceUiReviewTests` x2 batchmode'da takılıyor (Serhat, eac6b9f6): `WaitForEndOfFrame`; `yield return null` + elle render ya da batchmode'da atla
- [ ] [mekanik] Tuş süresi uçurumu: ritim 0.28'de tuş 0.13 sn = 11.1 sn, 0.17 sn = 100 m bitmiyor; klavyede görünür, telefonda dene
- [ ] [belge] IK işi bitince `PlayerController.md`'yi yeni sisteme göre güncelle (bölüm 4, 8); stale prefab alanları (`stepSlow`, `recoverTime`, `midTime`, `homeTime`) prefab yeniden kaydedilince düşer
- [ ] [mekanik] Çapraz kamera yalnız şehir temasında görüldü: Egypt, Rome, Pirate Sea, Sky Islands'ta arka plan boşluğuna bak
- Yarış testleri atlanıyor (06.11, `9d9f2b1e` "100m event requires an Entry Ticket" sonrası): `Race100m_ReplayPlayedToItsEnd...`, `Race100m_RunnerStaysPastTheLine...`, `Race100m_WatchReplay...`, `Results_NavButtons...` "The event catalog has no free playable event with opponents." ile çalışmadan geçiyor. Bilet verip koşturmalı.
- Bot "aynı ayak iki kere" hatası (`BotPressSchedule`) artık kısa tek ayak sekişi başlatır (T-157 niyet kuralı); yarışta göze batarsa kapat.
