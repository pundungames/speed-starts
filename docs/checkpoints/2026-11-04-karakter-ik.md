# Checkpoint 2026-11-04 — karakter IK (Slack listesi), dal `IKTest` 5296ae3f..e797d6e3, `agent/ik-kosucu` bir adım önde

> Bir kez yazılır, sonra değişmez (tarihçe). Güncel durum `docs/DURUM.md`'dedir.

## Commit'li
- 0ee142e3, 923f3fa0, 76fd8428, 81c0bbfa, ac9ec5bc · T-107 · koşucu form ölçümü: 13 senaryo, CSV + sheet + oyun karesi — yeşil (hedefli, kopya, grafik açık)
- 4c377f94, 0374dcd7 · T-108 · basışın zamanlaması baş vuruşunu belirler, `Form`, iki tuş tek itiş — ilişki testleri yeşil, sim ölçümü
- 6709deeb · T-112 · hafif çapraz kamera `sideYaw` 12 — açılış ve kulvar testleri yeşil, açı ölçüldü, görüntü temiz (şehir teması)
- af65d019 · T-109 · koşu döngüsü, ayak kilidi, uçuş, sürekli koşu pozu — form ölçümü; iki kabul maddesi sayıyla tutmadı (task notunda gerekçe)
- 5366e19a, 8b7d38f8 · T-110 · gövde burgusu, eğim, kol pompası, birleşme — form ölçümü
- 9c1134ca, 02309375, 8c708584, 8592a3d7, e797d6e3 · T-111 · dört kusur, iki tuş ve tek tuş, kalça hız sınırı, tek tuş hız uçurumu — 15/15 hedefli yeşil
- Tam takım: PlayMode 88/92 (`9c1134ca`): 2 kırmızı değişmemiş kodda da kırmızı, 2 atlanan ölçüm testi. EditMode 1281/1285 (`8c708584`): 4 kırmızı, IK dalının dokunmadığı UI builder testleri. `8592a3d7` PlayMode koşusu kullanıcı kapatınca durduruldu, sonuç yok; `e797d6e3` için tam takım koşmadı.
- dd3b6fe0 · T-113 · yavaş eğilme, spam tabanı, tek tuş gallop ve kollar, zemin kırpması; `agent/ik-kosucu`'da, `IKTest`'te yok — **derlenmedi, ölçülmedi** (kullanıcı: ölçüm yarın). Yalnız `simprobe` ile sim sayıları var.

## Gözle bakılacak (merged)
- T-109, T-110, T-111, T-112: `IKTest`'te Play. Kullanıcının ilk tepkisi tek tuştaydı: boşta kalma (`e797d6e3` ile kapandı), zemine girme, ölü kollar, spamde durma (T-113).

## Açık / kullanıcıda
- Malik'e haber verilmesi kullanıcıda; önce `RunnerPoseDriver.cs` çakışmasını konuşmak gerekir (dosyanın ~1000 satırı değişti).
- Tuş süresi uçurumu ve spamde hız/eğim dengesi: kullanıcı denesin; öneri: telefon dokunuşuyla karar ver.
- `git push` yok; `IKTest` yerel.

## Kullanıcının commit'siz yerel işi
- Yoktu (başlangıçta temiz). `agent-kit.config.local.psd1` git'te değil, 4 satırı yorumda.

## Öğrenilenler
- Metronom senaryolar elle oynamayı göstermez; ilk teşhisler ölçümle çürüdü: bacak uca varıyordu, sorun kare dalga ve yürüyüş kalıbıydı; tekleme ise tek tuş hız uçurumu → `docs/TUZAKLAR.md` (proje). Genel ders: görsel iş için önce ölçüm düzeneği, sonra teşhis; kite taşınabilir (`install.ps1 -GeriTasi`), kullanıcı isterse.
- Ölçüm araçları `scripts/runner-probe.ps1`, `scripts/runner-olcum/`; çıktılar repo dışında `D:\Pundun\form-probe\`.
- Güvenlik dosyası için izin sistemi genel "istediğin kodlara gir"i kabul etmedi; dosya adını geçiren açık cümle yetti, ikinci geniş kaldırma yine reddedildi → TUZAKLAR §7'de var, tek not eklendi.
