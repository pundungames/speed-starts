# Kozmetik hazırlık raporu — speed-starts — 2026-10-28

**Özet.** Kozmetiğin mesh tarafı büyük ölçüde hazır. Prefab'ta `Accessories/` altında 8 kategori var. Her kategoride 7 normal ve 2 premium set bulunuyor, toplam 75 SkinnedMeshRenderer, hepsi kapalı. Hepsi aynı iskelete deri ile bağlı, yani IK'yi kendiliğinden izliyor. Renkler 3 paylaşılan toon materyalinden geliyor ve `_Color` üzerinden MaterialPropertyBlock ile değiştirilebiliyor. Serhat'ın stüdyosu set seçme ve renk atama mantığının çalışan bir örneği. Yalnız ikon üretmiyor, set başına tam boy fotoğraf çekiyor. Eksik olanlar: ikonlar, premium efekt (trail/partikül), UI, kayıt ve ekonomi. Ayrıca eski temel kıyafetin mesh'i büyük olasılıkla kayıp (şüpheli, aşağıda).

Yöntem: prefab ve sahne YAML'ı Python ile ayrıştırıldı (GameObject → Transform → Renderer, materyal GUID'leri `.meta`'dan çözüldü). FBX düğüm adları ikili dosyadan okundu, git yalnız okundu. Dosya değiştirilmedi, Unity açılmadı.

## 1. Mevcut aksesuar yuvaları

Ortak noktalar:
- Bütün öğeler `HumanCharacterDummy_F/Accessories/<Kategori>/<Öğe>` altında duruyor. Hepsi SkinnedMeshRenderer, `m_RootBone` = B-root ve 56 kemiğin tamamını kullanıyor (82/82 renderer).
- Öğeler kemiğe parent edilmemiş. İskelete deri ağırlığıyla bağlılar, yani soket değil skinning kullanıyorlar.
- Her öğenin `m_IsActive` değeri 0 (ör. G01 `prefab:12053`). Kök `Accessories` açık (`prefab:7975`).
- Mesh'ler `Art/Character/RunnerAccessoryMeshes.asset` dosyasında (74 alt mesh ve ana nesne).
- Renk kanalları: C1 = `Color1_Toon` (varsayılan gri 0.8), C2 = `Color2_Toon` (kırmızı), C3 = `Color3_Toon` (koyu gri). Kaynak: `Materials/RunnerToon/Color{1,2,3}_Toon.mat:41`.
- Hepsi `Speed Stars/Runner Toon` shader'ını kullanıyor. Doku yok (`Color1_Toon.mat:26-27` fileID 0), renk tek `_Color` özelliğinden geliyor (`Shaders/RunnerToon.shader:5, :53, :81`). Kanal başına MaterialPropertyBlock ile **renk değiştirilebilir**. Paylaşılan materyal değiştirilmemeli.
- Materyal sırası öğeden öğeye değişiyor. Örneğin G01'in 0. yuvası C3, G02'ninki C1. Bu yüzden "Primary color" için "0. yuva" kuralı kullanılamaz, kanal materyal adıyla bulunmalı (Serhat da böyle yapıyor: `AccessoryPhotoStudio.cs:129`).

`prefab` = `Assets/Speed-Staes/Prefabs/Character/HumanCharacterDummy_F.prefab`. Satır numaraları `m_Name` satırları.

| Yuva | Nesne yolu (prefab satırı) | Renderer | Varyant / materyal | Renk | Sahibi |
|---|---|---|---|---|---|
| Headwear | `Accessories/Headwear` (:14036) | SMR | 9: H01–H07, premium PH01 (:6721), PH02 (:8041). Öğe başına 1–3 kanal | Evet (C1–C3) | Prefab: Malik. Mesh: Serhat |
| Glasses | `Accessories/Glasses` (:5070) | SMR | 9: G01–G07, PG01–PG02. 1–3 kanal. Rig'siz gelen gözlükler %100 B-head ağırlığı alıyor (`AccessoryPhotoStudioSetup.cs:73-85`) | Evet | aynı |
| Tops | `Accessories/Tops` (:3446) | SMR | 9: T01–T07, premium PTSMLE01 (:4504) ve PT02 (:10605). 1–3 kanal | Evet | aynı |
| Gloves | `Accessories/Gloves` (:14947) | SMR | 11 nesne, 9 set. Normal: GL01–GL03 (set 3'e GL03SMLEL/R da dahil), GLP04–07 (adında P var ama normal). Premium: GLP01 (özel kural, `AccessoryPhotoStudio.cs:78-79`) ve PGLP02 | Evet | aynı |
| Shorts | `Accessories/Shorts` (:7036) | SMR | 10 nesne, 9 set: S01–S07, PS01, PS02 (PS02SMLE dahil, :3069) | Evet | aynı |
| Leggings | `Accessories/Leggings` (:9163) | SMR | 9: L01–L07, PL01–PL02. Çoğu yalnız C3 | Evet | aynı |
| Socks | `Accessories/Socks` (:15525) | SMR | 9: SO01–SO07, PSO01–PSO02 | Evet | aynı |
| Shoes | `Accessories/Shoes` (:7109) | SMR | 9: SH01–SH07, PSH01–PSH02. Premium'larda 3 kanal | Evet | aynı |
| Ten (gövde) | `LowPoly Male` (:11765, mesh :11831) | SMR | 1 mesh, `ColorSkin_Toon` | Evet (Serhat rastgele atıyor, `AccessoryPhotoStudio.cs:143-148`) | Malik / Serhat |
| Eski temel kıyafet | `Tshirt` :1054, `Short` :14631, `Leggings` :5949, `Shoes` :7703, `Glove` :6895, `Helmet` :14774 (kökte, **açık**) | SMR | Mesh'ler Kevin Iglesias FBX'inde (`prefab:1121, 6016, 6962, 7770, 14699, 14842`) | — | Malik |

**Şüpheli: eski temel kıyafetin mesh'i kayıp olabilir.** Serhat `cff85eb` (2026-10-23 18:00) ile `Kevin Iglesias/.../HumanCharacterDummy_F.fbx` dosyasını değiştirdi (6 039 020 bayt). Yeni dosyada Model düğümleri şunlar: Rig, LowPoly Male ve 75 aksesuar. Tshirt, Short, Leggings, Shoes, Glove ve Helmet **yok**. Önceki sürümde (`f7f021f`, 1 050 332 bayt) bu 6 ad vardı, aksesuarlar yoktu. Prefab bu 6 mesh'e hâlâ bu FBX üzerinden bağlı. Büyük olasılıkla Inspector'da "Missing" görünüyorlar ve koşucu çıplak gövdeyle koşuyor. Serhat'ın iki aracı da bu 6 nesneyi elle gizliyor (`AccessoryPhotoStudioSetup.cs:154-158`, `ThemeRunPlaygroundSetup.cs:24`). Unity'de doğrulanmalı.

Sahiplik (git log):
- Prefab: malikyucel (`29b241a`, `f7f021f` "Add the designer's accessories…" 2026-10-23, son değişiklik `ad3616b` 2026-10-28).
- Aksesuar araçları, sahneler, toon shader ve materyaller: SerhatSiri (`504a42b`, `35b909e`, `cff85eb`, `dca7c3d`).
- `RunnerAccessories.fbx`: tasarımcının rig'siz orijinali (`f7f021f` mesajı). Tasarımcının adı geçmiyor.

## 2. Serhat'ın araçları ne yapıyor

- **`Scripts/Runner/AccessoryPhotoStudio.cs`** (runtime): Kıyafet önizleme aracı.
  - Kategori listesini 8 ad olarak tutuyor (`:38-39`).
  - Öğe adından set numarası çıkarıyor: 1–7 normal, 8–9 premium (`:13`, `:76-85`).
  - `SelectSet` her kategoride o setin öğesini açıp diğerlerini kapatıyor (`:87-116`). Set 0, temel görünüm olarak yalnız S01 ve SH01'i açıyor (`:104-106`).
  - Kanal renklerini materyal adına göre MaterialPropertyBlock ile rastgele atıyor (`:118-169`). Kapanınca eski bloğu geri koyuyor (`:171-176`). Paylaşılan materyale dokunulmadığı test ediliyor (`AccessoryPhotoStudioSetup.cs:321-323`).
  - **Capture:** set başına **tam boy** 1600×2000 PNG çekiyor (`:19-20`, `:184-228`). Görüntü RGB24, saydamlık yok (`:204`). Arka plan düz renk (`Setup:167-168`). Çıktılar `Captures/Accessories` klasörüne gidiyor (`:41`). Bu klasör `.gitignore:8` ile hariç tutulmuş ve diskte **yok**.
  - Sonuç: bu **ikon üretimi değil**, set önizlemesi ve kıyafet fotoğrafı. Öğe başına ikon üretmek için kategoriye göre kadraj (baş, el, ayak) ve saydam arka plan eklemek gerekir. Kamera ve render mantığı buna temel olabilir.
- **`Editor/AccessoryPhotoStudioSetup.cs`** (menü: Tools/Speed Stars/Accessory Photo Studio):
  - *Open Studio* (`:128-190`): karakteri ayrı bir sahneye kopyalıyor, bütün davranışları kapatıyor (`:152`), eski kıyafeti gizliyor.
  - *Validate All Sets* (`:215-237`): her sette her kategoride bir öğe olduğunu kontrol ediyor.
  - *Verify Runtime Colors* (`:265-326`) ve *Restore Original Environment* (`:328-387`).
  - **Dikkat, Sync Accessories From Model** (`:16-105`): FBX'ten mesh'leri yeniden üretip `RunnerAccessoryMeshes.asset`'e yazıyor (`:30`, `:90`). Malik'in prefab'ı aynı asset'i kullanıyor. Yani Serhat bu menüyü çalıştırınca oyundaki koşucunun mesh'leri de değişir (prefab dosyası değişmese bile).
- **`AccessoryLineup.cs` ve `Editor/AccessoryLineupSetup.cs`:** 9 karakteri yan yana dizip (set 1–9) 5760×1800 tek bir grup PNG'si çekiyor (`AccessoryLineupSetup.cs:38-57`). Tanıtım ve inceleme görüntüsü, ikon değil.
- **Çalışma anında değişim kanıtı:** `ThemeRunPlaygroundSetup.cs:38` stüdyoyu "wardrobe" olarak koşan koşucuya ayrı bir nesne üzerinden bağlıyor. `ThemeRunPlayground.cs:31` ile 0–9 tuşları koşarken set değiştiriyor. Yani prefab'a dokunmadan set açıp kapatmak zaten çalışan bir yol. IK'ye etkisini biz ölçmedik.
- Stüdyo ve dizilim sahnelerindeki karakterler prefab bağı kopmuş kopyalar (`reports/denetim-2026-10-28.md:34`). Prefab değişince bu sahneler güncellenmez, yalnız mesh asset'i paylaşılıyor.

## 3. Kozmetik sistemi önerisi

**Kategoriler** = mevcut 8 yuva (Headwear, Glasses, Tops, Gloves, Shorts, Leggings, Socks, Shoes). Her birinde 7+2 olduğu için GDD'nin "kategori başına 2 premium" kuralına (`Teknik_Tasarim.md:364`) birebir uyuyor. §18.6 açık kararı (`:593`) bununla kapanabilir. **DECISION** gereken noktalar:
- Ten rengi ayrı bir "yalnız renk" kategorisi olsun mu?
- Hangi kategoriler "yok" seçeneği alsın (Glasses, Headwear, Gloves…)?
- Türkçe görünen adlar ne olsun?

**Veri modeli** (asset'te tutulur, kodda sayı yok):
- `CosmeticCatalog : ScriptableObject` → `categories[]`, `swatches[]` (ekrandaki 5 hazır renk), `defaultLoadout`.
- `CosmeticCategory` → `id`, `displayName`, `slotPath` ("Accessories/Headwear"), `allowNone`, `iconFraming`.
- `CosmeticItem` → `id` (kalıcı, ör. `headwear.ph01`), `objectNames[]` (ör. `GL03, GL03SMLEL, GL03SMLER`), `premium`, `price` (50) ve `currency` (Mor Mücevher) asset'te, `icon`, `channels[]` (materyal adı ve etiketi: Primary/Secondary…), `defaultColors[]`, `effect` (premium için: prefab ve soket).
- Ad ayrıştırma (`SetFor`) yerine katalogda açık liste tutulmalı. GLP01 istisnası ve SMLE parçaları böyle kural gerektirmeden çözülür.
- Liste sırası: UI premium'ları üste dizer (`:365`), elle sıra tutmaya gerek yok.

**Kayıt** (ScriptableObject değil): `RacerLoadout` (preset id, kategori → öğe id, kanal → renk, ten) ve `OwnedCosmetics`. Satın alma tek yoldan EconomyService üzerinden yapılır (`:487`), idempotent olmalı.

**Uygulama: `CosmeticApplier`** (bizim namespace'imizde):
- Hedef koşucunun kökünü alır, `Accessories/<kategori>` altındaki öğeyi `SetActive` ile açıp diğerlerini kapatır.
- Rengi materyal adına göre bulunan kanala `_Color` MaterialPropertyBlock olarak yazar, `sharedMaterial`'a asla dokunmaz. Mantık `AccessoryPhotoStudio.cs:87-169`'un sade ve deterministik bir hâli.
- Bileşen prefab'a değil, sahnedeki ayrı bir nesneye konur ya da başlangıçta koddan eklenir (ThemeRunPlayground'daki gibi). Böylece **Malik'in prefab'ı değişmez**.
  - **SCENE:** PhysicalTest'e veya menü sahnesine applier nesnesinin eklenmesi ve sahnenin kaydedilmesi kullanıcıya kalır.
  - **DECISION:** applier prefab'ın içine konacaksa Malik onaylamalı.
  - **DECISION (Malik + Serhat):** eski temel kıyafet (Tshirt…Helmet). Ya applier çalışma anında kapatır ya prefab'tan kaldırılır.
- IK: mesh'ler aynı kemiklere deri ile bağlı olduğu için ek bir şey gerekmez. Animator veya RigBuilder'ın yeniden kurulması gerekmez (bu kodda Rebind yok).
  - Şüpheli: kalın tabanlı ayakkabılar (PSH*) yere gömülebilir. Zemin teması kemik geometrisinden hesaplanıyor, taban kalınlığı hesaba girmiyor (`RunnerPoseDriver.cs:409`, `:757-763`). Görsel kontrol gerekir.

**"Custom 1" yarışçı ön ayarları:**
- N tane `RacerLoadout` tutulur. Kaç tane olacağı açık karar.
- Ana ekrandaki seçici ◁ ▷ ve açılır listeyle çalışır. İlk değer `defaultLoadout`'tan gelir (ör. Serhat'ın set 0'ı: S01 + SH01).
- Seçicideki küçük portre çalışma anında RenderTexture ile ya da önceden üretilmiş ikonla gösterilebilir.
- Power, Stamina ve Height bu işin kapsamı dışında.

**Premium trail/partikül soketi:**
- Prefab'ta soket yok. Kemikler adla bulunur: B-head (:5394), B-hips, B-foot.L/R, B-toe.L/R, B-handProp.L/R (:3210, :3989; prop için açılmış kemikler).
- Efekt çalışma anında bu kemiğin çocuğu olarak oluşturulur, prefab değişmez.
- Çocuk nesnelere "B-" ile başlayan ad verilmemeli. `RunnerPoseDriver.FindDeep` kemikleri adla ilk eşleşmeye göre buluyor (`:554-559`).
- Yarış başa dönünce koşucu ışınlanıyor. Trail'ler `RunnerBody.RunReset` olayında (`RunnerBody.cs:26`, `:70`) temizlenmeli (`TrailRenderer.Clear`).
- Mobil bütçe önerisi: ayak başına tek trail, kısa ömür, az partikül (`Teknik_Tasarim.md:370`).
- **DECISION:** hangi kategorinin premium'u hangi efekti alacak (ör. Shoes → ayak izi, Headwear → baş partikülü).

## 4. Eksikler ve kimden

| Eksik | Durum (kanıt) | Kimden |
|---|---|---|
| Premium mesh varyantları | **Var**: 8 kategoride 2'şer, toplam 16 set. "Premium" ayrımı yalnız adlardan (P*) ve stüdyonun set 8–9 etiketinden geliyor, tasarımcıdan onay yok | Onay: tasarımcı / Serhat |
| Premium'un "göz alıcı" görünümü | Yok. Premium öğeler yalnız 3 renk kanalı taşıyor, parlak veya emissive materyal yok (bütün materyaller aynı toon shader) | Serhat (art) |
| Öğe ikonları | **Yok.** `Captures/Accessories` diskte yok ve git dışı (`.gitignore:8`). Stüdyo set başına tam boy, saydamlıksız çekiyor (`AccessoryPhotoStudio.cs:204`) | Ya Serhat stüdyoya öğe ve kategori kadrajlı, saydam çekim ekler ya biz ayrı bir editör aracı yazarız. Çıktı repo'da Sprite olmalı (**ASSET**) |
| Premium efektler (trail/partikül) | **Sıfır.** Hiçbir prefab veya sahnede ParticleSystem ya da TrailRenderer yok, efekt dosyası yok | Serhat (VFX) ya da biz basit yer tutucu. Kategori eşlemesi **DECISION** |
| UI (kategori sütunu, kilit, parlak premium panel, renk seçici, Mor Mücevher ikonu) | Hiç yok. uGUI, TMP, DOTween ve Zenject yeni eklendi (`875aab8`) | Biz (UI) + art (ikonlar) |
| Kayıt, ekonomi, 7. gün ödülü | Yok. 7. gün "geçici olarak kafa kozmetiği" (`Teknik_Tasarim.md:390`); PH01 mi PH02 mi belli değil | Biz. Hangi öğe olacağı **DECISION** |
| Eski temel kıyafet | Mesh'leri büyük olasılıkla kayıp (bkz. §1, **şüpheli**) | Malik (prefab) + Serhat (FBX) |
| Mesh senkronu süreci | Serhat'ın Sync menüsü oyundaki koşucunun mesh asset'ine yazıyor (`AccessoryPhotoStudioSetup.cs:16-105`) | Malik ve Serhat arasında süreç anlaşması |
| Normal öğelerin fiyatı ve kilidi | Ekranda normal öğeler de kilitli görünüyor. Nakit fiyatları GDD'de yok (§18.5) | Ürün sahibi (**DECISION**) |
| Kanal etiketleri | Ekranda yalnız "Primary color" var. Öğe başına hangi kanalın primary olduğu tanımsız | Tasarımcı (**DECISION**) |

Küçük not (şüpheli, zararsız görünüyor): G01'in mesh'i asset'in ana nesnesi (fileID 4300000, adı `RunnerAccessoryMeshes`). Sync `generated.name = asset.name` (`:36`) satırıyla bu adı veriyor. Katalogda öğeleri mesh adıyla değil, nesne adıyla eşlemek gerekir.
