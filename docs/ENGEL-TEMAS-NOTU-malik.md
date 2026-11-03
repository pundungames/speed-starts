# Engel teması: Malik'in notu

**Tarih:** 2 Kasım 2026
**Durum:** Kodu yazıldı 3 Kasım 2026, Play'de denenmedi. `RunnerHurdles` artık bacak kemiklerini (uyluk, baldır, ayak, parmak) çizildikleri yerden okuyor. Engel çizgisinden bar üstünün `forgiveness` (0.15 m) kadar altından daha aşağıda geçen parça varsa engel devrilir ve hız düşer. `logClearance` her engelin ölçümünü konsola yazar. Botlar da aynı kodu kullanıyor. Karakter dosyalarına dokunulmadı.

## Sorun

- Yerde atlasam bile engel doğru sayılıyor.
- Bariyer devrilmiyor, içinden geçiyorum.

## İstenen

Gövde bariyere temas eder ya da içine girerse bariyer devrilsin.

**Botlar için de aynısı geçerli.**

## Başlangıç noktası

Sebep henüz incelenmedi.

- `Assets/Speed-Staes/Scripts/Physical/RunnerHurdles.cs`, `Update`: engel geçilirken yalnız `sim.JumpInAir` değerine bakıyor. Gövde yüksekliğine ya da gerçek bir temasa bakmıyor.
- Botların şeritleri de aynı `RunnerHurdles` sınıfını kullanıyor (`Scripts/Bridge/RaceBots.cs`, `CreateLane`).
