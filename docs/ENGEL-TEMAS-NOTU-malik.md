# Engel teması: Malik'in notu

**Tarih:** 2 Kasım 2026
**Durum:** Açık. Karakter işi bittikten sonra Malik dönecek; o zamana kadar dokunulmayacak.

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
