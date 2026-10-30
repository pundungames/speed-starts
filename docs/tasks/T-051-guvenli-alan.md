---
id: T-051
title: Guvenli alan (safe area) - centik/kamera deligi olan telefonlarda butonlar ve metinler kesilmesin
status: todo
owner: ui-agent
depends_on: []
owns_paths:
  - "Assets/Speed-Staes/Scripts/UI/Framework/SafeArea*.cs"
  - "Assets/Speed-Staes/Scripts/UI/Framework/FullBleed*.cs"
  - "Assets/Speed-Staes/Tools/Editor/Builders/Scene/SceneUiLayout.cs"
  - "Assets/Speed-Staes/Tools/Editor/Builders/Scene/SceneBuilder.cs"
  - "Assets/Speed-Staes/Tools/Editor/Builders/Ui/UiSafeArea*.cs"
  - "Assets/Speed-Staes/Tests/EditMode/UI/Framework/**"
  - "Assets/Speed-Staes/Tests/PlayMode/**"
touches_scene: true
branch: agent/T-051-guvenli-alan
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
YOL-HARITASI Faz 8: "Safe area / farkli en-boy oranlari". Projede `Screen.safeArea` hic okunmuyor. Hedef cihaz Android, yatay
(ornek Galaxy S23: yatayda bir kenarda kamera deligi). Kenara yapisik ogeler (kaynak cubugu, Back, ayarlar/ses/tema ikonlari,
X butonlari, yaris HUD'u) centigin altinda kalmamali; arka plan, dim ve tam ekran sanat ise **kenardan kenara** (centik dahil) kalmali.

Tasarim (kendin dogrula, gerekirse daha iyisini sec ve calisma notuna yaz):
- `SafeAreaMath` (saf): ekran boyutu + guvenli dikdortgen -> anchorMin/anchorMax; yalniz yatay kenarlar (dikey icin ayri bayrak),
  kucuk kirpma toleransi. EditMode testli.
- `SafeAreaFitter` (MonoBehaviour): bir RectTransform'u guvenli alana oturtur; `ISafeAreaSource` seam'i (varsayilan `Screen.safeArea`,
  testte sahte) ile; yalniz deger degisince yazar (Update'te GetComponent yok, onbellekli).
- Tam ekran kalmasi gerekenler icin: ya her kanvasta `SafeArea` kapsayicisi + dim/arka planlarda `FullBleed` (ebeveynin icbukeyligini
  geri acar), ya da ekran prefab'larindaki icerik kokune fitter. Hangisi builder'da daha az ve daha guvenli degisiklikse onu sec.
  Paralel calisan T-050 (giris kapisi builder'i) ve T-052 (ses) ile cakismamak icin degisikligi sahne katmanina
  (`SceneUiLayout`/`SceneBuilder`) ve yeni dosyalara topla; ekran builder'larina dokunman gerekirse en az satir.
- Popup'larin pencereleri zaten ortada: bunlarin icerigini kaydirmak gerekmez, dim'leri tam ekran kalir.

Kod kalici UI kurmaz (fitter yalniz var olan RectTransform'un anchor'unu degistirir). Baglama/prefab/sahne uretimi supervizorde:
ihtiyaclari `needs:`'e yaz.

## Kabul kriterleri
- [ ] EditMode: SafeAreaMath (centik solda, sagda, iki yanda, yok; dikey bayrak) ve fitter'in yalniz degisince yazmasi testli
- [ ] PlayMode: sahte guvenli alanla (ornek sol 120 px) Home ve Race'te kenar ogeleri (kaynak cubugu, Back/ayarlar) guvenli alan icinde,
      dim/arka plan tam ekran; var olan butun PlayMode testleri yesil; ekran goruntusu `Home_safearea` (sahte centikle)
- [ ] Centiksiz cihazda (safeArea = tam ekran) gorunum bugunkuyle ayni

## Geri bildirim
GORSEL  : yok (yerlesim)
SES     : yok
DOKUNSAL: yok
ZAMAN   : yok
KAMERA  : yok

## Kapsam disi
- Dikey yon (oyun yalniz yatay), farkli en-boy icin yeni yerlesim

## Calisma notu
