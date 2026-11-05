MOD: süpervizör
# DURUM — Speed Stars (speed-starts)

> Oturum başı ilk okunan dosya. En fazla 60 satır; her checkpoint'te BAŞTAN yazılır (eklenmez).
> İlk satır geçerli moddur: `MOD: süpervizör` ya da `MOD: hızlı` (bkz. docs/CALISMA-KURALLARI.md §14 A).
> Geçmiş: `docs/checkpoints/`. Alttaki OTOMATIK bölümü `scripts/tur-sonu.ps1 -DurumYaz` yazar; elle düzenleme.

Son güncelleme: 2026-11-05 (süpervizör, tur içi). Son checkpoint `docs/checkpoints/2026-11-04-karakter-ik.md`. Push kullanıcıda.

## Şu anki iş: karakter IK (Slack listesi; T-107..T-143)
Kullanıcı: "istediğin kodlara gir, bizimki iyi olursa bizim sistemi kullanıcaz", "izin isteme, ne gerekliyse yap", "yapıldıkça bana geçir", **"test etmeden yap işlerini, en son ben bakarım" (05.11: koşucu görsel işinde test ve ölçüm turu yok, yalnız derle ve geçir)**. Ana kural: input yalnız hedef değiştirir, uzuv resetlenmez, snap yok; hareket geniş ve esnek. Malik'e haber verilip verilmediği belli değil; aynı dosyalara girerse çakışır.
- **Dallar:** kullanıcı `IKTest`'te (ana klasör, Unity açık). İş `agent/ik-kosucu`'da, worktree `D:\Pundun\speed-starts-ik`. Birleştirme `git merge --ff-only agent/ik-kosucu`. `agent/ik-govde` ve worktree'si birleşti, silinebilir.
- **Malik'in dalı (`claude/karakter-hareketleri-baec15`, 05.11):** kullanıcı "birleştirme yapma" dedi; deneme birleştirmesinde sürücüde 36 çakışma. Oradan yalnız geniş adım (T-128) ve tek tuş tutma ile dalış (T-129) elle yazıldı. Prefab'ı alınmadı. Onun `hipShare` kodda 0.45 yazıyor ama prefab'ta 0.65. `PhysicalTest.unity`'de engelleri silen commit'i ve yalnız o sahnede `pushEase` 2 ayarı var.
- **05.11 son turlar:** T-127 tek ayağa geçiş, gövde sallanması (`rideBob`), T-128 geniş adım (`strideMin` 0.8, `recoverFast`, `kneeOpen`), T-129 tutma ve dalış (`airTip`, `settle*` kod dışı), T-130 yerdeki ayak öne gideni bekler (`OtherTakesOver`, `swingBackLimit`, `HeelEndShare`), T-131 çift zıplamada bacaklar birlikte, T-132..T-137 diz, kol, zıplama yarıya, sekişte ayak değişimi, çift basış. T-138 kamera (Y sabit, `trackPull`).
- **Seri kare (kullanıcı istedi, T-139..T-143):** `-Filtre RunnerTransitions_WriteFrameStrips` ile `scripts/runner-probe.ps1`, sonra `python -I scripts/runner-olcum/transition_check.py <önce> <sonra>`; çıktı `D:\Pundun\form-probe\t139-before`, `t143-after`. Düzeltilen: `Track` freni, ayak ışınlanması, `holdAfter` 0.22, ağırlıklar yayla, sallanma ritme göre, basma karesi (`plantGap`), tek ayak yayı, dalış. Oyunda kullanıcının gözü bekleniyor; T-128..T-143 `review`.
- **05.11:** T-113, T-114 (ezme, `earliness`), T-115 (zemin, kollar) `merged`; tam takım `cb1763bf`'te PlayMode 98/102, EditMode 1281/1285, kalan 6 kırmızı bilinen. T-116..T-119, T-121..T-123 `review`: kullanıcı bakacak, tam takım koşmadı. T-121 oyunda bozdu (bir basışa iki sekme), geri alındı (`cb7e9985`); yalnız kollar, üst beden ve `liftKick * _dive` döndü. Kural: kör turda her geçişte tek değişiklik. Kullanıcı 05.11 onayladı: ayak kilidi, sahte çift basış, ayakkabı ucu; tek ayak "gelişebilir". Sonra T-124, T-125 (`9eb8381e`) ve fren (`stopBrakeForce` 12, kullanıcı: en hızlıdan 1.5 sn) geçti, henüz cevap yok. `T-120-sesler` başka oturumda, ayrı worktree'de.
- **Kilit:** `agent-kit.config.local.psd1`'de 4 yol yorumda (`Scripts/Physical`, `Physical`, `Prefabs/Character`, `SideFollowCamera.cs`). Kalan 8 satırı kaldırmayı izin sistemi reddetti, gerekmedi.
- **Yapılan:** koşu pozu sürekli, bacak tek döngü eğrisi, ayak kilidi, uçuş, kalça hız sınırı, burgu ve kol pompası, hafif çapraz kamera (`sideYaw` 12), iki tuş tek itiş, tek tuş hız uçurumu kapalı.
- **Sonuç (form ölçümü, önce → sonra, en iyi koşu):** koşu pozu 0.38 → 1.00, geçişte diz 111° → 43°, eğim 7.5° → 19°, kalça burgusu 0 → 18°, kol ileri +33 (çene) → +45, iki tuşta en alçak diz 0.04 → 0.47 m.

## Sıradaki iş
1. Kullanıcı `IKTest`'e baksın; yanıta göre. Açık (seri kareden): tek ayakta kalkış kalçada ~10 cm/kare sivri, gövde ~30° geride ve denge ayağı alçak; dalışta kollar yukarıda kalıyor, önce çift zıplıyor sonra havada yatıyor, kalkışta baş ~20° sıçrıyor; koşudan çift zıplamaya ilk basışta ~0.25 sn gecikme. Tuşsuz 1.5 sn süzülmede ayak yerde kayar, tutmada öndeki ayak çivili değil. Hareket oturunca `PlayerController.md`.
2. Prefab'ta kayıtlı, kod varsayılanı işlemez: `pushEnd` −42/150, `recover` −52/60, `mid` 15/44, `frontHomeTime` 0.3, `liftKick` 1.5, `armBack` −40, sim `stopDelay` 0.6, `stopRamp` 0.3. Eski `stopBrake: 3.5` satırı artık okunmuyor. Hop ve kol alanları prefab'ta yok. Prefab Editor'den yeniden kaydedilirse T-114..T-125 kod varsayılanları dosyaya yazılır ve bundan sonra koddan değişmez.
3. Tam takım T-117'den beri koşmadı; `RunnerSimRelationTests` hop bias testleri yeni `repeat` kuralıyla koşmadı. Yeni `toeTipLength` son kemik pozunda çalışıyor, görülmedi.
3. Kullanıcı görsün: ezmede 100 m 22+ sn (hedef "en az 13" varsayımdı); tuş 0.04'te 30°/60° ~1.5 sn geç, 0.08-0.11 tuş 0.07'de ~1 sn hızlı; basılı tutmada koşudan baş ilk saniye ~6° geride.
4. Tuş süresi (`simprobe cliff`, 05.11): ritim 0.28'de tuş 0.13 sn = 11.1 sn, 0.17 sn = 13.1 sn, 0.20 sn = 29.5 sn. Uçurum büyük ölçüde kapalı; dokunulmadı.
5. Video: `D:\Pundun\form-probe\ref\` kareler; `Downloads/Screen_Recording_20261104_174949_Speed Stars.mp4`. Davranış anlamak için, kare kare kopya yok.
6. İş bitince `PlayerController.md` (mekanik rehberi) yeni sisteme göre güncellenir; `CLAUDE.md` Malik'in, kilitli.

## Ölçüm (MCP bu oturumlarda yoktu)
- `scripts/runner-probe.ps1 -Kaynak D:\Pundun\speed-starts-ik -Etiket x -Sahip T-xxx -Filtre 'RunnerFormProbeTests|RunnerSimRelationTests'` → kopyada grafik açık koşar, çıktı `D:\Pundun\form-probe\x\` (CSV + 12 karelik sheet + oyun karesi). `before` = işten önceki hâl.
- `python -I scripts/runner-olcum/final_table.py <a> <b>`: yan yana tablo. `scripts/runner-olcum/simprobe.csproj`: `RunnerSim`'i Unity'siz koşar (worktree dosyasını derler); `dotnet build -c Release`, sonra exe'yi `spam`, `scan`, `over`, `human`, `hold`, `trace`, `grid`, `cliff`, `phased`, `start` ya da argümansız çalıştır.

## Verilen kararlar
- Kullanıcı 03.11: "referans oyun ne ise aynısı". Ekonomi: yarış Nakit'i bitirme +10, zorluk bonusu 10 × oyuncu zorluğu, rakip bonusu round(10 × geçilen/toplam); rakipsiz yarış Nakit ve Mor Mücevher vermez. Giyim 500 / kafa 1.000 Nakit, emote 3×2.500 / 3×5.000 / 3×7.500, reklam +200 (günde 3), Süreli Bilet 300, Adet Bileti Nakit'le satılmaz. 100m / Race of the Day / Practice biletsiz. Tema: Egypt 2.500, Rome 5.000, Pirate Sea 7.500 Nakit, Sky Islands 50 Mor Mücevher.
- Gerçek gelir / reklam SDK'sı / IAP şimdilik YOK (Çin müşterisi). TMP + American Captain her yerde. Prefab'lar elle düzenleniyor: builder var olanı ezmez (`-forceOverwriteHandEdits` kullanma).
- Installer bağlamaları: kalıcı onay. Teslim belgesi, telefonda APK, ana menü hızı: kullanıcı "geç" dedi. Günlük build Mono/ARMv7, mağaza IL2CPP.
- Karakter işinde seçilen sayılar kod varsayılanı, prefab'a yazılmadı: `sideYaw` 12, `togetherPush` 0, `hopFullGap` 0.25, `pressFullGap` 0.15, `tapFloorSpeed` 3.2, `tapFloorGain` 0.6, `goodKickShare`/`wasteKickShare` 0.03, `strainGain` 0.065, `strainHead` 1.3, `strainWasteHalf` 0.23, `strainWastePower` 8, `strainReliefFormPower` 5 (T-114); `hopRise` 0.09, `hopPushRise` 0.04, `hopHipRiseMax` 3, `hipRiseMax` 1.75, `hopContact` 10/165 (T-115, T-116).

## Kullanıcıdan / başkalarından bekleyen
- Malik: IK işini bilmeli; UI üstündeki dokunma; engel teması; 4×100m ve Medley; dev bitiş tuşu (F) replay'i bozuyor (T-094).
- Serhat: 4abc876a `BridgeCanalRiverside.prefab`; `PastelBuildingRow` sahne açılınca bina üretiyor. Mekanikten bağımsız kırmızılar `_backlog.md`'de (Home kartı builder testleri, BuilderGuard, `ReferenceUiReviewTests`).
- Uygulama/paket adı ("Speed Stars" referans oyunun da adı, mağazadan önce değişmeli), keystore, Privacy URL: sonra.

## Gözle bakılacaklar (Editor'de Play)
- `IKTest`: ritim, yavaş, spam, iki tuş, tek tuş, basılı tutma, kamera. Telefonda denenmedi; yalnız şehir teması görüldü.
- Önceki: American Captain, bitiş sekansı, tutorial, kulvarlar, 200m/400m/engelli, emote, Practice, High Scores → DAILY, Watch Replay.

## Dikkat
- PowerShell'de `git` bozuk (system32 gölgesi): git Bash'ten. Kancada `git commit` + "main" aynı komutta olmaz.

<!-- OTOMATIK:BASLA -->
_tur-sonu.ps1 2026-11-05 11:08 - otomatik blok, elle yazilan bu blokta kaybolur._

- **KAPI TEMIZ (cikis 0)**
- Acik needs: 3 (DECISION 3)
- Review kuyrugu: 1
- Goz bekleyen (merged): 107 - T-114, T-115, T-113, T-111, T-109, T-110, T-112, T-108
- in-progress: 0 / blocked: 0
- Uyari: govde-needs 0, test 4, analytics 0 (kapsam: status in-progress/review/merged)
- GIT: 4 calisma agaci (ana: IKTest); ana agacta commit'siz .prefab/.unity/.asset: 1
- KOPYALAR: 2 (kopya.ps1 -Liste -Json)
<!-- OTOMATIK:BITIR -->
