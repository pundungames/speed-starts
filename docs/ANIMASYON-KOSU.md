# Koşu animasyonu: animasyoncu için not

**Şikâyet:** Karakter koşarken robot gibi duruyor, akıcı değil.
**İstenen:** Daha canlı ve akıcı bir koşu. Oyunun hızı, zamanlaması ve basış hissi değişmeden.

Bu not koda ve prefab değerlerine bakılarak yazıldı (2 Kasım 2026). Bölüm 3'teki sebepler koddan çıkan adaylardır; hareket hâlâ gözle değerlendirilmeli.

## 1. Sistem nasıl çalışıyor

**Animasyon klibi yok.** Koşu, her karede kodla hesaplanan açılardan ve Animation Rigging IK'sından oluşuyor.

```text
N/M basışı → RunnerSim (hız, ritim) → RunnerPoseDriver (faz → açı → IK) → kemikler
```

- **Oyuncu her adımı kendisi başlatır.** Bacak hareketi önceden bilinmez, bir sonraki basış hareketi yarıda kesebilir. Baştan sona oynayan klasik bir koşu döngüsü bu yüzden doğrudan kullanılamaz.
- **Görsel taraf hızı ya da zamanlamayı değiştirmez.** Bu kural korunmalı.

| Dosya | Görev |
|---|---|
| `Assets/Speed-Staes/Scripts/Physical/RunnerPoseDriver.cs` | Bütün görsel hareket; asıl çalışılacak dosya |
| `Assets/Speed-Staes/Scripts/Physical/RunnerSim.cs` | Hız ve ritim; **görsel iş için değiştirilmemeli** |
| `Assets/Speed-Staes/Prefabs/Character/HumanCharacterDummy_F.prefab` | Rig ve kayıtlı değerler |
| `Assets/Speed-Staes/Physical/Poses/` | `Ready`, `Run`, `Stumble` pozları |
| `Assets/Speed-Staes/Scripts/Physical/PlayerController.md` | Ayrıntılı rehber (bazı değerleri eski olabilir, güncel olan prefab'dır) |

**Rig:** `Hip target` → `B-hips`; `Spine Root/Tip Target` → `B-spine`, `B-chest`, `B-neck`; `Left/Right Leg Target + Hint` (TwoBoneIK); `Head Aim Rig` → `B-head`. Kollar IK ile değil, doğrudan kemik açısıyla sürülüyor.

## 2. Nasıl denenir

1. `Assets/Speed-Staes/Scenes/Test/PhysicalTest.unity` sahnesini aç, Play'e bas.
2. **N** sol ayak, **M** sağ ayak. Saniyede yaklaşık 4 kez, yani 0.24-0.30 sn arayla sırayla basınca ritim oturur.
3. **H** ayar panelini gizler. **1-4** kayıtlı basış dizilerini oynatır, **Esc** durdurur; aynı hareketi tekrar tekrar izlemek için kullanışlı. Bu diziler eski ritimle kaydedildi, yalnız hareket izlemek için kullan.
4. Game penceresine tıkla ki tuşlar oyuna gitsin.

Değerler Play sırasında Inspector'dan canlı değişir, ama Play bitince geri döner. Beğendiğin değeri not al ya da prefab'a yaz.

Poz düzenlemek için: `RunnerPoseDriver` → `preview` aç, `previewPose` seç.

## 3. Robot görünümün olası sebepleri

Etkisi en büyük olduğunu düşündüğüm sırayla.

| # | Koddaki durum | Görünen etkisi | Yer |
|---|---|---|---|
| 1 | Gövde yalnız X ekseninde dönüyor; Y ve Z bilerek 0 | Kalça ve omuz karşı dönüşü yok, yana yük aktarımı yok; karakter "düz bir levha" gibi | `RunnerPoseDriver`, sınıf özeti |
| 2 | Bacaklar tek düzlemde (atlayış hariç) | Dizler ve ayaklar ray üstünde gidiyor gibi | `LegFK` |
| 3 | Fazlar durağan başlayıp durağan bitiyor (`SmoothStep`) | Her basışta bir an duraklama, tek tek pozlara gidiş hissi | `UpdateLeg` |
| 4 | Kol yalnız bacağın geriye gidişini izliyor, gecikme yok ("no lag"), bacak öne giderken kol dinleniyor | Kollar mekanik, sallanma ve takip hareketi yok | `StepArm`, sınıf özeti |
| 5 | Ayak yere kilitli değil (foot lock yok); kök sim hızıyla gidiyor, bacak açıyla | Yerdeki ayak kayıyor olabilir, itiş ağırlıksız görünür | `UpdateBodyHeight`, `LegFK` |
| 6 | Sim'deki uçuş evresi görsele yansımıyor; gövde yüksekliği yalnız bacak açısından geliyor | Sprintteki sıçrama ve iniş ağırlığı zayıf | `UpdateBodyHeight` |
| 7 | Baş ve boyun yalnız öne-arkaya hareket ediyor | Baş donuk | `WriteHead` |

## 4. Kod yazmadan oynanabilecekler

`RunnerPoseDriver` Inspector'ı. Tüm alanların açıklaması (Tooltip) var.

**Bacak açıları** (uyluk: düz aşağıdan derece, + öne; diz: 180 düz). Şu anki prefab değerleri:

| Alan | Uyluk | Diz | Anlamı |
|---|---|---|---|
| `runHome` | 10 | 160 | Koşu sırasında dinlenme |
| `pushEnd` | -42 | 150 | İtiş sonu |
| `recover` | -52 | 60 | Geride, kaval yukarı katlanmış |
| `mid` | 15 | 44 | Öne gelirken |
| `reach` | 47 | 175 | Önde, ayak yere inerken |

**Zamanlama**

| Alan | Değer | Anlamı |
|---|---|---|
| `pushTime` | 0.2 | İtiş ve diğer bacağın öne uzanması |
| `recoverTime` / `midTime` / `homeTime` | 0.15 / 0.15 / 0.15 | Salınımın payları |
| `frontHomeTime` | 0.3 | Öndeki bacağın yerine dönüşü |
| `stepSlow` | 2.667 | Salınım, basış aralığının kaç katı sürer |
| `stepFollow` | 0.5 | Ritim değişimine uyum hızı |
| `animSpeedUp` | 0.5 | En yüksek hızda sürelerin payı |

**Diğerleri**
- **Gövde yüksekliği:** `heightSpring` (8 / 0.8), `dropSpring` (3 / 0.6)
- **Ayak ve parmak:** `accelProfile`, `maxSpeedProfile` eğrileri
- **Kol:** `armBack` (-40), `elbowBack` (30), `armOutward` (8), `shoulderFollow` (12)
- **Gövde eğimi:** `hipShare` (0.65), `waistBaseShare` (0.27), `waistTopShare` (0.67), `waistSpring`
- **Pozlar:** `Physical/Poses/Run.asset`

Sıra önerisi: Önce kol ve ayak eğrileri, sonra `reach`/`recover` açıları. Kısa bir ayar turunda ne kadar düzeldiğine bakmak, kod işine girmeden önce faydalı olur.

## 5. Kod isteyebilecek iyileştirmeler

Gerekirse birlikte kararlaştıralım.

1. **Kalça ve omuz dönüşü:** Adıma bağlı Y dönüşü ve hafif Z yatması, omuz ters yönde. Muhtemelen en çok fark yaratacak iş.
2. **Faz geçişlerinde hız sürekliliği:** Yeni faz durağan başlamasın, bacağın mevcut hızından devam etsin. `ReachOn` bir örneği.
3. **Kol ve baş için takip:** Gecikme ve yay (`Spring`). Gövdedeki eğim zinciri aynı yöntemi kullanıyor.
4. **Ayak kilidi:** Yerdeki ayak dünyada sabit kalsın.
5. **Görünür uçuş:** Sim'deki uçuşun küçük bir kısmı gövde yüksekliğine yansısın.
6. **Klip katmanı:** Üst gövde ya da küçük ikincil hareketler için additive klip. Bacaklar basışa bağlı kalmak zorunda.

## 6. Değişmemesi gerekenler

Bunlar proje sahibinin onayladığı davranışlar:

- **Basış anı:** Tuşa basınca bacak hemen tepki vermeli; gecikme oyunu bozar.
- **Sim'e dokunma:** `RunnerSim.cs`, `stepTime`, `pushGain`, `maxSpeed`. Bot tablosu bu sayılara göre ölçüldü, değişirse botlar da yeniden kalibre edilir.
- **Özel durum yok:** "Koşuyorsa şunu yap" gibi modlar eklenmesin. Hareket girdiden kendiliğinden çıkmalı. Tek ayak tutma ve iki parmak dalışı aynı sistemden çıkıyor, onlar da bozulmamalı (**N** veya **M**'yi basılı tut).
- **Bekleme duruşu:** Tuş yoksa koşu duruşu değil, `crouch` çömelmesi.
- **Engelli atlayış:** `jumpLead*`, `jumpTrailPath` ve hava süresi videoya göre ayarlandı. Atlama tuşu **K**; `Hurdles` nesnesi olan sahnede engeller çıkar.

## 7. Bitirince

- Oyun hızı ve süre değişmemiş olmalı. İyi ritimde 100 m ~10-11 sn; yaklaşık değer.
- Prefab farkında yalnız istenen satırlar olmalı: `git diff`.
- Sahne ve prefab değişikliklerini kimin commit edeceğini Malik'le konuş.

**Referans:** Orijinal oyunun kayıtları Malik'te. Kare kare inceleme için onlardan iste.
