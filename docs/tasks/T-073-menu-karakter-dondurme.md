---
id: T-073
title: Ana menude karakteri parmakla surukleyerek kendi ekseninde dondurme (birakinca yavaslayarak durur, oldugu yerde kalir)
status: todo
owner: gameplay-agent
depends_on: []
owns_paths:
  - "Assets/Speed-Staes/Scripts/Bridge/MenuCharacterSpin.cs"
  - "Assets/Speed-Staes/Scripts/Core/Runtime/Menu/SpinMath.cs"
  - "Assets/Speed-Staes/Tools/Editor/Builders/Scene/SceneHomeCharacter.cs"
  - "Assets/Speed-Staes/Tests/EditMode/Core/Menu/SpinMathTests.cs"
touches_scene: true
branch: agent/T-072-073
commits: []
tests:
  - "Assets/Speed-Staes/Tests/EditMode/Core/Menu/SpinMathTests.cs"
analytics:
  - "yok: projede analytics yok"
needs: []
started:
finished:
blocked_reason:
---

## Hedef
Kullanici (30.10): "ana menude karakteri tutup rotate edebiliyoruz kendi ekseninde, bizimde olsun" (referans oyunda karakter yatay
surukleyince kendi dikey ekseni etrafinda donuyor; birakinca oldugu acida kaliyor). Home sahnesindeki menu karakteri
(`Bridge/MenuCharacter`, builder `SceneHomeCharacter`) bugun sabit duruyor.

1. **Saf matematik** (`SpeedStars.Core` tarafinda, asmdef'li, test edilebilir; TUZAKLAR: test asmdef'i Assembly-CSharp'i goremez):
   surukleme px -> derece (ekran genisligine oranla: tam ekran genisligi = N derece, N parametre), birakista hiz -> atalet, ataletin
   sonumle sifira inmesi (kare hizindan bagimsiz: `exp(-k*dt)`), kucuk hizda tam durma.
2. **`SpeedStars.Bridge.MenuCharacterSpin`** (MonoBehaviour, Tooltip'li `[SerializeField] private` alanlar: derece/ekran, sonum, durma
   esigi): Input System (dokunma + fare). Surukleme UI uzerinde BASLARSA yok sayilir (`EventSystem` raycast; buton/popup'a basmak
   karakteri dondurmesin); UI disinda herhangi bir yerden baslayan yatay surukleme karakteri kendi dikey ekseninde dondurur. Karakter
   gizliyken (tema secimi `IsBrowsing`, `MenuThemeView` platformu kapatiyor) bilesen zaten pasif kalsin. Emote/idle pozu ile cakismasin:
   donen sey karakterin kok ya da pivot transform'u (animasyonun yazdigi kemik degil) — hangisi dogru, `MenuCharacter`/`MenuIdlePose`/
   `MenuEmotePlayer`'a bakip sec. Birakinca oldugu acida kalir (geri donmez).
3. **Builder**: `SceneHomeCharacter` bileseni karaktere ekler ve baglar (Find yok). Kostum ekraninda da karakter gorunuyorsa ayni bilesen
   orada da calisir (UI disindan surukleme).

Kisitlar: Malik'in dosyalari (Scripts/Physical, RunnerInput, prefab) hook'la kilitli — dokunma. Unity/batchmode/verify CALISTIRMA; sahne
uretimi supervizorde (needs SCENE). Calisma klasoru: `D:\Pundun\speed-starts-tema` (worktree, dal `agent/T-072-073`). Commit atma;
supervizor atar. Ayni worktree'de T-072 calisiyor (`RaceThemeView`, `SceneRaceTheme`, `SceneBuilder.cs`, `ShotTool.cs`) — onlara dokunma.

## Kabul kriterleri
- [ ] Home'da UI disindan yatay surukleme karakteri kendi ekseninde dondurur; birakinca yavaslayip durur, o acida kalir
- [ ] Butona/popup'a basip surukleme karakteri dondurmez
- [ ] EditMode: px->derece orani ekran genisligiyle olceklenir, atalet sonumu kare hizindan bagimsiz ve sifira iner (iliski assert'i)

## Geri bildirim
GORSEL  : karakter parmagi izleyerek doner, birakinca yumusak durur
SES     : yok
DOKUNSAL: yok
ZAMAN   : yok
KAMERA  : yok

## Kapsam disi
- Kamera yakinlastirma, dikey donme, karakteri secme/degistirme

## Calisma notu
(Agent doldurur: 5 satir)
