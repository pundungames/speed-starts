---
id: T-054
title: Giris kapisi cilasi - tasan buton yazisi, gereksiz satir, bosluk, ikincil buton etiketleri; butun durumlarin ekran goruntusu
status: merged
owner: ui-agent
depends_on: [T-050]
owns_paths:
  - "Assets/Speed-Staes/Scripts/UI/Home/EntryGate*.cs"
  - "Assets/Speed-Staes/Scripts/UI/Home/HomeTexts.cs"
  - "Assets/Speed-Staes/Tools/Editor/Builders/Ui/UiEntryGatePrefabs.cs"
  - "Assets/Speed-Staes/Tools/Editor/Builders/Ui/UiViewBinder.cs"
  - "Assets/Speed-Staes/Tests/EditMode/UI/Home/**"
  - "Assets/Speed-Staes/Tests/PlayMode/**"
touches_scene: true
branch: agent/T-054-giris-kapisi-cila
commits: [515218a, b85c824]
tests:
  - "Assets/Speed-Staes/Tests/PlayMode/ScreenshotTests.cs"
  - "mevcut EntryGateModel EditMode + EntryGateAdSmokeTests degismedi"
analytics:
  - "yok: projede analytics yok"
needs:
  - tip: SCENE
    durum: closed
    not: "EntryGateModal prefab'i yeniden uret (UiEntryGatePrefabs.EntryGate, overwrite) ve Home'u yeniden uret. Yeni yol: Window/Actions/{UseEntryTicket,UseTimedTicket} (VerticalLayoutGroup); Window/Remaining baslangicta pasif; TicketIcon RectMask2D ile alttan kirpildi. Sonra Shots_EntryGateStates + Shots_HomeAndRace ile 5 goruntuyu al; Home_entrygate_ready'de biletin altindaki cikinti gittiyse TicketTabCrop (0.10) dogru, kesiliyorsa dusur"
started: 2026-10-30
finished: 2026-10-30
blocked_reason:
---

## Hedef
T-050'nin giris kapisi calisiyor ama ekran goruntusunde (`pipeline-logs/shots-int7b-2026-10-30_125137/Home_entrygate.png`, 0/3 bilet
durumu) cila eksik:
1) Pasif **USE TICKET** yazisi butondan tasiyor (beyaz, buyuk). Etiket her durumda (aktif/pasif/basili) butona sigsin ve okunsun.
2) "**Left: 0**" satiri "Tickets 0/3" ile ayni bilgiyi tekrarliyor -> pencereden kalksin (model alani testlerde kullaniliyorsa kalabilir,
   yalniz gosterilmez).
3) USE TICKET ile GET TICKETS arasinda **buyuk bos alan** var (gizli USE TIMED TICKET butonunun yeri olabilir). Gizli satir delik
   birakmasin: ya yerlesim grubu ya da gorunurluge gore yeniden yerlestirme; pencere icerigi dengeli dagilsin.
4) **BUY WITH CASH** etiketi dikeyde ortali degil ve WATCH AD'den kucuk; iki ikincil buton ayni etiket stilinde, ortali.
5) `daily_ticket_icon`'un altindaki cikinti (sanat) goze batiyor: `ticket_flight_icon` daha temizse onu kullan, degilse kirp (sprite rect).
6) RACE ENTRY basligi seridin iki ucuna dayaniyor; biraz kucuk ve ortali.
Butun durumlarin ekran goruntusu: `Home_entrygate` (0 bilet) + yeni `Home_entrygate_ready` (1+ bilet, USE TICKET aktif),
`Home_entrygate_full` (3/3, TICKETS FULL), `Home_entrygate_timed` (Sureli Bilet stokta / etkin). Kurulum var olan PlayMode yardimcilariyla
(kayit/servis uzerinden), yalniz `-shotDir` ile calisir (ScreenshotTests deseni).

Mantik degismez (EntryGateModel/EntryGateAdFlow davranisi ayni); yalniz gorunum, builder ve metin. Kod kalici UI kurmaz; DOTween yalniz
cekirdek; dokunma alanlari gorunur bolge (TUZAKLAR). Prefab/sahne uretimi supervizorde (`needs:` SCENE).

## Kabul kriterleri
- [ ] Dort durumun goruntusunde tasan/kesilen yazi yok, bos delik yok, ikincil butonlar ayni stilde
- [ ] Var olan EditMode/PlayMode testleri yesil (gerekiyorsa guncellenir; davranis testi silinmez)
- [ ] needs: SCENE (EntryGateModal prefab + Home yeniden uretimi) yazili

## Geri bildirim
GORSEL  : var olan (punch/sallanma/donus) korunur
SES     : T-052 buton sesi
DOKUNSAL: T-052
ZAMAN   : yok
KAMERA  : yok

## Calisma notu
- 1) USE TICKET etiketi 52->42, rim icinde (0.14-0.86 x 0.16-0.84); pasifte etiket rengi koyu gri-mavi (view'da iki Color alani), aktifte beyaz.
- 2) "Left" satiri her zaman gizli; yalniz reddedilen giriste uyari olarak Refill satirinin yerinde gorunur. 3) Butonlar Window/Actions VerticalLayoutGroup'unda, gizli buton yer tutmaz; ust satirlar sikistirildi.
- 4) BUY WITH CASH / WATCH AD ayni boy (26), ayni dikey ortalama; fiyat butonun sag ucunda. 5) Ticket ikonu: ticket_flight_icon'da da ayni cikinti var, bu yuzden RectMask2D ile alttan %10 kirpildi. 6) Baslik 64->50, iki yandan icerde.
- Prefab/sahne uretilmedi (needs SCENE); gercek goruntu gelince kirpma orani ve yerlesim ayarlanabilir. Unity calistirilmadi.
