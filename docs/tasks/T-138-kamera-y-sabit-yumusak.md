---
id: T-138
title: Yaris kamerasi Y'de sabit, basislarda sallanmasin, kosu boyunca yumusak izlesin
status: review
owner: gameplay-agent
depends_on: [T-137]
owns_paths:
  - "Assets/Speed-Staes/Scripts/Runner/SideFollowCamera.cs"
  - "Assets/Speed-Staes/Tools/Editor/Builders/ShotTool.cs"
touches_scene: false
branch: agent/ik-kosucu
commits: [2ffb57e7, 812d65fb]
tests:
  - "gerekmiyor: kullanici 05.11 'test etmeden yap'; gorsel, derleme + kullanicinin gozu"
analytics:
  - "yok: projede analytics yok"
needs: []
started: 2026-11-05
finished: 2026-11-05
blocked_reason:
---

## Hedef
Kullanici 05.11: "kamera direkt karakteri focusluyor, en ufak titresimde kamerada sallaniyor. kamera y de sabit olsun, daha smooth olsun."

## Kok sebep (kod okundu)
- Hedef kosucunun koku, kalca degil; kokun Y'si sabit. Kamera Y'si `Desired()`'da zoom'a bagli: basis zoom'u (`_pressZoom`) her basista yuksekligi oynatiyor.
- Erken ve gec basista `wrongStepShake`: 0.03 m, Y dahil. Tam zamaninda basmamak oyunun normali, surekli caliyor.
- Kosu ekseninde `Vector3.Lerp(_position, Desired(), 1 - exp(-12 dt))`: kosucunun hiz dalgalanmasini kameraya geciriyor; hareketli hedefte gecikme kare suresine bagli, kare suresi oynadikca titriyor.

## Isler (iki commit)
1. Y sabit (kosu hizindaki yukseklik, hizli kosuda goruntu bugunkuyle ayni); sarsinti yalniz yatay; yanlis basis sarsintisi kalkar.
2. Kosu ekseninde kamera hizi yumusatilmis kosucu hiziyla ilerler, sapmayi yavasca kapatir; zoom daha yavas akar.

## Varsayimlar
`trackSpeedSmooth` 0.35 sn, `trackPull` 2.5, zoom 0.45 / 0.6 sn. Inspector'dan degisir.

## Kapsam disi
- Baslangic ve bitis kamerasi, replay kamerasi, FOV, aci, mesafe sayilari. Prefab, sahne. Unity, test, olcum yok.

## Calisma notu
- Commit 1 (2ffb57e7): Y sabit (StandingHeight = tam zoom yuksekligi), yanlis basis sarsintisi kalkti, hit sarsintisi yalniz yatay.
- Commit 2 (812d65fb): kosu ekseninde yumusatilmis hizla izleme, zoomFlowOutTime/InTime yeni alanlar; derlenmedi, calistirilmadi.
- ShotTool.SnapFollowCamera durus yuksekligini kendi hesapliyor (StandingHeight artik zoom'lu).
- ThemeRunPlayground.SnapCamera (Serhat) StandingHeight'i okuyor; durus mesafesinde yuksek kalir, bir kare sonra takip duzeltir; dokunulmadi.
