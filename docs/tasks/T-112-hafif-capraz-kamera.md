---
id: T-112
title: Yaris kamerasi hafif capraz - sag/sol bacak ve kol ayirt edilsin, 2B hissi gitsin
status: merged
owner: gameplay-agent
depends_on: [T-107]
owns_paths:
  - "Assets/Speed-Staes/Scripts/Runner/SideFollowCamera.cs"
touches_scene: false
branch: agent/ik-kosucu
commits: [6709deeb]
tests:
  - "gerekmiyor: gorsel; RaceLanesSmokeTests ve T-107 goruntuleri bakilir"
analytics:
  - "yok: projede analytics yok"
needs:
  - tip: DECISION
    durum: closed
    not: "SideFollowCamera bu makinede kilitli (T-108 ile ayni karar). -> kullanici 04.11 acti"
started: 2026-11-04
finished: 2026-11-04
blocked_reason:
---

## Hedef
Slack: "biz direkt yandan baktığımız için karakter 2B gibi geliyor, hafif çapraz olunca sağ sol bacak kol ayırt ediliyor."

`SideFollowCamera.Desired()` ve `Facing()` tam yana sabit. Yeni alan `sideYaw` (deg, Tooltip'li): kamera konumu ve bakışı hedefin
çevresinde bu kadar döner; takip yalnız koşu ekseninde kalır. Başlangıç değeri 12, yön: koşucunun önüne doğru.

## Kabul kriterleri
- [ ] `RaceStartCamera`'nın yana dönüşü, `RaceFinishSequence` önüne dönüşü, `ReplayCamera`, zoom, sarsıntı ve `cameraBackPerLane` bununla sıçramadan çalışır (kullanan kodu oku, gerekiyorsa `Facing`'i public yap).
- [ ] Kulvar 1 ve 6'da, en az bir temada, ekran görüntüsünde arkaplanda boşluk/kesik yok (Serhat'ın bina satırları yandan bakışa göre dizili: bozuyorsa değeri düşür ve yaz).
- [ ] `RaceLanesSmokeTests` yeşil.

## Kapsam dışı
- Dünya/tema prefab'ları (Serhat'a sorulur).

## Çalışma notu
- SideFollowCamera: `sideYaw` (12, Tooltip'li); `Side()` yönü hem konumu hem bakışı döndürür; takip koşu ekseninde.
- leadAhead artık kamera yatay ekseni boyunca uygulanıyor (her yawda kadrajda aynı kayma).
- Start/Finish/Replay kamerayı gerçek konumundan okuyor (sabit yan yön yok); onlara dokunulmadı.
- ThemeRunPlayground.SnapCamera tam yan sabit (Serhat'ın): yalnız ilk kare, sonra LateUpdate yaw'a kayar.
- Derleme/görüntü doğrulanmadı (süpervizör).
- Süpervizör: ölçülen açı 12°, eğim 8°, hızlıda takip gecikmesiyle 10-19°. Yakın ve uzak kulvar görüntüsünde arka plan temiz (yalnız şehir teması bakıldı). Açılış testi yeşil; bitiş koddan okundu.
