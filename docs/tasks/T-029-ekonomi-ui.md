---
id: T-029
title: Faz 3 ekonomi UI - giris kapisinda Sureli Bilet kullan, dolu envanter uyarisi, sonuc ekraninda Mor Mucevher
status: todo
owner: ui-agent
depends_on: [T-028]
owns_paths:
  - "Assets/Speed-Staes/Scripts/UI/Home/EntryGate*.cs"
  - "Assets/Speed-Staes/Scripts/UI/Home/HomeTexts.cs"
  - "Assets/Speed-Staes/Scripts/UI/Home/ResourceBarView.cs"
  - "Assets/Speed-Staes/Scripts/UI/Race/ResultsScreenView.cs"
  - "Assets/Speed-Staes/Scripts/UI/Race/RaceFlow*.cs"
  - "Assets/Speed-Staes/Tools/Editor/Builders/Ui/**"
  - "Assets/Speed-Staes/Tests/EditMode/UI/**"
  - "Assets/Speed-Staes/Tests/PlayMode/HomeRaceButtonSmokeTests.cs"
touches_scene: true
branch: agent/T-029-ekonomi-ui
commits: []
tests:
  - "Assets/Speed-Staes/Tests/EditMode/UI/Home/EntryGateModelTests.cs"
analytics:
  - "yok: projede analytics yok"
needs:
  - tip: WIRING
    durum: open
    not: "RaceFlow -> IRaceRewardResolver (yaris bitisinde bir kez); view'lar ITimedAccess alir"
started:
finished:
blocked_reason:
---

## Hedef
T-028'in ekonomi çekirdeğini oyuncuya gösterir:
1) **Giriş kapısı (Entry Gate modal):** Süreli Bilet alanı stok > 0 ve etkin değilken "Use Timed Ticket" butonu olur (`ITimedAccess.TryActivate`);
   etkinken kalan süre geri sayımı ("Timed Ticket active · 59:12"), "Use Entry Ticket" gizli (var olan kural). Adet Bileti tavandayken
   "Buy with Cash" ve "Watch Ad" pasif + "Entry Tickets Full" etiketi; fiyat yokken (0) Nakit butonu pasif, reklam SDK'sı yokken reklam pasif.
2) **Ana ekran kaynak çubuğu:** Süreli Bilet kutusu etkinken kalan süreyi gösterir (Adet Bileti sayacının yaptığı gibi).
3) **Sonuç ekranı:** yarış bitince `IRaceRewardResolver.Resolve` bir kez çağrılır (RaceFlow); kazanılan Mor Mücevher > 0 ise sonuç ekranında
   "+N" satırı ve Mor Mücevher sayacına punch. 0 ise satır görünmez (bugün yalnız 100m oynanabilir, ödülü yok: satır görünmez — doğru).
Var olan prefab builder'ları güncellenir (kod kalıcı UI kurmaz); metin legacy Text, İngilizce.

## Kabul kriterleri
- [ ] `EntryGateModel` saf: Süreli butonu görünürlüğü/etkileşimi, dolu uyarısı, Nakit/reklam pasifliği; EditMode testli
- [ ] PlayMode duman testi giriş kapısı akışıyla yeşil kalır (gerekirse Süreli Bilet kullanımı için yeni adım)
- [ ] Ödül çağrısı yarış başına bir kez (RaceFlowLogic testli)
- [ ] needs listesinde open WIRING/SCENE yok

## Geri bildirim
GORSEL  : Süreli Bilet etkinleşince kutu punch + geri sayım başlar; dolu envanterde butonlar gri; ödül satırı punch
SES     : yok (ses sistemi yok)
DOKUNSAL: yok
ZAMAN   : yok
KAMERA  : yok

## Kapsam disi
- Ekonomi kuralları (T-028), günlük hediye ve 7 gün (Faz 4), reklam SDK'sı

## Calisma notu
(Agent doldurur)
