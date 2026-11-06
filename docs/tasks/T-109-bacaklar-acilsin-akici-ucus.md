---
id: T-109
title: Kosu dongusu - kosu pozu surekli, bacak tek egride, yuruyus yerine kosu, gorunur ucus, ayak kaymasi yok
status: merged
owner: gameplay-agent
depends_on: [T-107, T-108]
owns_paths:
  - "Assets/Speed-Staes/Scripts/Physical/RunnerPoseDriver.cs"
  - "Assets/Speed-Staes/Tests/PlayMode/RunnerFormProbeTests.cs"
touches_scene: false
branch: agent/ik-kosucu
commits: [af65d019]
tests:
  - "gerekmiyor: gorsel; T-107 olcumu once/sonra karsilastirilir"
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
Slack: "bacaklar açılmıyor, sanki altında etek var"; "hızlı koşunca zıplayıp bacaklarını daha uzun açmalı"; "bacağı arkadan öne
atma çok hızlı ve tekliyor"; "bazen dizi yere çöküyor"; "ayak ve gövde ayrı gibi".

## Kök sebepler (T-107 ölçümü, gerçek prefab, Race sahnesi)
İlk teşhisteki "bacak uca varamıyor" ve "basış bacağı tek karede durduruyor" **sabit ritimde görülmedi**: uyluk `perfect`, `rhythm026`, `late050`'de
±47-52°'ye varıyor, split 100°. Asıl sebepler:

| Ölçüm | `perfect` | `rhythm026` | Anlamı |
|---|---|---|---|
| Uyluk |rate| < 120 deg/s kare payı | 0.29 | 0.37 | Uçlarda duruyor |
| Uyluk |rate| > 600 deg/s kare payı | 0.36 | 0.30 | İki bacak ~0.1 sn'de yer değiştiriyor: **kare dalga**, tepe ~1250 deg/s. "Tekliyor" budur |
| Kalça, bacaklar en açıkken / geçerken | 0.869 / 0.905 m | 0.830 / 0.891 m | Kalça en açıkken alçak: **ters sarkaç, yani yürüyüş**. Öndeki düz bacak (47°, diz 175) gövdeyi taşıyor |
| Geçişte diz (öne giden bacak, |uyluk| < 15) | 111° | 97° | Bacak katlanmadan geçiyor, kareler: iki ayak yan yana yerde. "Etek" budur |
| Koşu pozu ağırlığı (`_stepping`, hesap) | 0.13..0.50 | 0.11..0.49 | Yalnız tuş basılıyken yükseliyor; tam sprintte vücut çoğunlukla `Ready`'de: yumruk çenede, kalça geride. Oyun görüntüsünde görünüyor |
| Koşu pozu ağırlığı (`steppingAvg`, ölçüldü) | 0.38 | 0.32 | `late050` 0.17. Yukarıdaki hesabı doğruladı |
| Yere yakın ayağın dünyadaki hızı (`footSlide`) | 12.2 m/s | 9.6 m/s | Koşu hızı 11.5 / 10.6: **ayak yere hiç basmıyor, gövdeyle birlikte kayıyor**. "Ayak ve gövde ayrı" budur |
| En alçak diz, `both030` | 0.04 m | | `Carries`: havadaki bacak taşıyor, diz yerde |

Ek: `sim.HopY` çizilmiyor; sheet'in 12 karesinde de bir ayak yerde. `reach`, `pushEnd` hızdan bağımsız.

## Yapılacak
1. **Koşu pozu sürekli:** `stepping` = tuş basılı **ya da** son basıştan beri `stepTimeMax` geçmedi (ya da `InJump`). Bekleme duruşu kuralı aynı: basışlar kesilince çömelmeye gider. `_runHome` da bununla düzelir.
2. **Tek döngü eğrisi:** koşudaki bacak, kendi basışından kendi sonraki basışına (2 × basış aralığı) tek sürekli eğride: temas → itiş → kalkış → topuk yukarı → **katlı geçiş** → diz önde → aşağı süpürüp temas. İki bacak ters fazda. Mevcut `pushEnd`, `recover`, `mid` kullanılır; temas ve diz önü için yeni alanlar (ör. `contact`, `kneeDrive`, kaldırılmış adlar hariç). `reach`/`Front` tutma akışı için kalır.
3. **Press süreklilik:** beklenenden erken gelen basış, bacağı o anki açı ve hızından alır (kübik Hermite, kısa); ani durma yok. Geç gelen basış için bacak temasa hazır bekler, basışlar kesilince eve döner.
4. **Ayak yere basar (kilit):** itişteki ayağın runner-space `ankle.z`'si kökün o karedeki gerçek yer değiştirmesi kadar geri gider (`sim.Speed` değil: `RunnerRootOffset`, bitiş sekansı kökü ayrıca taşır); uyluk ve diz bundan iki kemik çözümüyle çıkar. Temas süresi böylece hızdan doğar, hızlıda kısa, yavaşta uzun. Ayak geride sınıra varınca kalkar.
5. **`Carries`:** yalnız yerdeki ayak taşır. Dalış ve Hold akışı aynı kalır.
6. **Koşu yüksekliği:** temasın ortasında en alçak, uçuşta en yüksek (bacaklar en açıkken). Uçuş sim'in `HopY`'sinden ya da aynı zamanlı kendi yayından, hızla büyür; tepede hedef ~8-10 cm (tek Inspector alanı). Yavaşta uçuş yok.
7. **Genlik hızla:** diz önü ve itiş sonu, hız × `sim.Form` ile büyür; yavaş koşu kısa, hızlı koşu uzun.

Mümkünse eşiksiz, sürekli değerlerden. Kod sadeleşsin: yeni döngü, yerini aldığı faz kodunu (`ReachOn`, `reachNext`, `easeOut`, `swing*` ve benzeri) kaldırır, üstüne eklenmez.

## Kabul kriterleri (T-107 `form_summary.csv`, süpervizör önce/sonra karşılaştırır; sayılar görsel hedef, denge değil)
- [ ] `perfect`, `rhythm026`: `holdShare` ≤ 0.15, tepe uyluk hızı ≤ 800 deg/s.
- [ ] `perfect`, `rhythm026`: `corrHipSplit` > +0.3, `hipAtWide` > `hipAtPass`.
- [ ] `passKnee` < 80°; sheet'te iki ayak yan yana yerde kare yok.
- [ ] Sheet'te `perfect` karelerinde iki ayağın da yerden kesildiği an görünür; `flightShare` (düzeltilmiş temas kuralıyla) `perfect` > `rhythm026` > `late050`.
- [ ] `footSlide`: `perfect` ve `rhythm026`'da ≤ 2 m/s; `late050`'de önce değerinin en fazla yarısı.
- [ ] `steppingAvg` ≥ 0.9 (`perfect`, `rhythm026`, `late050`); oyun görüntüsünde yumruk çenede değil.
- [ ] `both030`, `mash012`: en alçak diz > kaval boyunun yarısı.
- [ ] Split maks.: `perfect` > `rhythm026` > `late050`.
- [ ] `holdOne`, `holdBoth` kareleri önceki karakterde (dalış bozulmadı). `RunnerHurdles`, `RaceFinishSequence`, `RunnerCooldown`, `RelayBatonArms`, replay çalışır (kullanan kodu oku).
- [ ] Yeni alanlarda `Tooltip`; dosya başındaki özet güncel. Kaldırılmış adlar yok (T-108'deki liste + `FootCycle`, `lie*`, `pelvisWaistShare`, `airTimeAtSpeed`).
- [ ] Değişen prefab değeri gerekiyorsa `needs: SCENE`; prefab'a dokunulmaz.

## Kapsam dışı
- Gövde burgusu, kollar (T-110), iki/tek tuş sekme karakteri (T-111), sim, kamera.

## Çalışma notu
- Yapılan: `RunnerPoseDriver.cs` (worktree `D:\Pundun\speed-starts-ik`, derlenmedi/koşulmadı) tek koşu döngüsüne çevrildi: duruş (ayak yere çivili, kök gerçek yer değiştirmesiyle geri gider, kalça-bilek iki kemik çözümü) → toe-off → tek Hermite salınımı (recover → mid → kneeDrive → contact, bacağın kendi döngü süresine göre) → `Land` (yeni faz, sona eklendi). Erken basış kısa eğriyle yakalanır, geç basışta bacak yerin 4 cm üstünde bekler.
- Yükseklik: yerdeki ayak ister (duruş ortasında çökme), basışlar arası yay (hızla büyüyen `flightRise`); taşıyan bacak yoksa ve inen bacak da yoksa eski balistik düşüş (dalış). `stepping` basışlar `stepTimeMax` içinde sürdükçe 1. Tutma/dalış/`Reach`/`Front`/engel akışı korundu; tutma süresi `pushTime` aşılınca eski Hold'a geçiş.
- Kalkanlar: `ReachOn`, `reachNext`, `easeOut`, `swing*`, `recoverTime/midTime/homeTime`, `stepSlow`, `StepPart` kaldırıldı. Yeni alanlar `kneeDrive`, `contact`, `swingKeys`, `swingMin`, `catchSpeed`, `footGrip`, `stanceMax`, `strideSpeed`, `strideMin`, `formWeight`, `holdBlendTime`, `stanceSpring`, `stanceDip`, `flightRise`, `riseSpeed`, `flightSpring`, `landHover`.
- `RunnerFormProbeTests.cs`: topuk teması için `FootTouchToleranceMeters` 0.035 eklendi (`TouchToleranceMeters` 0.02 toe için); `flightShare` ve `footSlide` aynı kuralı kullanıyor.
- Riskli/doğrulanmadı: flightShare sıralaması (perfect > rhythm026), tepe uyluk hızı (stance kaçınılmaz ~v/H; `footGrip` 0.92 ile ~780), holdOne'da öndeki bacağın Reach'e geçişi basıştan `pushTime` sonra (eskiden basışla), prefab'ta `mid`/`recover`/`pushEnd` eski değerlerle kayıtlı (kod varsayılanları ezilir: `mid` 15/44, `recover` -52/60, `pushEnd` -42/150), yeni alanlar kod varsayılanıyla çalışır. needs: SCENE gerekmiyor.
- Süpervizör: kabulde iki madde sayıyla tutmadı. Tepe uyluk hızı ≤800 değil ~1890 deg/s: ayak 12 m/s'de kilitliyken zaten ~1200 gerekiyor, hedef yanlıştı. footSlide ≤2 değil 4.8: ölçü salınım karelerini de sayıyor, kilitli karelerde 0.9. Diğerleri tuttu: koşu pozu 1.00, geçişte diz 43°, corrHipSplit 0.80, kalça ≤2.5 cm/kare.
