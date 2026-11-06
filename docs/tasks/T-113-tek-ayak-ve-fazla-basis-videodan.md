---
id: T-113
title: Oyun denemesi ve referans video - tek ayak gallop ve kollar, zemine girme, her tap ilerletsin, fazla basis yavas yavas
status: merged
owner: gameplay-agent
depends_on: [T-111]
owns_paths:
  - "Assets/Speed-Staes/Scripts/Physical/RunnerPoseDriver.cs"
  - "Assets/Speed-Staes/Scripts/Physical/RunnerSim.cs"
  - "Assets/Speed-Staes/Tests/PlayMode/RunnerSimRelationTests.cs"
touches_scene: false
branch: agent/ik-kosucu
commits: [dd3b6fe0]
tests:
  - "Assets/Speed-Staes/Tests/PlayMode/RunnerSimRelationTests.cs"
analytics:
  - "yok: projede analytics yok"
needs: []
started: 2026-11-04
finished: 2026-11-05
blocked_reason:
---

## Hedef
Kullanıcı 04.11 akşamı `IKTest`'te oynadı ve referans oyundan dokunuşları görünen bir video verdi
(`Downloads/Screen_Recording_20261104_174949_Speed Stars.mp4`). Video davranışı anlamak için; pozlar ve sayılar bizim, kare kare kopya yok.

Kullanıcının sözleri: "tek ayak koşarken karakter zeminin içine giriyor, kol çok az hareket ediyor, sanki kendini zıplatıyormuş hissi vermiyor,
kollar daha canlı olsun"; "her tap bizi ilerletmeli, boşta duruyorum"; "sürekli sağ sol spamlersem ona da min bir basış hızı ekle, olduğu
yerde kaldı, ilerlesin yine de ama kör topal".

Videodan okunanlar:
- Tek tuş (`c_hold`, ~2 dokunuş/sn): basılan bacak gövdenin gerisine geniş gider, diz açık, topuk az kalkar; öbür bacak önde düz, ayağı yer hizasında; gövde 10-20° geride, kollar göğüs önünde.
- Fazla basış (t≈15-22 sn, cadence 5→9.8): hız ~6 sn en iyi hâlinde kalır, enerji düşer, gövde yavaş yavaş öne yatar (30°, 60°, yatay), bacaklar altta kısa kısa; sonra hız düşer (18→14→9 mph, ~4 sn). Dokunuş 4/sn'ye inince ~2 sn'de dikleşir. Bizde 1 sn'nin altında kapanıyordu.

## Yapılacak ve kabul
- [ ] Zemin: hiçbir senaryoda, koşunun başı dahil, ayak ucu ve bilek dinlenme yüksekliğinin 0.5 cm altına inmez. Kök sebep: `Update`'te zemin kırpması `SitBack`'ten önce; `same025` t=0.80'de ayak ucu −6 cm, bilek −4 cm.
- [ ] Tek tuş gallop: `hop*` anahtarları geniş; öbür ayak yerden en fazla ~5 cm; kalça ≤ 3 cm/kare, baş düz.
- [ ] Kollar: tek tuşta iki kol basışla yukarı-öne vurur (yaklaşık −45..+60, dirsek ~90), hızlı çıkış, yumuşak dönüş; her aralıkta (0.16-0.40) canlı.
- [ ] Her tap ilerletir: tek tuşla duruştan ilk saniyeler canlı; tek tuş ardışık koşuyu geçmez.
- [ ] Taban: `tapFloorSpeed` 3.2, `tapFloorGain` 0.6: sürekli spamde koşucu yaklaşık 2.5-3.5 m/s ile topallayarak ilerler, yavaş ritmin (0.50) altında kalır; iyi ritim ve başlangıç değişmez.
- [ ] Fazla basış birikerek: spamde baş ~2 sn'de 30°, ~4-5 sn'de 60°, ~6-7 sn'de 80°; ilk ~3 sn hız %15 içinde; ritme dönünce ~2 sn'de 15° altı. Tutma ve dalış aynı hızda kalır.
- [ ] İyi satırlar (en iyi, 0.24-0.30, 0.50) ±%2; ilişki testleri yeşil; `simprobe`, form ölçümü ve tam PlayMode bakılır.

## Kapsam dışı
- Prefab, sahne. Referansın görüntüsü, pozları, sayıları birebir.

## Çalışma notu
- Kod yazıldı, `dd3b6fe0`. Unity'de derlenmedi, form ölçümü yok; simprobe: spam 3.0-3.1 m/s, baş 30° 2.4 sn, 60° ~6 sn, 80° ~7 sn, ritme dönünce 15° altı 1.2 sn; tek tuş 6.1-7.0 m/s, duruştan 0.25 sn'de 1/2/3 sn: 2.6/3.9/4.8. Spamde 3 sn'de hız −%25 (hedef −%15, bilerek). Kırılgan: `MashingShowsMoreHeadThanPressingEarlyThanARhythm`.
- 05.11 ilk derleme ve ölçüm (`D:\Pundun\form-probe\t113\`): derleme temiz, ilişki testleri 15/17, iki kırmızı ezme testi; açık kalan kabuller T-114 (ezme, sim) ve T-115 (zemin, kollar, poz) ile kapanacak. İyi satırlar ±%2 tuttu.
- 05.11 birleşme: `IKTest` `cb1763bf` (T-114, T-115 ile). Tam takım: PlayMode 98/102 (2 kırmızı `ReferenceUiReviewTests`, bilinen; 2 atlanan ölçüm testi), EditMode 1281/1285 (4 kırmızı UI builder, bilinen). Tek tuş görünümü T-116'da devam ediyor.
