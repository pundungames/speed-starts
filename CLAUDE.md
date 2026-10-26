# Speed Stars (speed-starts) — Proje Devir Notu

> Bu dosya Claude Code tarafından her oturumda otomatik okunur. Projeye yeni bir oturumla devam ederken
> bağlam buradan gelir. Son güncelleme: 2026-10-26.
> **Ayrıntılı rehber:** `Assets/Speed-Staes/Scripts/Physical/PlayerController.md` — oturumun başında oku.

## Proje nedir

Mobil, yandan görünümlü bir **sprint (100 m) koşu oyunu prototipi** (örnek: orijinal "Speed Stars").
Oyuncu iki tuşla (←/→ veya ekranın sol/sağ yarısı) koşucunun **her bir ayağını ayrı** kontrol eder.
Animasyon klip ile değil, **prosedürel**: Animation Rigging IK hedefleri koddan yazılır.

- Unity **2022.3.62** (Built-in RP). Paketler: Animation Rigging 1.2.1, Input System 1.19.0 (Active Input
  Handling = Both), Device Simulator, **MCP for Unity** (Claude editörü buradan kontrol eder),
  postprocessing, recorder, shadergraph.
- Git: `https://github.com/pundungames/speed-starts.git`, branch `main`. Ekipte başka biri de (Serhat) çalışıyor.
- Kullanıcı Türkçe konuşur; HUD metinleri Türkçe, kod ve yorumlar İngilizce (kısa, "neden"i anlatan
  `<summary>` blokları; mevcut stile uy).

## Kullanıcıyla çalışma kuralları (önemli)

- **Git'e izinsiz dokunma:** commit, push, pull, merge, stash, dosya silme — hiçbiri kullanıcı açıkça
  istemeden yapılmaz. Git hakkında soru sorarsa sadece bilgi ver.
- **Önce anla, sonra yap:** Kullanıcı tarif ederken sık sık "ne anladın" diye sorar. Bu durumda **kod
  yazma**; anladığını kısa ve net anlat, onay bekle. "İşlem yapma" dediyse kesinlikle değişiklik yapma.
- **Play mode testi:** Varsayılan olarak kullanıcı kendisi test eder. "Test et" derse MCP ile play mode'da
  test et: `Application.runInBackground = true` (Unity arka plandayken kare ilerlemez), olayları
  `RunnerInput.Inject` ile ver, `EditorApplication.isPaused` ile durdur, `manage_camera` ile Main Camera'dan
  ekran görüntüsü al (`output_folder: Temp/...`, iş bitince sil).
- **Sahne:** Kullanıcının kaydedilmemiş değişiklikleri olabilir; sahne dirty ise kaydetme. Koda yeni
  alan eklenince sahnedeki örnek eski/varsayılan değeri tutabilir (Inspector değeri kodu ezer) — kontrol et.
- **Referans videolar:** Kullanıcı orijinal oyundan video/ekran görüntüsü atar. Videoyu tarayıcıda kare kare
  incele (Range destekli yerel bir sunucu + canvas'a kare ızgarası). Gördüğünü tablo hâlinde anlat.
- Değişiklikten sonra `read_console` ile derleme hatası kontrol et. Rehberi (`PlayerController.md`) güncel tut.

## Sistem: yay tabanlı koşucu (`Assets/Speed-Staes/Scripts/Physical`, namespace `SpeedStars.Physical`)

Sahne: **`Scenes/Test/PhysicalTest.unity`** (koşucu: `HumanCharacterDummy_F`). Yeniden kurulum:
**Tools → Speed Stars → Set Up PhysicalTest Scene**. Eski prosedürel sistem ve `TestScene.unity` **silindi**
(2026-10-26); `Scripts/Runner`'da yeni sistemin kullandığı `RunnerInput` ve `SideFollowCamera` ile Serhat'ın dünya kodu kaldı.

Akış: **RunnerInput → RunnerBody (+RunnerSim: fizik) → RunnerPoseDriver (sadece görsel, IK)**.
Görsel taraf hızı/zamanlamayı asla değiştirmez.

| Dosya | Görev |
|---|---|
| `RunnerSim.cs` | Saf C# fizik: hız (impulse + sürtünme), bacak `ext`/`hold` yayları, eğim yayı (sadece oyun mantığı: sendeleme), zamanlama için görünmeyen uçuş, enerji. Basış verimi fiziksel durumdan (havada = erken, geç, bacak hâlâ geride). İki tuşa özel kural **yok**: diğer ayaktan hemen sonra gelen basış (`togetherWindow`) erken sayılmaz; tutma itişi ayak başına. |
| `RunnerBody.cs` | Girdiyi zaman damgasıyla sim'e verir, kökü taşır; `Tapped`/`RunReset` olayları. |
| `RunnerPoseDriver.cs` | Görsel: pozlar (Ready/Run/Stumble), **bacak açıları** (faz → uyluk/diz açısı → FK → IK), bel yayı, tutma, gövde yüksekliği, kollar, baş. |
| `RunnerPose*.cs` | Poz verisi ve varsayılanlar (Ready, Run, Stumble). Tutma/dalış pozu **yok**. |
| `RaceController.cs` | 100 m; bitişte süreyi kaydedip direkt başa döner (R tuşu yok). |
| `RunnerReplay.cs` / `ReferenceSequences.cs` | Videolardan ölçülen basış dizileri, 1–4 tuşları. |
| `RunnerBodyHUD.cs` | Ayar paneli (H gizler). `Editor/PhysicalSetup.cs` kurulum aracı. |

Rig (Control Rig): `Hip target` (MultiParent → `B-hips`), `Spine Root/Tip Target` (TwistChain
`B-spine → B-neck`), `Left/Right Leg Target + Hint` (TwoBoneIK), `Hand Rig` (play'de kapatılır),
`Head Aim Rig` (MultiAim → `B-head`, hedef `Head LookAt Target`). `Step Points` prefab'tan silindi. Kemikler isimle bulunur (`B-shin.L`, `B-toe.R`, `B-upperArm.L` …).

### Oturmuş kurallar (kullanıcının onayladığı davranışlar)

- **Bacaklar açıyla (2026-10-25, videodan):** Hareket **uyluktan başlar**; alt bacağa hareket yönüne göre açı
  verilir, ayak nereye düşerse adım odur. Ayak yolu / elips / Step Points **yok**. Her bacak: uyluk açısı
  (dikeye göre, + = önde) + diz açısı (180 = düz) → FK → IK (target = bilek, hint = dizin önü). Fazlar:
  basılan bacak **Push** (uyluk geriye, diz açılır) → tuş basılıysa **Hold**, değilse **Recover** (uyluk geride
  sabit, alt bacak katlanır) → kendiliğinden **Mid** (orta açı) → **Rest**. Diğer bacak aynı anda **Reach**
  (uyluk önde sabit, sadece alt bacak öne açılır, ayak önde yere iner) → **Front** (yerde bekler) → diğeri
  itmiyor/tutulmuyorsa Rest. Her faz **o anki açıdan** başlar: ortada gelen basış kaldığı yerden geriye devam eder.
- **Bekleme duruşu:** Karakter hep **Ready çömelmesine** yönelir: `crouch` açıları (uyluk önde, diz bükük) —
  bir açı pozu, konum değil. Koşu duruşu (ev = `runHome`, Run pozu) sadece basışlar sürerken
  (`stanceKeepTime`); girdi kesilince doğrudan çömelmeye, arada dik duruş yok.
- **Gövde yüksekliği açılardan:** Kalça, en alttaki ayak tam zemine değecek yükseklikte. Açı genişleyince bacak
  uzar → gövde kalkar; kapanınca iner. Ayak zemine girmez, hafif kayma kabul. Ayrı zıplama yok.
- **Zincir baş → boyun → kalça (2026-10-26):** Bel **sabit**: `B-spine` X = `waistAngle` (6.5), hiç bükülmez.
  Bütün eğim (tutma `holdLean`, başın çekişi, `speedLean`, dalış) **kalçadan**: kalça açısı = pozun `pelvisPitch`
  + eğim, `pelvisPitchRange` −10 … 90 (bu bizim açımız; Inspector'daki `Hip target` X'e kemiğin dinlenme açısı
  4.5 eklenir). Ready pozu kalça −10 (geride). Kalçanın yeri bacaklardan gelir, sadece açısı değişir.
  **Bacaklar kalçaya göre**: havadaki ve tutulan bacaklar kalçanın *eğim* kısmıyla döner (pozun sabit
  eğimiyle değil); yere basan bacak (Rest, Front/destek, taşıyan Push) yere göre kalır (`FollowHips`).
  Tutulan bacağın kalkması (eski `SeeSaw`) artık kalçayla kendiliğinden; en fazla `heldLegMaxAngle` = 79 (yataya 11° kala), diz `holdBack` 159 (hafif bükük, dümdüz değil) — kullanıcının play'de ayarladığı sağ bacak pozundan ölçüldü (2026-10-26), iki bacak için aynı.
  `pelvisWaistShare` ve pozdaki `trunkPitch`/`armSwing` kaldırıldı.
- **Tek ayak tutma:** Tutulan bacak `holdBack` (geride, düz); öndeki bacak tutma yayıyla `holdSupport`'a geçer
  (alt bacak dik, diz ~100°) ve taşır; kalça ayağın arkasında-üstünde, üstüne çökmez. Gövde 1–2 sn'de akarak
  ön bacağın üstüne katlanır (`holdLean` 85°/ayak, kalçadan); arka bacak kalçayla birlikte yataya kalkar
  (`heldLegMaxAngle`). Referans: ikinci video, 1.4–3.4 sn.
- **Baş ve boyun (`UpdateHead`):** Tek bir baş açısı (geride ↔ dik ↔ önde). Hızla **aynı kaynak, ters
  grafik**: basışın verimi (`LastEfficiency`), **eğri, eşik yok**. İyi basış dikleştirir ve öndeki başı
  `pressReturn × verim` kadar geri alır; boşa basış `earlyKick × (1 − verim)` öne iter. Öndeki baş **her an**
  geri döner (`headReturn`, basış olsa da olmasa da, bekleme yok); önde sadece boşa basışlar dönüşten hızlı
  geldikçe kalır. Bel başı izler, yayıyla (`waistSpring`) biraz geriye taşıp oturur. Boşa basış beli öne
  iter (`stepKick × (1 − verim)`), doğru basış itmez. Tuş basılı değilken baş geriye düşer. Her basışta küçük
  bir sallanma (`headNod`). Tutulan ayak başı öne iter (`holdHead`; tek ayak `oneFootMax` tavanlı, iki ayak
  tavansız). Boyun `neckMax`'a kadar alır, aşan kısım beli çeker (gövde başı takip eder).
- **Hazır yatma pozu yok** (kullanıcı reddetti: tahta gibi yatış, dümdüz bacaklar). Çok sık basmada gövde
  alçalır ama bacaklar fazlarında koşmaya devam eder.
- **İki ayak tutma = dalış, karakter yere iner** (2026-10-26, kullanıcı düzeltti; önceki "düşme yok" yanlıştı;
  referans `Downloads/screen-20261026-000711.mp4`: 6.28–6.60, 10.28–10.56, 18.02–18.62). Kalça dikey düşmez:
  1) **Havada:** tutulan bacak taşımaz; diğeri tutulurken basılan bacak da taşımaz (yerden kalkıp düz geriye
     savrulur, gövde yükselmez). Gövde o anki yükseklik/hızıyla yerçekimiyle iner. Çömelmeden iki tuş birden
     basılırsa iki bacak önce birlikte iter.
  2) **Ayaklar yere yapışmaz** (kullanıcı reddetti: ayak ucu etrafında dönme/`DiveLeg`/`diveBend`/`headTip`
     kaldırıldı). İki bacak tek ayaktaki gibi geride, açılı, havada (kalçayla döner). Gövde düşerken öne yatar
     (`diveLean`'a doğru; zamanla ve düşülen mesafeyle, kalça en fazla 90), kafa önce iner. Düşüş hıza bağlı
     (`diveTime` 0.3 → 0.5 sn, `diveSpeed`'e kadar; yerçekimi ölçeklenir).
  3) **Kafa yere değince durur** (`headRest`); kalça yere değmez. Güvenlik: kalça `MinPelvis` altına inmez.
  `pelvisClearance` kaldırıldı. Bırakınca eğim yayla (`diveRecover`) geri gelir, bacaklar Mid → Rest.
- **Kollar:** El IK kapalı. **Bir tuş = bir bacak + bir kol** (sağ tuş: sağ bacak + sol kol). Kol, kendi tuşunun
  bacağının **uyluk açısının** sadece evin gerisine gidişini izler (basış, tutma, eve dönüş); diğer tuşun öne
  uzattığı bacak kolu oynatmaz. Yay/gecikme yok; tutmada ayak geride → kol geride. Önkol üst kolla zincir gibi geriye
  döner. Tam geride kol **sabit** açıda (`armBack`/`elbowBack`): hız ve poz karışımı bunu
  değiştirmez (önceden Ready pozu tutarken kolu öne itiyordu).
- **İki tuşa özel durum yok** (fizikte de görselde de): baş, kol, gövde basış sıklığından ve basılı kalmadan
  kendiliğinden oluşur. Hızlı koşuda basışlar 0.05–0.08 sn arayla gelebilir (`togetherWindow`, erken sayılmaz).
- **Baş hızlı koşuda gövdeyle aynı çizgide** (Run pozu `headDown` = 1), dururken/yavaşken ileri bakar.
- Gövde ağırlıkla X ekseninde döner. (Pelvis/göğüs Y/Z dönüşü ve el IK denendi, kullanıcı geri aldırdı.)

## Şu anki konu: BACAKLAR AÇIYLA + KALÇA ZİNCİRİ + DALIŞ (2026-10-26: test edildi, kullanıcı elle ayar yapıyor)

- Durum: sistem kuruldu ve MCP ile test edildi; kullanıcı açıları ve diğer değerleri **Inspector'dan elle**
  ayarlıyor. Bir değeri değiştirmeden önce prefab/Inspector'daki güncel değeri oku, koddaki varsayılana güvenme
  (aşağıdaki "şimdiki değerler" ayardan önceki hâli gösterir).

- Kullanıcının videosundan (`Downloads/screen-20261025-202059.mp4`, adım 12.00–12.55 sn, çömelme 11.0–11.9,
  tutma 20.6–20.85) ölçülen başlangıç açıları `RunnerPoseDriver → Leg angles`'ta. Ayrıntı ve faz tablosu
  `PlayerController.md` bölüm 4.
- Kaldırılanlar: `FootCycle.cs`, Step Points kullanımı, elips/taban (`loopHeight`, `loopSkew`, `strideSpeed`,
  `returnSpeed`, `minMoveTime`, `kneeHintOffset`, `groundBlendTime`), `stanceCompression`, `contactSpring`,
  `bodySpring`, `liftKick`, `holdShiftSpring`, `holdKneeAngle` (yerine `holdSupport`), `maxLegReach` (yerine
  `maxKnee`), pozlardaki `left/right` (FootPose) ve `pelvis.y`.
- Kullanıcı denedi: "sistem iyi, açılar kötü". MCP ile kare kare test edildi, videoyla karşılaştırıldı:
  ön bacak yetersiz uzanıyordu ve erken dönüyordu. Front artık diğer bacak Recover'ı bitirene kadar önde kalır.
  İkinci denemede "açılar hâlâ dar": video kareleri piksel ızgarasıyla ölçüldü (12.05/12.15/12.25): ön bacak
  neredeyse düz ~47° önde, arka uyluk −30…−40°, toparlanmada diz ~100° (topuk kalça hizasında). Şimdiki değerler:
  `reach` 47/175, `pushEnd` −30/150, `recover` −40/100, `mid` 15/70. Gözle tahmin yetmiyor: açıyı videodan
  ızgarayla ölç, kalça→bilek çizgisinin açısına bak. Kemikler istenen açıyı ±5° içinde alıyor.
- Play'e girince ilk saniyelerde kendiliğinden bir basış geliyor (test sırasında görüldü, kaynağı bakılmadı).
- **Ön ayak açısı (kullanıcı istedi):** Öndeki ayak düz değil, topuk yerde parmak ucu yukarıda (`touchdownPitch`
  −17/−15, videoda ~17°); önde durdukça kalır, eve dönerken düzleşir. Topuk zemine girmesin diye bilek kalkar
  (`HeelLift`), parmaklar ayakla düz kalır (yere yatırılmaz).
- **Dikkat (değer değiştirirken):** Sürücü prefab'ta (`Prefabs/Character/HumanCharacterDummy_F.prefab`) ve prefab
  son olarak 2026-10-26'da kaydedildi (dalış, `waistAngle`, `pelvisPitchRange` dahil bütün alanlar dosyada). **Kod varsayılanını değiştirmek etkilemez**; değeri
  prefab'ta değiştir (SerializedObject + `PrefabUtility.SavePrefabAsset`, prefab kirli değilken) ya da
  kullanıcı Inspector'dan ayarlar.
- Açık noktalar: sık basmada kalça ~0.95 m'de kalıyor (videoda gövde alçalıyor); play'e girince gelen
  kendiliğinden basış; sahne testler sırasında bir noktada dirty göründü (kaydedilmedi).

## Önceki konu: BAŞ/BOYUN + İKİ PARÇALI GÖVDE (2026-10-24 akşamı bırakıldı)

- Baş açısı `Head LookAt Target` konumlandırılarak verilir (Head Aim weight hep 1). Gövde `neckMax`'a kadar
  eğikken baş ileri bakar, ötesinde gövdeyle iner; baş açısı bunun üstüne eklenir. Boyun (Spine Tip) baş
  açısının yarısını alır (`NeckShare`).
- Kaldırılanlar: `headLead`, `headDropSpring`, `headFollowLean`, yere iniş tetiği ve bütün `lie*` yatma pozu
  alanları (`lieRange`, `lieLean`, `lieHipHeight`, `lieSpring`, `lieLegReach` …), `diveLift`, `holdArm`, `armSpring`.
- Ayrıca kaldırılanlar (o zaman): yere düşme (`pelvisClearance`; yerine 2026-10-26'da dalış geldi, yukarıya bak), belin ayaklara göre ortalanması (`balanceGain`),
  geriye eğimin bütün gövdeyi ayaklar etrafında döndürmesi, `holdCrouch` (yerine `holdKneeAngle`),
  `kneeLeanFollow`, fizikte iki tuşa özel kurallar (`Both`, `bothMultiplier`, `diveLean`).
- Son yazılan ama **henüz play'de görülmemiş**: tek ayak tutmada kalça `holdKneeAngle` (95°) ile destek
  ayağının arkasında-üstünde, gövde ön bacağa katlanıyor (sahnede `holdLean` = 85, kaydedildi).
- Sıradaki: kullanıcının play'de denemesi (tek ayak tutma, iki ayak tutma, sık basma → toparlanma,
  kolların tutmada geride sabit kalması), `1–4` dizileriyle değer ayarı. HUD'a baş açısı çizgisi önerildi,
  cevaplanmadı.
- **Ders:** Kullanıcı özel durum / kural istemiyor; davranış input akışından kendiliğinden çıkmalı. Bir şey
  bozulunca yeni kural ekleme, sebebi koddan bul ve anlat. İki referans video: `Downloads/screen-20261024-105457.mp4`
  (koşu, sık basma, dalış) ve `Downloads/screen-20261024-170028.mp4` (tek ayak tutma).

## Durum

- Bütün yeni sistem `main` çalışma klasöründe; **son değişiklikler commit edilmemiş** (kullanıcı kendisi
  commit eder). Eski worktree'ler geride; Unity ana klasörü (`speed-starts`) açıyor. Worktree'den çalışan
  oturumda ana klasör Edit ile düzenlenemiyor: worktree'de düzenle, kullanıcının onayıyla ana klasöre kopyala,
  sonra `read_console`.
- **Temizlik (2026-10-26, kullanıcı istedi; yeni yapı kalıcı):** silinenler: eski prosedürel sistem
  (`RunnerGaitDriver`(+.md), `RunnerStepController`, `LegCycle`, `PushEvaluator`, `RunnerDebugHUD`, `RunnerMovement`),
  `TestScene.unity`, `Lunge*/Dive` pozları, prefab'taki Step Points, pozlardaki `trunkPitch`/`armSwing`. Serhat'ın
  `LoopWayGenerator`/`LoopWayGeneratorEditor` koşucuyu artık `RunnerBody` ile buluyor. Dokunulmayanlar:
  `RunnerContactShadow`, `LoopWay*`, `Pastel*`, `CloudVariation`, `AccessoryPhotoStudio`, `UndoRecoveryBackup.unity`
  (eski bileşenleri içeriyor, açılırsa eksik script uyarısı verir), eski worktree klasörleri. `PhysicalSetup` artık
  sahne kopyalamıyor; PhysicalTest yoksa hata döner.
- `ProjectVersion.txt` ekipte 62f2/62f3 arasında gidip geliyor; ekipçe karar verilmeli.
