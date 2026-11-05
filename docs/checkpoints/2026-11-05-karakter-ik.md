# Checkpoint 2026-11-05 — karakter IK, dal `IKTest` e797d6e3..507c8110 (origin/IKTest f3d231c5, yerel 58 commit önde)

> Bir kez yazılır, sonra değişmez (tarihçe). Güncel durum `docs/DURUM.md`'dedir.

## Commit'li
- T-113..T-127 · ezme (`earliness`), zemin, kol, ayak kilidi, tek ayak, çift zıplama, fren `stopBrakeForce` 12, spam kapısı, stock poz. T-121 oyunda bozdu, `cb7e9985` ile geri alındı, parça parça döndü. Tam takım son kez `cb1763bf`: PlayMode 98/102, EditMode 1281/1285, 6 kırmızı bilinen.
- a9340e38 · T-128 geniş adım. d976a851, 0b674c70 · T-129 tek tuş tutma ve dalış (Malik'ten elle). 625f6923 · T-130 yerdeki ayak öne gideni bekler. b6123b34 · T-131 çift zıplamada bacaklar birlikte.
- 1ae44ce4, 176d56f5, 24fc81e7 · T-132 koşu zıplaması yarı, omuz az dirsek çok, diz sonra. 92099e49, 6d74bcea, 3b283905 · T-133. 26efc554, be1e3a54, f11b560d · T-134 ön bacak alçak, arka bacak geride. 657b7094 · T-135 sekişte ayak değişimi. eaf9533e · T-136 çift basış sekiş sayılmaz. ae7549d1 · T-137 basarken diz kırık.
- 2ffb57e7, 812d65fb · T-138 kamera Y sabit, yumuşak izleme.
- 78fd292d · T-139 seri kare ölçümü (`RunnerTransitions_WriteFrameStrips`, `transition_check.py`).
- 443b4183, 5d19ddf4, 7474a1b0, 5f0ebc11, 6f6015d6 · T-140 `Track` freni, çivili ayak, `holdAfter`, ağırlık yayları, sallanma. 9b01cfa1, 45128afc, 5e2bad9d, 159982b8 · T-141 basma karesi, tek ayak yayı, tutma yayı, fren sınırı. e254bf11, 4a2b6dc0, 9aadc3d2 · T-142. 429454c8, 4e9bceaa · T-143 dalış.
- 1176dd99, ade4e614, 9169217d · T-144 Malik'in sırt ve kafa zinciri, `RunnerSim.ShownHeadAngle`.
- Doğrulama: T-128..T-138 yalnız derleme; T-140..T-144 seri kare kaydı (derleme dahil), `D:\Pundun\form-probe\t139-before` .. `t144-after`. Tam takım T-117'den beri koşmadı.

## Ölçülen etki (t139-before → t143-after)
Uyluk tepesi 81-115° → 61-68° · havada diz 177° → 150-166° · basışta ayak sıçraması 46-81 → 1-5 cm/kare · 0.18 sn tuşta yanlış tutma 16 → 0 · basışta diz sarsıntısı 37-52° → 15-20° · hızlı koşuda kalça kare titremesi 6.5 → 1.2/sn · tutma çıkışı kalça 15.6 → 4.1°.

## Gözle bakılacak
- `IKTest`'te Play: kullanıcı T-144 sonrası üç şikayet verdi (DURUM "Yarın ilk iş"). Geri kalanına "her şey çok güzel oldu" dedi.

## Açık / kullanıcıda
- Üç not yarına: bacağın karına çekilmesi, kolların hıza göre yumuşaması, koşudan tek ayağa geçiş.
- `git push` kullanıcıda. Malik'in dalı birleştirilmedi; kullanıcı "birleştirme yapma" dedi. `RunnerPoseDriver.cs` iki dalda çok ayrıştı (ilk denemede 36 çakışma, şimdi daha fazla).
- Agent'ın bıraktığı boşta python süreçleri (PID 17400, 7796): kapatma izni çıkmadı, zararsız.

## Kullanıcının commit'siz yerel işi
- `ProjectSettings/EditorSettings.asset` (ana klasör). Dokunulmadı. `agent-kit.config.local.psd1` git'te değil.

## Öğrenilenler
- Seyrek kareyle ve sim sütunlarıyla karar vermek yanılttı: "balerin", "diz düz" haftalardır anahtar sayısı sanıldı, kök sebep `Track`'in frenlemeyi de sınırlamasıydı. Her düzeltmeden sonra bütün geçişleri yeniden kaydet: `holdAfter` dalışı bozmuştu, ancak böyle görüldü.
- Başka daldan alınan kod fiziği de varsayar: Malik'in zinciri `HeadAngle` okur, bizim fizikte ritimde sabit −15. → `docs/TUZAKLAR.md`.
- Ölçüm: `scripts/runner-probe.ps1 -Filtre RunnerTransitions_WriteFrameStrips`, `scripts/runner-olcum/transition_check.py`, karakter için `frames.py`/`series.py` benzeri küçük dökümler (scratchpad'de kaldı, gerekirse yeniden yazılır).
