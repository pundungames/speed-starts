---
id: T-105
title: Basma animasyonu - sonuc ekraninda dugme kucuk kaliyor, kartta hizli dokunusta olcek sisiyor
status: todo
owner: ui-agent
depends_on: []
owns_paths:
  - "Assets/Speed-Staes/Scripts/UI/Framework/ButtonFeedback.cs"
  - "Assets/Speed-Staes/Scripts/UI/Framework/UiPanel.cs"
  - "Assets/Speed-Staes/Scripts/UI/Framework/UiPunch.cs"
  - "Assets/Speed-Staes/Scripts/UI/Costume/CostumeItemCardView.cs"
  - "Assets/Speed-Staes/Scripts/UI/Costume/CostumeSkinSwatchView.cs"
  - "Assets/Speed-Staes/Scripts/UI/Costume/CostumeChannelBoxView.cs"
  - "Assets/Speed-Staes/Scripts/UI/Emotes/EmoteCardView.cs"
  - "Assets/Speed-Staes/Scripts/UI/Settings/FlagCellView.cs"
  - "Assets/Speed-Staes/Scripts/UI/Rewards/RewardDayCardView.cs"
  - "Assets/Speed-Staes/Tests/PlayMode/**"
touches_scene: false
branch: agent/T-105-basma-animasyonu
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
Kullanici (04.11): "resultta Race Again, Watch Replay falan basili tutunca buton kuculuyor, elimi kaydirip cekersem kucuk kaliyor; diger yerleri de kontrol edelim. Shop icinde contentlere hizli hizli tap yaparsam scale abarti buyuyor ve oyle kaliyor."

**Kok neden 1 (kucuk kalma, koddan dogrulandi):** `ResultsScreenView.OnEnable -> PlayReveal` reveal ogelerini (Nav dugmeleri dahil) `scale * StaggerScaleFrom`e indirir. Ilk etkinlesmede kokun OnEnable'i cocuklarin Awake'inden ONCE kosar, bu yuzden `ButtonFeedback.Awake` kucuk olcegi `_baseScale` diye kaydeder. Her basista `punchTarget.localScale = _baseScale` dugmeyi bu degere dondurur. Normal tikta ekran degistigi icin gorulmez; basili tutup kaydirinca ekran degismez, dugme kucuk kalir.
**Kok neden 2 (sisme):** Ayni transform'da iki sahip var: `ButtonFeedback` ve kartin kendi `Punch`'i (`EmoteCardView`, `CostumeSkinSwatchView`, `CostumeChannelBoxView`, `FlagCellView`, `RewardDayCardView`, `CostumeItemCardView`). Biri digerini oldurmez. DOTween baslangic degerini ilk guncellemede okur, yani diger tween'in sisirdigi degeri "baslangic" alir ve bitince oraya doner. Hizli dokunusta birikir.

Yapilacak:
1. `ButtonFeedback.Initialize` public + idempotent; `UiPanel` kendi Initialize'inda (kokun Awake'i, animasyondan once) `GetComponentsInChildren<ButtonFeedback>(true)` hepsini baslatir. Boylece tasarim olcegi her ekranda animasyondan once okunur (yalniz Results'a ozel yama yazma).
2. Ortak yardimci `UiPunch` (Framework): `target.DOKill()`, olcegi tasarim degerine koy, `DOPunchScale`, `ForceInit` (baslangic degeri simdi okunsun), `OnComplete`'te tasarim olcegine geri yaz; config sayilari `UiMotionConfig`ten. `.SetUpdate(true).SetLink(...)` korunur. `ButtonFeedback` ve yukaridaki kart view'lari bunu kullanir. Her sahibin `_punch` alani OnKill ile temizlenir (mevcut kalip).
3. Reveal suren dugmeye basilirsa: bitiste olcek tasarim degeri olmali (OnComplete + reveal'in son yazimi).
4. Diger yerler: `Scripts/UI` altinda her `DOPunchScale`/`DOScale` noktasini gez; hedef transform'da `ButtonFeedback` varsa ya da ayni transform'u baska bir tween suruyorsa yardimciya gecir. Digerlerine dokunma. Hepsinin listesini ve karari calisma notuna yaz.

## Kabul kriterleri
- [ ] Results'ta her Nav dugmesi: reveal bittikten sonra basili tutulunca olcek tasarim degerinin altina inmez; kaydirip birakinca olcek tam tasarim degeri
- [ ] Reveal sirasinda basilan dugme animasyon bitince tasarim degerinde
- [ ] Emote karti, kostum karti, ten swatch, bayrak hucresi: kareler arasi hizli 10 dokunus sonrasi olcek tasarim degerinde; tepe olcek tek punch sinirini (`1 + ButtonPunchScale`) belirgin asmaz
- [ ] Test sabit denge sayisi assert etmiyor: tasarim olcegi prefab'tan, punch miktari `UiMotionConfig`ten okunur
- [ ] PlayMode testleri gercek olayla basar: `PlaySmokeSupport.Tap` ya da `ExecuteEvents` pointerDown / pointerExit / pointerUp
- [ ] needs listesinde open WIRING/SCENE yok

## Geri bildirim
GORSEL  : mevcut punch korunur; yalniz yanlis olcek duzelir
SES     : degismez
DOKUNSAL: degismez
ZAMAN   : yok
KAMERA  : yok

## Kapsam disi
- Punch sayilari (`UiMotionConfig`). Prefab/sahne degisikligi. SpriteSwap sanati. Navigation modu (backlog).

## Calisma notu
