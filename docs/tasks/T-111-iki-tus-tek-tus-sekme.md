---
id: T-111
title: Iki tus birlikte cuval sekmesi, tek tus tek ayak sekmesi (gövde geriye, obur bacak duz ileri)
status: merged
owner: gameplay-agent
depends_on: [T-108, T-109, T-110]
owns_paths:
  - "Assets/Speed-Staes/Scripts/Physical/RunnerSim.cs"
  - "Assets/Speed-Staes/Scripts/Physical/RunnerPoseDriver.cs"
touches_scene: false
branch: agent/ik-kosucu
commits: [9c1134ca, 02309375, 8c708584, 8592a3d7, e797d6e3]
tests:
  - "Assets/Speed-Staes/Tests/PlayMode/RunnerSimRelationTests.cs"
analytics:
  - "yok: projede analytics yok"
needs:
  - tip: DECISION
    durum: closed
    not: "Scripts/Physical kilidi (T-108 ile ayni karar). -> kullanici 04.11 acti"
started: 2026-11-04
finished: 2026-11-04
blocked_reason:
---

## Hedef
Slack: "sağa sola aynı anda basarsam çuval gibi zıplama olmalı, bizde diz üstünde sekiyor gibi"; "sadece sağa/sola basınca bastığın
ayak tek ayak üstünde seker, üst vücut geriye yatar, diğer ayak ileri doğru düz şekilde denge sağlar".

Ölçüm (önce): `both030` en alçak diz 0.04 m, kalça 0.48-0.72 m: gerçekten dizde sekiyor. `same030` kalça aralığı 0.001 m (hiç sekme yok), basılan bacak −44..−50°'de geride takılı, karşı bacak 47-51° ileride, gövde eğimi ~0°.

Kendi hareketimiz: davranış tarif edildiği gibi, sayılar ve pozlar bizim. Mümkünse eşiksiz, sürekli bir durumdan doğsun (Malik'in dersi: `if (ikiTus)` modu değil).

## Yapılacak
1. Sim: `hopBias` benzeri sürekli değer (ad serbest, kaldırılmış adlar hariç): aynı bacağa art arda basış yükseltir, karşı bacak düşürür; `Press`'in mevcut `LastLeg == leg` koşulundan beslenir.
2. Tek tuş: basılan bacak gövdenin altında kısa sekme (yere varıp iter), karşı bacak `Front`'ta **havada** düz ileri (taşımaz), gövde `hopBias` kadar geriye (baş `HeadRest`'in ardında, `SitBack` kullanılabilir).
3. İki tuş: iki bacak aynı fazda iter, birlikte kalkar, ayaklar birlikte iner; kalça ve dizler açık, gövde dik. T-109'daki `Carries` düzeltmesiyle gövde dizlere inmez; `liftKick` + yerçekimi sekmeyi verir. Üst üste basışta her iniş yeni sekme.
4. Tutma/dalış akışı aynı kalır: basılı tutunca dalış hâlâ çalışır.

## Kabul kriterleri (T-107)
- [ ] `both030`: iki ayak birlikte yerden kesilir, hipY her basışta belirgin (`perfect`'ten büyük) sekme çizer, en alçak diz > kaval boyunun yarısı (dizler yere yakın değil).
- [ ] `same030`: karşı bacağın uyluğu ileride ve dizi açık kalır, ayağı yerde değil; gövde eğimi ortalaması < 0 (geride).
- [ ] `holdOne`, `holdBoth`: T-109 sonrasıyla aynı.
- [ ] `RunnerSimRelationTests`: art arda aynı bacak `hopBias`'ı yükseltir, ardışık bacak düşürür.

## Kapsam dışı
- Hız sayıları, kamera. Referans oyunun görüntüsünü kare kare kopyalamak.

## Çalışma notu
- Yapildi (worktree speed-starts-ik, derlenmedi/kosulmadi): `RunnerSim.HopBias` (ayni bacak art arda 1'e, obur bacak/cift tus 0'a, zamanla soner; hiza etkisi yok; alanlar `hopBiasFollow`, `hopBiasDecay`). Surucu: `_hop` (HopBias), `_pair` (iki bacagin basis zamanlari ayni fazda mi, `PairWeight`), `Leg.idle` (son basilmayan bacak `balanceLeg`'e: havada, duz, ileride, tasimaz), `hopBackLean` ile govde geriye, `hopRise` ile sekme (tek bacak ve cift tus), kollar bos bacak yerine kendi bacagini aynalar, cift tusta gait egimi 0.
- Zorlanilan: iki tus ile alternatif ayrimi `leg.lastPress` farkindan (mod yok); `same030` govde egimi < 0 icin `hopBackLean` 30 tahmini deger, olcumle ayarlanir.
- Test: RunnerSimRelationTests'e HopBias iliskileri eklendi. Dalis (holdOne/holdBoth) akisi hop 0 iken ayni; yalniz bacak `follow` kurali ve hold sonrasi bacaklar degisti.
- Süpervizör: iki tuş tamam. Tek tuş kullanıcı denemesinde yetersiz bulundu, T-113'e geçti. e797d6e3 tek tuş hız uçurumunu kapattı (0.20 sn: 0→6.7 m/s, hopFullGap 0.25).
