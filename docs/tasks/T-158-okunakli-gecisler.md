---
id: T-158
title: Okunakli gecisler: basista govde geri, spam %50, ayak degince calisan basis, ayakta tam sekis, titreme
status: review
owner: gameplay-agent
depends_on: [T-157]
owns_paths:
  - "Assets/Speed-Staes/Scripts/Physical/RunnerPoseDriver.cs"
  - "Assets/Speed-Staes/Tests/PlayMode/RunnerFormProbeTests.cs"
  - "docs/tasks/T-158-okunakli-gecisler.md"
touches_scene: false
branch: agent/ik-kosucu
commits:
  - 121ba5ee
  - 6b2efbff
  - d36bee54
  - cce0ca29
  - 9158fe8c
  - 5dcf6215
  - 53acfe65
  - 84b2be0f
  - 14c207b7
  - 0ae539ad
  - 3d2d12f2
tests:
  - "gerekmiyor: gorsel; kullanici gozle bakar"
analytics:
  - "yok: projede analytics yok"
needs: []
started: 2026-11-06
finished: 2026-11-06
blocked_reason:
---

## Hedef
Kullanici 06.11 (T-157 sonrasi): "her harekette inputta ust govde kendini biraz geri itsin. spam atarsamda hareketi %50 ye kadar sart kos sonrasinda hareketi tekrarlattir eger spam atiyorsam. tek ayakta birinden baska ayaga gecerken bastigim ayak ilk yere degdiginde calistir o inputu. animasyonlarda karakterin orasi burasi titremeleri hala var. duruken ilk adimi atarken karakter normal adim atiyor arada. tam olarak tek bacak sekisi yapmali. sonrasinda diger ayaga tiklayinca mukemmel bir kosmaya gecis yapmali. her geciste ne olursa olsun animasyonlar kusursuz ve okunakli gecis yapmali, bir anda cok hizli bir gecis istemiyorum."

## Istenen
**A. Basista govde geri.** Pozun aldigi her basis (kosu, sekis, cift): ust govde (bel, gogus, boyun; kalca ve bacak degil) kisa, yumusak geri itilir. Yeni alanlar: `pressBackKick` (~5 derece tepe), yay (kritik altina yakin sonumlu, ~4-5 Hz; tepeye ~0.1 sn, donus ~0.3 sn). Yayin hizina darbe, poz snap yok. Spam'de birikmez (tavan ~8). `UpdateBounceLean`'in `baseBend`/`bend` yolu uygun. T-154'un sabit govdesi bunun disinda ayni.

**B. Spam: %50 sarti.** Hareket yarisina gelmeden basis alinmaz; yarisini gectiyse basis alinir ve hareket tekrarlanir. Tek kapi kurali:
- Sekis: `SwingShare >= hopSpamShare` (0.5) ya da ayak yerde. Alinan basis mevcut sekisi kesmez, kalanini kisaltir: ayak en az `hopRushTime` (~0.14 sn) sonra iner (salinimi yeniden zamanla, `Catch` degil, ayni Hermite egrisi; kalca yayi ve `hipMaxAccel` yumusak tutar), `hopStanceMin` sonra yeni sekis.
- Kosu: `pressGateShare` ve `pressGateSameShare` 0.5. `hopTime` kapisi kalkar (yerini %50 alir), sekis suresi tabani (`hopSwingMin`) yalniz tek basista gecerli.
- %50'den once gelen basis: `hopBufferTime` yerine, %50'ye ~0.12 sn kala geldiyse saklanir, %50'de alinir; daha erkeni yok sayilir.

**C. Bacak degistirme ve sekisten kosuya: basilan ayak yere degince calisir.** Sekisteyken diger tusa basis (%50 sarti gecerli ya da eski bacak yerde):
- Basilan ayak (ondeki serbest bacak) yere iner, en az `hopSwapTime` (~0.18 sn), okunur sekilde.
- Basis **o ayak ilk degdigi karede** calisir: yeni stance, `pressedAt`, kol rolleri, serbest bacagin degisimi, kosu ise oteki bacagin salinimi hep bu anda. Degene kadar eski bacak eski rolunde (`_poseLastLeg`, `idle` eski).
- Eski bacak yerdeyse yeni ayak degince kalkar; havadaysa salinimina devam edip serbest bacaga acilir. Iki ayak birlikte yerde uzun kalmaz.

**D. Dururken ilk basis tam sekis (bloklar haric).** T-148 adimi (`StartSwing(stepper, ..., true)`), `hopStandWait`, "tus kalkti mi" sarti bu yolda kalkar: rastgele adim atmasinin sebebi tusun 0.15 sn'den uzun basili kalmasi.
- Basista `_hopTarget` 1, diger bacak serbest bacaga okunur sure icinde acilir (~0.22 sn), basilan ayak iter, ~0.18 sn'de kalkar, tam sekis, inis, sonra stop.
- Tus basili kalirsa T-129 tutma yolu (`holdAfter`) aynen; cift tus penceresi aynen (`holdStand`, `holdBothStand`, `spamPair` bozulmasin).
- Diger tusa basilinca C yolu: mukemmel kosu baslangici. `start`, `walkStart`, `restart`, `brakeRestart`.
- Bloklar (`startStance`) T-153 yolu aynen.

**E. Okunakli gecis hizlari.** Gecislerde hic bir sey bir anda olmaz: `IdleRate` 12 -> ~5, `hopStandSpring` 10 -> ~6, sekis baglaminda `MinCatch` en az ~0.12, `stockReturnTime` sekisten cikarken ~0.24. Hedef: `hopSwapSlow`, `hopSwap`, `hopToRun`, `runToHop`, `oneTap`, `start`: uyluk <= 14 derece/kare, diz <= 20, kol <= 14 (oturmus sekis ici ayni kalir).

**F. Titreme.**
- Bulunan: basissiz kalkista `Enter(Rest)` karesinde ayak ziplar: `slowRun` 3.567-3.63: y 0.132 0.160 0.152 0.143 0.138, ankleZ -0.187 -0.234 -0.221 -0.205 (yavas ritimde her adim). Ayak bulundugu yerden surekli gitsin (pin birakma, `pinOffset`).
- Olcum icin (`RunnerFormProbeTests`): CSV'ye `hipZ` (kalcanin koke gore ileri-geri) ve `head_pitch` ekle. Duzenege `RunnerDtJitter` statigi: true iken `Time.captureDeltaTime` her kare 1/60 x [0.7..1.3] tohumlu rastgele. Kullanici degisken karede oynuyor, bizim kayit sabit 1/60.

## Kurallar
- Kapi yalniz pozda; sim her basisi sayar, `RunnerSim.cs`'e dokunma. Bir basis bir hareket (T-157): basissiz salinim yok, niyet ham tustan, stop direkt.
- Input hic bir uzvu snap etmez. Yeni alanda Tooltip, okunmayan alan ve sabiti sil (grep `Assets`). Prefab, sahne yok.
- Yorumlar Ingilizce, "neden"i anlatan. Unity calistirma, derleyici yok; C# 9, imza hatasi yapma. Olcum supervizorde.
- Commit: `git commit -F <mesaj-dosyasi> -- <yollar>`, mesaj dosyasi scratch'te (`C:\Users\Monster\AppData\Local\Temp\claude\d--Pundun-speed-starts\1c7d0dba-289b-4948-ba09-42f88d086602\scratchpad\`), adinda "main" yok, son satir `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`. Git'i Bash ile cagir. `git add -A`, push, reset yok. Calisma yeri `D:\Pundun\speed-starts-ik`, dal `agent/ik-kosucu`.

## Ek (kullanici, ayni is)
**G.** Gecilen bacak adim gibi basar: temas kosu temasi, `swapBehind` 0.25 m arkasinda ya da `swapStanceMax` 0.3 sn sonra toe-off, sonra sekis (`leg.swapStance`).
**H.** Kisa adimda diz yukari/topuk karna: `tuckPeakFast` (95, 58), `tuckFastPeriod` (0.34 -> 0.22), `leg.cycle`'dan agirlik; tepe daha erken ve `tuckLead` daha uzun.

## Calisma notu
- Yapildi: D (`_standLeg` 0.07 sn pair beklemesi, `hopStandWait` ve adim kalkti, bloklar ayni), C+G (`DeferSwap`/`CompleteSwap`: rol ayak degince, `hopSwapTime` yakalama, tam stance), B (`HopBlocked`, `RushSwing`, kapilar 0.5, tampon 0.12), A (`_pressBack` yayi), E (IdleRate 5, hopStandSpring 6, HopMinCatch 0.12, StockTime 0.24), H, F olcum sutunu `hipZ` ve `RunnerDtJitter` (head_pitch zaten `headPitch` olarak vardi).
- Yeni alanlar: pressBackKick/Max/Spring, hopSwapTime, swapBehind, swapStanceMax, hopSpamShare, hopRushTime, tuckPeakFast, tuckFastPeriod (9); kaldirilan: hopStandWait (1). hopBufferTime 0.25 -> 0.12 ve anlami degisti.
- YAPILMADI: F'nin ayak zipalamasi (basissiz kalkista `Enter(Rest)` karesinde y/ankleZ sicramasi): kor tahminle pin/Enter koduna dokunmadim, olcumle bakilmali.
- Supheli: DeferSwap yolu (`_swapLeg`) ve eski bacagin `FreeOtherLeg` zamani; `hopSwingMin` hala tek basista degil spamde de gecerli; `RushSwing` hiz sicramasi; bas basis geri itme isareti (negatif = geri) varsayim.
- Derleyici yok, hicbir sey olculmedi.
- Olcum t158-a sonrasi duzeltmeler: A darbesi hiz biriminde (tepe=kick), H agirligi leg.cycle*0.5, bacak degistirmede tavan (hopSwapTime+0.1), eski ayak adim salinimiyla kalkar ve inince serbest bacaga acilir (FreeOtherLeg + Land), FreeFootRise riseW ile 0.12 sn, holdStand egimi hopBodyW yayindan. Yapilmadi: PinFeet birakma Z sicramasi; spamToRun kilidinin sebebi kodda net bulunamadi (tahmin: _jumping TickLegs durdurur).
