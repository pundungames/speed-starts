---
id: T-081
title: Ok tuslariyla kosu - sol ok sol ayak, sag ok sag ayak (yarista ve Practice'te; N/M ve dokunma aynen kalir)
status: review
owner: gameplay-agent
depends_on: []
owns_paths:
  - "Assets/Speed-Staes/Scripts/Bridge/ArrowKeyRunnerInput.cs"
  - "Assets/Speed-Staes/Scripts/Bridge/RaceSessionAdapter.cs"
  - "Assets/Speed-Staes/Scripts/Core/Runtime/Race/Input/**"
  - "Assets/Speed-Staes/Tests/EditMode/Core/Race/Input/**"
touches_scene: false
branch: agent/T-081-ok-tuslari
commits: []
tests:
  - "Assets/Speed-Staes/Tests/EditMode/Core/Race/Input/RunnerTapTranslatorTests.cs"
analytics:
  - "yok: projede analytics yok"
needs:
  - tip: WIRING
    durum: open
    not: "ArrowKeyRunnerInput -> RaceSceneInstaller.InstallBindings (Container.BindInterfacesTo<ArrowKeyRunnerInput>().AsSingle().WithArguments(raceSession); constructor takes the RaceSessionAdapter)"
started: 2026-11-03
finished: 2026-11-03
blocked_reason:
---

## Hedef
Kullanici 30.10'da "klavye yon tuslariyla oynama" istedi; Malik ok tuslarini N/M'ye cevirmisti (Editor'de Scene gorunumunu
kaydiriyordu) ve geri koymadi. Race sahnesinde (100m, 110mH, Practice) sol ok = sol ayak (leg 0), sag ok = sag ayak (leg 1).
Malik'in `Scripts/Runner/RunnerInput.cs` ve `Scripts/Physical/**` dosyalarina YAZILMAZ: oklar ayni kuyruga
`RunnerInput.Inject(leg, pressed, gameTime)` ile girer (zaman damgasi Input System olay zamanindan oyun zamanina tasinir, RunnerInput'un
yaptigi gibi), boylece bir ok basisi N/M basisi kadar kesindir. Kapi kosucunun kendisidir: `RunnerBody.acceptInput` kapaliyken
(GO'dan once, bitisten sonra) BASIS gonderilmez; BIRAKMA her zaman gecer (ayak basili kalmasin).
Sahne degismez: sinif sahnedeki bir bilesen degil, Zenject ile kurulan duz C# nesnesidir (`IInitializable`, `IDisposable`).
Oyuncunun govdesine `RaceSessionAdapter` uzerinden ulasilir (gerekirse ona salt okunur bir ozellik ekle; baska davranis degismez).
Mantik (kapi + zaman donusumu) Bridge'den bagimsiz, test edilebilir saf bir siniftadir (`Core/Runtime/Race/Input/`); Bridge yalniz
Input System baglantisi. Eski deneme kodu (derlenmedi) referans olarak: `pipeline-logs/arsiv/agent-T-077-080-duzeltmeler-77a3c763.patch`
icindeki `KeyboardRunnerInput.cs` (MonoBehaviour'du; bu task'ta Zenject nesnesi olacak).
Once oku: `docs/TUZAKLAR.md` (ozellikle asmdef'li test assembly'si Assembly-CSharp'a referans veremez; Bridge asmdef'siz).

## Kabul kriterleri
- [ ] GO'dan sonra sol/sag ok basisi ve birakmasi, oyuncunun RunnerInput kuyruguna dogru bacak ve zamanla girer
- [ ] GO'dan once ve bitisten sonra ok basisi kosucuyu baslatmaz; birakma her durumda gecer
- [ ] N/M ve dokunma aynen calisir; botlar oklardan etkilenmez (yalniz oyuncunun govdesi)
- [ ] Kapi + zaman donusumu EditMode testli (sabit denge sayisi yok)
- [ ] Bagama icin WIRING maddesi needs'te (RaceSceneInstaller supervizorde)

## Kapsam disi
- Malik'in RunnerInput/RunnerBody/RaceController'ina degisiklik; UI ustundeki dokunmanin adim sayilmamasi (Malik)
- Home sahnesi, ayarlar, tus atama ekrani; sahne/prefab dosyalari

## Calisma notu
- Yeni: Core/Runtime/Race/Input/RunnerTapTranslator.cs (kapi + zaman donusumu, saf), Bridge/ArrowKeyRunnerInput.cs (Zenject IInitializable+IDisposable, InputAction leftArrow/rightArrow).
- RaceSessionAdapter'a salt okunur `PlayerBody` eklendi; baska davranis degismedi. Ok olaylari `body.input.Inject(leg, pressed, gameTime)` ile girer, botlar etkilenmez.
- Namespace `SpeedStars.Core.Race` (klasor Input ama ad alani Input degil: UnityEngine.Input golgelenmesin).
- Not: RunnerBody.Update zaten acceptInput kapaliyken basislari atiyor; kapi burada ikinci kez, task geregi.
- Birakilan: installer baglamasi (WIRING open); Unity/test kosulmadi; .meta'lar supervizorde uretilecek.
