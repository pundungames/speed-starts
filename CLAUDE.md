# Speed Stars (speed-starts) — Proje Devir Notu

> Bu dosya Claude Code tarafından her oturumda otomatik okunur. Projeye yeni bir oturumla devam ederken
> bağlam buradan gelir. Son güncelleme: 2026-10-24.
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
**Tools → Speed Stars → Set Up PhysicalTest Scene**. Eski prosedürel sistem `Scripts/Runner` +
`TestScene.unity`'de duruyor ama kullanılmıyor; oradan sadece `RunnerInput` ve `SideFollowCamera` kullanılır.

Akış: **RunnerInput → RunnerBody (+RunnerSim: fizik) → RunnerPoseDriver (sadece görsel, IK)**.
Görsel taraf hızı/zamanlamayı asla değiştirmez.

| Dosya | Görev |
|---|---|
| `RunnerSim.cs` | Saf C# fizik: hız (impulse + sürtünme), bacak `ext`/`hold` yayları, eğim yayı (sadece oyun mantığı: sendeleme), zamanlama için görünmeyen uçuş, enerji. Basış verimi fiziksel durumdan (havada = erken, geç, bacak hâlâ geride). İki tuşa özel kural **yok**: diğer ayaktan hemen sonra gelen basış (`togetherWindow`) erken sayılmaz; tutma itişi ayak başına. |
| `RunnerBody.cs` | Girdiyi zaman damgasıyla sim'e verir, kökü taşır; `Tapped`/`RunReset` olayları. |
| `FootCycle.cs` | Ayak durum makinesi (Step/Reach/Hold/FrontWait/Return/Ready). Süre = yol ÷ hız. |
| `RunnerPoseDriver.cs` | Görsel: pozlar (Ready/Run/Stumble), adım döngüsü, bel yayı, tutma, yere iniş, gövde yüksekliği, kollar, baş. |
| `RunnerPose*.cs` | Poz verisi ve varsayılanlar (Ready, Run, Stumble). Tutma/dalış pozu **yok**. |
| `RaceController.cs` | 100 m; bitişte süreyi kaydedip direkt başa döner (R tuşu yok). |
| `RunnerReplay.cs` / `ReferenceSequences.cs` | Videolardan ölçülen basış dizileri, 1–4 tuşları. |
| `RunnerBodyHUD.cs` | Ayar paneli (H gizler). `Editor/PhysicalSetup.cs` kurulum aracı. |

Rig (Control Rig): `Hip target` (MultiParent → `B-hips`), `Spine Root/Tip Target` (TwistChain
`B-spine → B-neck`), `Left/Right Leg Target + Hint` (TwoBoneIK), `Hand Rig` (play'de kapatılır),
`Head Aim Rig` (MultiAim → `B-head`, hedef `Head LookAt Target`). `Step Points/Left|Right` altında
`Front`, `Back`, `Rest`. Kemikler isimle bulunur (`B-shin.L`, `B-toe.R`, `B-upperArm.L` …).

### Oturmuş kurallar (kullanıcının onayladığı davranışlar)

- **Bekleme duruşu:** Karakter hep **Ready çömelmesine** yönelir. Ayaklar `Rest` noktalarında yere basar;
  Rest yükseldikçe çömelme derinleşir. Koşu duruşu (ev = elipsin tabanı, Run pozu) sadece basışlar sürerken
  (`stanceKeepTime`); girdi kesilince doğrudan çömelmeye, arada dik duruş yok.
- **Adım döngüsü:** Front–Back arası elips; alt yay zemine tek noktada (taban) değer. Ayaklar **eşli ve aynı
  hızda**: basılan geriye, diğeri öne; tuş yokken ikisi aynı anda eve döner, yakın olan önce varır.
- **Gövde yüksekliği ayaklardan:** Yerdeki ayak gövdeyi taşır; kalkışta itiş gövdeyi atar. Ayrı zıplama yok.
- **Bel:** Kendi yayı (`UpdateWaist`): ayaklara göre ortalanır + basılı her ayak `holdLean` öne. Öne eğim
  **belden** (kalça ayakların üstünde kalır); geriye yaylanma (bırakınca) ayaklar etrafında.
- **Tek ayak tutma:** Tutulan ayak Back'te, diğeri Front'ta yere iner ve taşır; bel yavaşça destek ayağına
  kayar, arka bacak kalça etrafında yataya kadar kalkar (tahterevalli).
- **Baş ve boyun (`UpdateHead`):** Tek bir baş açısı (geride ↔ dik ↔ önde). Hızla **aynı kaynak, ters
  grafik**: basışın sonucu/verimi (`LastResult`, `LastEfficiency`). Tam zamanında = baş dik kalır; geç = biraz
  geride kalır (`lateRecover`); erken / boşa basış = `earlyKick × (1 − verim)` öne iter. Sık basınca itmeler
  birikir (`headRecover` yetişmez). Basışlar arası (adım indikten sonra) baş geriye düşer. Her basışta küçük
  bir sallanma (`headNod`). Tutulan ayak başı öne iter (`holdHead`; tek ayak `oneFootMax` tavanlı, iki ayak
  tavansız). Boyun `neckMax`'a kadar alır, aşan kısım beli çeker (gövde başı takip eder).
- **Hazır yatma pozu yok** (kullanıcı reddetti: tahta gibi yatış, dümdüz bacaklar). Çok sık basmada gövde
  alçalır ama bacaklar adım döngüsünde koşmaya devam eder. İki ayak geride tutulunca hiçbir ayak gövdeyi
  taşımaz: gövde yerçekimiyle düşer, yerde durur (`pelvisClearance`); tutulan bacaklar düzleştirilmez.
- **Kollar:** El IK kapalı. **Karşı ayağa tam kilitli** (sağ kol ↔ sol ayak, sol kol ↔ sağ ayak), yay/gecikme
  yok; tutmada ve yerdeyken de (ayak geride → kol geride). Kol geriye giderken dirsek açılır (`elbowOpenBack`).
- **İki tuşa özel durum yok** (fizikte de görselde de): baş, kol, gövde basış sıklığından ve basılı kalmadan
  kendiliğinden oluşur. Hızlı koşuda basışlar 0.05–0.08 sn arayla gelebilir (`togetherWindow`, erken sayılmaz).
- **Baş hızlı koşuda gövdeyle aynı çizgide** (Run pozu `headDown` = 1), dururken/yavaşken ileri bakar.
- Gövde ağırlıkla X ekseninde döner. (Pelvis/göğüs Y/Z dönüşü ve el IK denendi, kullanıcı geri aldırdı.)

## Şu anki konu: BAŞ ve BOYUN (2026-10-24, ilk sürüm yazıldı, göz ayarı bekliyor)

- Baş açısı `Head LookAt Target` konumlandırılarak verilir (Head Aim weight hep 1). Gövde `neckMax`'a kadar
  eğikken baş ileri bakar, ötesinde gövdeyle iner; baş açısı bunun üstüne eklenir. Boyun (Spine Tip) baş
  açısının yarısını alır (`NeckShare`).
- Kaldırılanlar: `headLead`, `headDropSpring`, `headFollowLean`, yere iniş tetiği ve bütün `lie*` yatma pozu
  alanları (`lieRange`, `lieLean`, `lieHipHeight`, `lieSpring`, `lieLegReach` …), `diveLift`, `holdArm`, `armSpring`.
- Sıradaki: kullanıcının play'de denemesi, `1–4` dizileriyle değer ayarı. HUD'a baş açısı çizgisi
  eklenmesi önerildi, kullanıcı henüz cevaplamadı.

## Durum

- Bütün yeni sistem `main` çalışma klasöründe; **son değişiklikler commit edilmemiş** (kullanıcı kendisi
  commit eder). Eski worktree `.claude/worktrees/physics-based-speed-stars-56d8b3` geride kaldı; ana
  klasörde (`speed-starts`) çalış.
- `Physical/Poses/LungeLeft|LungeRight|Dive.asset` artık kullanılmıyor (silmek kullanıcıya bırakıldı).
- `ProjectVersion.txt` ekipte 62f2/62f3 arasında gidip geliyor; ekipçe karar verilmeli.
