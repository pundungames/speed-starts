---
id: T-085
title: Ekonomi v2 arayuzu - sonuc ekraninda kazanc dokumu, Nakit "+" -> reklamla Nakit penceresi, premium etkinlikte "300 Nakit -> 1 saat"
status: todo
owner: ui-agent
depends_on: [T-084]
owns_paths:
  - "Assets/Speed-Staes/Scripts/UI/**"
  - "Assets/Speed-Staes/Tools/Editor/Builders/Ui/**"
  - "Assets/Speed-Staes/Tests/EditMode/UI/**"
  - "Assets/Speed-Staes/Tests/PlayMode/**"
touches_scene: false
branch: agent/T-085-ekonomi-arayuz
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
T-084 (dal `agent/T-084-ekonomi-v2`, bu dal ondan acildi) cekirdegi kurdu; bu task arayuzu. Referans olcumu ve karar:
`docs/analiz/2026-11-03-referans-oyun-ekonomi.md`, `docs/decisions/ADR-2026-11-03-ekonomi-v2-referans.md` (guncelleme bolumu dahil).
Oyuncuya gorunen metin Ingilizce. UI kalici olarak kodla kurulmaz: prefab'lar mevcut builder'lardan (`Tools/Editor/Builders/Ui/**`) uretilir,
view `[SerializeField]` ile baglar (CALISMA-KURALLARI §3.1). Animasyon DOTween + `UiMotionConfig` (agent-kit/05).
1) **Kazanc dokumu** (sonuc ekrani): `ResultsScreenView` `IRaceCashRewardView.ShowCashReward(RaceCashReward)`'i uygular (`RaceViewContracts.cs`).
   Referanstaki gibi Nakit sayacinin altinda: "+30" ve satirlar "Finished race +10", "Difficulty bonus +10", "Rivals beaten +10" (sifir olan
   satir gizli; `IsEmpty` ise blok gizli). Nakit sayacina ucus (mevcut `RewardFlight` / gem odulu kalibi) + sayac guncellenir.
2) **Nakit "+" butonu** (kaynak cubugu, Nakit kutusu): `IRewardedCash` ile pencere: "Watch ad -> +200" satiri, kalan hak "0/3" sayaci,
   hak bitince / reklam yokken pasif + aciklama; basarida Nakit ucusu. Gercek para satirlari YOK.
3) **Premium etkinlik** (`requiresTicket`): giris penceresinde (`EntryGateModalView`) Adet Bileti / Sureli Bilet secenekleri yaninda
   "Spend 300 -> unlock for 1 hour" (fiyat config'ten, `ITimedTicketShop.TryBuyWithCash` + mevcut Sureli Bilet etkinlestirme tek dokunusta);
   Nakit yetersizse pasif. Metindeki sayi koddan degil config'ten.
Kontrat adlarini T-084'un dalindaki gercek koddan oku (`Scripts/Meta/Contracts/EconomyContracts.cs`, `RaceViewContracts.cs`); tahmin etme.
Baglama: installer'lara yazma -> needs: WIRING (tam satir). Prefab/sahne uretimi -> needs: SCENE (hangi builder metodu, ne beklenir).
Geri bildirim (agent-kit/03): buton basisi, Nakit ucusu, sayac punch, yetersiz Nakit'te titreme/ses.

## Kabul kriterleri
- [ ] Rakipli yaristan sonra dokum gorunur ve toplam Nakit sayacina ucar; solo yarista blok gizli
- [ ] Nakit "+" -> pencere; reklam +200, gunde 3; 3'ten sonra pasif
- [ ] Premium etkinlik girisinde 300 Nakit -> 1 saat calisir; yetersiz Nakit'te pasif
- [ ] Mantik (dokum satirlari, kalan hak metni, buton durumlari) EditMode testli; prefab icin PlayMode duman testi (gercek tiklama: `PlaySmokeSupport.Tap`)
- [ ] needs'te WIRING ve SCENE maddeleri tam

## Kapsam disi
- Gercek para / tam surum ekrani; cekirdek mantik (T-084); Malik'in kodu

## Geri bildirim
GORSEL  : Nakit ucusu + sayac punch; dokum satirlari sirayla belirir
SES     : mevcut odul/buton Sfx enum'lari (yeni ses yok)
DOKUNSAL: mevcut HapticTone (odul)
ZAMAN   : yok
KAMERA  : yok

## Calisma notu
(Agent doldurur: 5 satir)
