---
id: T-012
title: Bridge - RaceSessionAdapter (IRaceSession, kosucu sistemini salt okur) + Dev bitis tetikleyicisi
status: merged
owner: gameplay-agent
depends_on: [T-002]
owns_paths:
  - "Assets/Speed-Staes/Scripts/Bridge/**"
touches_scene: false
branch: agent/faz2-dilim
commits: []
tests:
  - "gerekmiyor: Bridge asmdef'siz ve Malik'in MonoBehaviour'larina bagli; davranis Race sahnesinde gozle (Play) - kabul asagida"
analytics:
  - "yok: projede analytics yok"
needs:
  - tip: WIRING
    durum: closed
    not: "RaceSessionAdapter -> IRaceSession (RaceSceneInstaller, sahnedeki RaceController/RunnerBody Inspector referansi), DevRaceFinish, T-014 -> T-014a/b builder + Installers (agent/faz2-dilim), Zenject validate 2 sahne OK"
  - tip: SCENE
    durum: closed
    not: "Race.unity'de adapter ve Dev bilesenleri + alan baglari (builder, T-014) -> T-014a/b builder + Installers (agent/faz2-dilim), Zenject validate 2 sahne OK"
started: 2026-10-28
finished: 2026-10-28
blocked_reason:
---

## Hedef
`SpeedStars.Core.Race.IRaceSession`'ı Malik'in koşucu sistemi üstüne uygulayan ince adaptör (`docs/TEMEL-SOZLESME.md` §4 seam tablosu, §10).
**Malik'in koduna yazılmaz** (hook kilitli: `Scripts/Physical/**`, `RunnerInput.cs`); public alan/olay/özellikleri **okunur**: `RaceController`
(`Current`, `RaceTime`, `LastRaceTime`, `raceDistance`, `ResetRace()`), `RunnerBody` (`Tapped`, `RunReset`, `acceptInput`, `Sim.Distance` …).
Asmdef'siz (Assembly-CSharp), namespace `SpeedStars.Bridge`. Karar yok, yalnız çeviri.

## Kabul kriterleri
- [ ] `RaceSessionAdapter : MonoBehaviour, IRaceSession`: `[SerializeField] RaceController race; RunnerBody body;` (Find yok).
      `RequestStart(req)`: `raceDistance = req.DistanceMeters` (0 ise sahnedekini bırak), `ResetRace()`, girdi açık, durum Ready.
      Ready→Running geçişini (RaceController.Current) izler → `Started` bir kez. `RaceTime`/`Distance`/`TargetDistance` okunur.
      `Finished(RaceOutcome)` yarış başına **tam bir kez**: bugün Malik'in kodunda bitiş olayı yok ve 100 m'de aynı karede başa dönüyor
      (RaceController.cs:55-58) → `RunReset` anında `LastRaceTime` değiştiyse o değerle Finished (sıra 1, koşucu 1) + `acceptInput = false`
      (Results açıkken koşucu kıpırdamasın; `RunReset`'in `acceptInput=true`'sundan SONRA, RunnerBody.cs:69-70).
- [ ] `Dev/DevRaceFinish` (`#if UNITY_EDITOR || DEVELOPMENT_BUILD`): editörde bir tuş (ör. F) ile o anki süreyle Finished tetikler (100 m koşmadan
      sonuç ekranını görmek için).
- [ ] Malik'in dosyalarında değişiklik yok (`git diff --name-only`); onun API'sinde olmayan bir şeye ihtiyaç varsa YAZMA, `needs:`'e DECISION.
- [ ] Çalışma notunda: Race.unity'de hangi bileşen hangi nesneye, hangi alan neye bağlanacak (T-014 builder'ı kullanacak).

## Çalışma kuralları
- Çalışma klasörü MUTLAK: `D:\Pundun\speed-starts-faz2` (worktree, dal `agent/faz2-dilim`). Ana klasöre yazma.
- Önce oku: `docs/CALISMA-KURALLARI.md`, `docs/TEMEL-SOZLESME.md` §4, §10, §11, `docs/TUZAKLAR.md`, `Scripts/Core/Runtime/Race/**`,
  Malik'in `Scripts/Physical/RaceController.cs`, `RunnerBody.cs`, `RunnerSim.cs` (salt okunur), `CLAUDE.md` (bilgi).
- Unity/batchmode/verify YASAK, `git commit` YASAK, `.meta` yazma.
- Bitince `status: review`, `## Çalışma notu` 5 satır + bağlama tablosu.

## Çalışma notu
1. `Bridge/RaceSessionAdapter.cs` (IRaceSession) ve `Bridge/Dev/DevRaceFinish.cs` (F tusu, `#if UNITY_EDITOR || DEVELOPMENT_BUILD`) yazildi; Malik'in dosyasina dokunulmadi.
2. Finished: `body.RunReset`'te `LastRaceTime` degistiyse (sira 1, kosucu 1, mesafe = raceDistance) + `acceptInput = false` (RunReset olayi acceptInput=true'dan sonra gelir). Kendi RequestStart'imizin reset'i `_requesting` bayragiyla yok sayilir.
3. Started: Update'te `race.Current` Ready->Running yoklamasi; ayni karede bitis olursa Started once yayilir.
4. Risk: bitis `RunReset`'ten cikarimdir; Malik RunReset/ResetRace davranisini degistirirse bozulur. Finished sonrasi sim sifirlandigi icin Distance/RaceTime donmus outcome'dan okunur. Ayni sure iki yaris ust uste birebir ayni float olursa (pratikte yok) bitis kacar.
5. Test yazilmadi (kabul: Race sahnesinde Play, gozle). Dev bilesen release'te derlenmez; adapter'daki `DevForceFinish` de.

### Race.unity baglama tablosu (T-014 builder)
| Nesne | Bilesen | Alan -> hedef |
|---|---|---|
| Adapter nesnesi (ornegin `RaceSession`) | `SpeedStars.Bridge.RaceSessionAdapter` | `race` -> sahnedeki `RaceController`; `body` -> `HumanCharacterDummy_F` uzerindeki `RunnerBody` (bos birakilirsa `race.body`) |
| Ayni nesne (yalniz Dev) | `SpeedStars.Bridge.DevRaceFinish` | `adapter` -> yukaridaki adapter; `finishKey` = F |
| RaceSceneInstaller (WIRING) | - | `RaceSessionAdapter` -> `IRaceSession` (`FromComponentInHierarchy` / sahne referansi) |
6. Duzeltme: DevRaceFinish sinifi+alanlari her zaman derlenir (release sahnede missing script olmaz); yalniz Update/DevForceFinish cagrisi #if icinde, release te bos MonoBehaviour.
