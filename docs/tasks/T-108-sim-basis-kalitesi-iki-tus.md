---
id: T-108
title: RunnerSim - basisin kalitesi govdeye gecsin, Form degeri, iki tus birlikte cift itis olmasin
status: merged
owner: gameplay-agent
depends_on: [T-107]
owns_paths:
  - "Assets/Speed-Staes/Scripts/Physical/RunnerSim.cs"
  - "Assets/Speed-Staes/Tests/PlayMode/RunnerSimRelationTests.cs"
touches_scene: false
branch: agent/ik-kosucu
commits: [4c377f94, 0374dcd7]
tests:
  - "Assets/Speed-Staes/Tests/PlayMode/RunnerSimRelationTests.cs"
analytics:
  - "yok: projede analytics yok"
needs:
  - tip: DECISION
    durum: closed
    not: "Scripts/Physical bu makinede kilitli (Malik alani). Kullanici kilidi kaldirmayi onaylamali. -> kullanici 04.11 acti"
started: 2026-11-04
finished: 2026-11-04
blocked_reason:
---

## Hedef
Slack (04.11): "doğru anda basarsan bir şey olmuyor, fazla basarsan öne gidiyor"; "iyi koşma ve kötü koşma ayrımı yok".

Kök sebep (kod): `StepHead`'te `headKick` basışın kalitesine bakmaz ("whatever the press"); doğru basış da erken basış da
başı aynı 12° öne atar. `stumbleKick` yalnız ekranda çizilmeyen `LeanSpring`'e gider. Sim'in ölçülen farkı (prefab
değerleri, 100 m): en iyi 9.8 sn / 13 m/s, ritim 0.26 10.6 sn, ritim 0.50 21 sn / 5.6 m/s, ritim 0.125 mash tıkanır; **baş
açısı bu koşularda −12..+3°, yani hepsi aynı görünür.** Ayrıca **iki tuş birlikte 0.25 sn = 8.72 sn: en iyi tek tek koşudan
1.1 sn hızlı** (ikinci basış `togetherWindow` içinde tam itiş alıyor). Race sahnesinde de doğrulandı (T-107): `both030` ort. 12.67 m/s, `perfect` 11.51 m/s.

## Yapılacak
1. Başın ilk vuruşu basışın zamanlamasından gelsin: `Press`'te zamanlama verimi (baş/enerji çarpanı HARİÇ) saklanır,
   `StepHead`'teki vuruş `headKick × Lerp(goodKickShare, wasteKickShare, 1 − verim)` olur. Doğru basış neredeyse öne
   atmaz, boşa basış sert atar ve birikir. Tutma (`holdPush`) değişmez.
2. `Form` (0..1, public): basış başına zamanlama veriminin hareketli ortalaması (`formFollow`), basış yokken sönümlenir (`formDecay`). Görsel okur; hıza etkisi yok.
3. `togetherPush` (0..1, varsayılan 0): `togetherWindow` içindeki ikinci basışın itiş payı. Karar: iki tuş birlikte = bir itiş.
4. Sim'in başlığındaki `<summary>` ve tooltip'ler güncel.

## Kabul kriterleri
- [ ] Sim ölçümü (scratchpad `simprobe`, süpervizör verir): `en iyi`, `ritim 0.24..0.30`, `0.50` 100 m süresi eskiye göre ±%2 (bot kalibrasyonu bozulmaz).
- [ ] İki tuş birlikte hiçbir ritimde en iyi tek tek koşudan hızlı değil.
- [ ] Baş ortalaması: mash > erken (0.20) > ritim; ritimde baş tepesi eskisinden küçük.
- [ ] `RunnerSimRelationTests`: yukarıdakiler ilişki olarak (sabit sayı yok), `RunnerSim` reflection ile (test asmdef Assembly-CSharp'ı görmez).
- [ ] Yeni alanların hepsinde `Tooltip`. Kaldırılmış adlar yeniden kullanılmaz: `stumbleDrag, speedLean, stepKick, trunkFollow, holdLean, oneFootMax, earlyKick, pressReturn, headFallSpring, headReturn, holdHead, stanceKeepTime, holdBlendTime`.
- [ ] Yeni alanlar prefab'ta yok: kod varsayılanı geçerli. Prefab'a dokunulmaz.

## Kapsam dışı
- `RunnerPoseDriver`, kamera, `pushGain`/`drag` gibi var olan hız sayıları.

## Çalışma notu
- RunnerSim: Press'te zamanlama verimi (legReady + sameLeg dahil, enerji/baş hariç) `LegSim.Kick`'e, vuruş `headKick x Lerp(goodKickShare 0.2, wasteKickShare 1.6, 1-verim)`; `Form` (formFollow 0.3, formDecay 1.5, formHoldTime 0.5); `togetherPush` 0.
- Çift (togetherWindow + launchPending) ikinci tuşta itiş, nod, lean vuruşu ve baş vuruşu `togetherPush` ile çarpılır; LastResult/LastEfficiency birinci tuşunkinde kalır, Form güncellenmez.
- simprobe: en iyi 9.78 (9.81), ritim 0.24..0.30 ve 0.50 +-%0.3 içinde; iki tuş 0.25 8.72 -> 10.44; baş ort mash 77 > erken 7 > ritim -12; ritim baş tepesi 13 -> 3.
- Doğrulanamadı: Unity derlemesi ve PlayMode testi koşulmadı (test dosyasının .meta'sı yok, Unity üretir). Tutma itişi (holdThrust) çift tuşta hâlâ iki ayak sayıyor, kasıtlı değişmedi.
- Botlar: BotPressSchedule bacakları sırayla basar, her basış önceki bırakıştan sonra; yalnız rush/stumble basışları togetherWindow (0.08 sn) içine düşebilir, o durumda ikinci basış artık itmez.
- Takip: wasteKickShare 1.6 -> 3.5 (Range 0..5), mash hafif tap 0.14 hâlâ ödüyordu; test LightFastMashIsSlowerThanTheRhythm eklendi.
- Süpervizör: wasteKickShare 3.5 seçildi (sim taraması). Oyun sahnesinde mash 5.7→1.3 m/s, iki tuş 12.7→9.4, en iyi koşu 11.5→11.7. T-113 birikimli modele geçince 3.5 düşecek.
