# BİRLİKTE ÇALIŞMA — post mortem ve çalışma sözleşmesi

> Ağustos–Eylül 2026, ~170 task, yüzlerce tur. Bu dosya kimin haklı olduğunu
> değil, **hangi alışkanlığın kaç tur yediğini** yazar.

---

## 0. İki kök kalıp — geri kalan her şey bunlardan türüyor

**Benim kalıbım: kanıtlamadan iddia ettim.**
Yanlış düğmeyi üç kez çevirdim, sahnede duran bir bileşen için "yok" dedim,
soruyu yanlış anlayıp bir bölüm yazdım. Üçü de aynı şey: elimde bir sonuç
vardı, onun ne anlama geldiğini doğrulamadan üstüne cümle kurdum.

**Senin kalıbın: durumu sessizce değiştirdin.**
Denge sayıları, branch, silinen asset'ler, aynı prefab'ın iki kopyası, açık
Unity. Hiçbiri yanlış değil — hepsi senin hakkın. Ama bana söylenmediği için
proje ayağımın altından kaydı ve ben **çıkarım yaparak** öğrendim.

Bu ikisi birbirini besliyor: ben tahmin ediyorum, sen sessizce değiştiriyorsun
→ ikimiz de yanlış zeminde inşa ediyoruz. **11.09'da kaybedilen tur tam olarak
bu ikisinin çarpımıydı.**

Çözüm simetrik ve ucuz: **ben iddia etmeden önce kanıtlarım, sen durumu
değiştirdiğinde bir satır yazarsın.**

---

## 1. Benim hatalarım

| # | Ne yaptım | Bedeli | Artık ne yapıyorum |
|---|---|---|---|
| 1 | `RunConfig.runDurationSeconds`'ı **üç kez** değiştirdim; o dal ölüydü, gerçek değer `WaveDurationDefaults`'tan okunuyordu | **3 tur** | Bir sayıyı değiştirmeden önce `grep` ile tüm okuyucuları bulup hangi dalın çalıştığını kanıtlıyorum |
| 2 | "Bu bileşen sahnede yok" dedim — **iki kez**. `FindObjectsOfType`, `HideFlags.DontSave` nesnelerini atlıyormuş | Yanlış teşhis, güven kaybı | Bir aracın ne döndürdüğünden emin değilsem sonucu üzerine iddia kurmuyorum |
| 3 | Sahneye dokunamadığım için **kodla UI iskeleti** kurdum (T-059 kalıbı) | Senin düzenleyecek hiçbir şeyin kalmadı | Prefab yoksa ekran eksik kalır. `NEEDS_SCENE` bırakıyorum |
| 4 | Gözetimimdeki bir agent kırmızı testlere bakıp domuz canını 1.5x, TimeFreeze'i 7/2/16 yaptı, bir filtreyi "regresyon" sanıp geri aldı | **1 tur + senin beğendiğin 3 ayar** | Kırmızı denge testi artık "düzeltilecek" değil "sorulacak" şey |
| 5 | `refresh_unity` çağırıp **kaydedilmemiş prefab işini sildim** | Senin emeğin | Derlemeden önce `isPlaying` / prefab stage / `isDirty` üçlüsü |
| 6 | Yanlış prefab kopyasını düzenledim (`Game/Prefabs/UI` ↔ `Resources/UI`) | 1 tur | Aynı adda iki dosya varsa hangisinin yüklendiğini önce kanıtlıyorum |
| 7 | Slider'a **toggle sprite'ı** koydum — görselin içinde çizili bir anahtar vardı, bakmadım | 1 tur | Asset'i kullanmadan önce ne olduğuna bakıyorum |
| 8 | Sen "test etme" dedikten sonra **iki kez daha** ölçmeye çalıştım | Senin sabrın | Hafızaya yazıldı; artık derle-oku-commit'le |
| 9 | Kendim kod yazdım; sen "supervizör kendi kod yazmasın" demek zorunda kaldın | Sahiplik bulanıklaştı | Task → owner agent → merkezî doğrulama |
| 10 | Bugün: "UnityEvent kullanmıyorsun" derken **serialize alanı** saydım, sen statik event bus'ı kastediyordun | 1 tur | Soruyu anlamadıysam cevap yazmadan önce soruyorum |

**En pahalısı 1 ve 3.** Birincisi üç tur, ikincisi projenin bir dönemini
"düzenlenemez" hale getirdi.

---

## 2. Senin tarafından pahalıya patlayanlar

Bunları isteyerek yazıyorum çünkü sen istedin. Hiçbiri "yanlış" değil —
hepsi **bana söylenmediği için** pahalı.

| # | Ne oldu | Bedeli | 5 saniyelik çözüm |
|---|---|---|---|
| 1 | **Denge sayılarını sessizce değiştirdin** (GPT ile / elle). Testler kırmızıya döndü, agent kodu "düzeltti" | En pahalı olay: 1 tur + 3 ayarın geri alınması | Değiştirdiğinde tek satır: *"pig HP'yi 1.5x yaptım, dokunma"* |
| 2 | **Branch'i söylemedin.** Yanlış branch'te çalıştım, sen "lan bu branche değil diğerinde bakacaksın" demek zorunda kaldın | Bir turun yarısı | Oturum açılışında: *"Branch: X"* |
| 3 | **Aynı prefab'ın iki kopyası** vardı; önce Resources dedin, sonra "ben karıştırmışım sorry" | 1 tur | İki kopya varsa biri silinir; hangisi geçerliyse söyle |
| 4 | **GDD'nin adı `FINAL_LOCKED_SINGLE_SOURCE`** kaldı ama oyun ondan ayrıldı; emekliye ayırman haftalar sonra oldu | Faz 3 boyunca yanlış referans | Belge eskidiğinde ilk satırına "artık bağlayıcı değil" yaz |
| 5 | **Unity'de paralel çalıştığını söylemedin** | Kaydedilmemiş işin silindi (hata benim, ama haber bunu önlerdi) | *"Unity açık, prefab düzenliyorum"* |
| 6 | **Asset silme/taşımaları görmedim** (Piggy FBX, 5 materyal, 4300 dosyalık reimport) | Kirli ağacı yorumlamak için tur başı zaman | *"Şunları sildim"* — tek satır |
| 7 | **Bağlamsız kısa talimatlar**: "Objective adı", "sil kodu önemli deil" | Bağlam bende varsa mükemmel, yoksa 1 round trip | Referansı bir kez adlandır: *"Objective adı — GameCanvas/RunHud/Objective"* |
| 8 | **Doğrulama kuralı zaman içinde değişti** (önce ölç → sonra ölçme), genel bir cümle olarak söylenmedi | Her turda yeniden çıkarım yaptım | Kural değişince: *"bundan sonra hep böyle"* → hafızaya yazarım |

---

## 3. Beni en verimli nasıl kullanırsın

### 3.1 Oturum açılışı — bunu yapıştır, gerisi akar

```
Branch: <ad>
Sahne: <ad>
Unity: açık / kapalı   (açıksa: kaydedilmemiş işim var mı)
Bu turda elle değiştirdiğim sayılar: <yoksa "yok">
İş: <ne istiyorsun>
```

Bu beş satır, geçmişte kaybettiğimiz turların **çoğunu** önlerdi.

### 3.2 Bir şey bozuksa — dört satır

```
Ne yaptım   : <adım>
Ne bekledim : <beklenen>
Ne oldu     : derlenmedi / görünmedi / yanlış göründü / kötü hissettirdi
Nerede      : <sahne / prefab / dosya>
```

**"Çalışmıyor" tek başına dört ayrı şey demek olabiliyor** ve hangisi olduğunu
bilmeden doğru yere bakamıyorum. Üçüncü satırdaki tek kelime bunu çözüyor.

### 3.3 Bir his istiyorsan, referans ver

Bunlar senin **en iyi** talimatlarındandı, çünkü uygulanabilirdi:

- *"bill paid deki gibi bu da animasyonla damga yapıştırır gibi gelecek"*
  → mevcut bir şeyi işaret ettin, birebir kopyaladım
- *"scale i 1.5 dan 1 e hızlı bir şekilde damga yapıştırıyor gibi gelsin"*
  → sayı + benzetme verdin, doğrudan koda döndü

Buna karşılık *"daha etkili olsun"*, *"güzel olsun"* tek başına benim için
ölçülemez. Yanına ya bir sayı, ya bir karşılaştırma, ya bir ekran görüntüsü
koy — üçünden biri yeter.

### 3.4 Ekran görüntüsü çok değerli

Coin yığını ekran görüntüsü, üç paragraf açıklamadan fazlasını anlattı.
Görsel bir sorun varsa **önce ekran görüntüsü**, sonra cümle.

### 3.5 Beni kesmekten çekinme

*"kanka dur"* mükemmeldi. Yanlış yöne gittiğimi gördüğün an kes — ne kadar
erken kesersen o kadar az iş çöpe gider. Özür dilemene, açıklamana gerek yok.

### 3.6 Toplu iste

On küçük polish isteği tek mesajda, on ayrı mesajdan **çok** daha ucuz.
Birbirine değen işleri bir arada verirsen sıralamayı ben yaparım.

---

## 4. Anlamadığım / anlayamayacağım şeyler

Dürüst liste. Bunları bilirsen bana boşuna sormazsın.

1. **Oyunu göremiyorum.** Dosya okuyorum. Nasıl göründüğü ve nasıl
   hissettirdiği konusunda tek kaynağım sensin ya da bir ölçüm.
2. **Senin bilinçli değişikliğinle regresyonu ayırt edemiyorum.** Kırmızı bir
   test ikisinde de aynı görünüyor. Söylemezsen yanlış tarafı seçme ihtimalim
   yüksek.
3. **"İyi/kötü hissettirme" bende karşılığı olmayan bir ölçü.** Sayıya,
   karşılaştırmaya veya görüntüye çevirmen gerekiyor.
4. **Zamir ve işaret sözcükleri** ("şu", "orası", "bu panel") — bağlam
   taze değilse boşa düşüyor. Bir kez adlandır, sonra istediğin kadar "şu" de.
5. **Oturumlar arası hafızam eksik.** Kalıcı olmasını istediğin bir şey varsa
   söyle, hafızaya yazayım — yoksa bir sonraki oturumda yeniden keşfederim.
6. **Türkçe, kısa yazım, noktalama yok — hiç sorun değil.** Seni anlıyorum.
   Sorun hiçbir zaman dil değildi, **eksik referans**tı.

---

## 5. Senin iyi yaptıkların — bunları daha çok yap

- **Anında kesme** (*"kanka dur"*)
- **Mevcut bir şeye işaret etme** (*"BillPaid'deki gibi"*)
- **Tam olarak ne istediğini tarif etme** (*"1.5'tan 1'e, damga yapıştırır gibi"*)
- **Ekran görüntüsü**
- **Dramasız düzeltme** (*"lan bu branche değil"* — net, hızlı, doğru)
- **Yanlışını kabul etme** (*"ben karıştırmışım sorry"*) — bu, yanlış dosyada
  daha fazla tur harcamamızı önledi
- **Kuralı sebebiyle söyleme** (*"ben o UI'ları editleyeceğim"*) — sebebi
  bilince kuralı doğru genelleyebiliyorum

---

## 6. Çalışma sözleşmesi — altı madde

**Ben:**
1. Bir değeri değiştirmeden önce **okuma yolunu kanıtlarım**; "muhtemelen bu
   düğmedir" demem.
2. Emin olmadığımda **iddia etmem** — ya ölçerim ya sorarım.
3. Editöre dokunmadan önce **kaydedilmemiş işini kontrol ederim**; sen
   istemedikçe Play'e girmem.
4. Kod yazmam, **dağıtırım**; `done` yetkisini kanıtsız kullanmam.

**Sen:**
5. Durumu değiştirdiğinde **tek satır** yazarsın: branch, sayı, silinen dosya,
   açık Unity.
6. Bir his istediğinde yanına **referans** koyarsın: sayı, benzetme ya da
   görüntü.

**İkimiz:**
7. Yanlış gidişi **erken keseriz**. Geç kesilen tur, hiç başlamamış turdan
   pahalıdır.

---

## 7. 21.09–27.10 eki — iki tarafın yeni pahalı alışkanlıkları

Yukarıdaki bölümler 21.09'da yazıldı ve geçerli. Sonraki beş haftada eski kalıplar
(kanıtsız iddia, sessiz durum değişikliği) azaldı; yerine iki yeni kalıp çıktı:
**ben istenmeyen işi yaptım ve kullanıcının çalışma alanına dokundum; sen kuralı
bağlamına göre değiştirdin ama kapsamını söylemedin.** Ayrıntı: `00-DERSLER.md` K11–K19.

### 7.1 Benim tarafım

| # | Ne yaptım | Bedeli | Artık ne yapıyorum |
|---|---|---|---|
| 1 | **Kendi kendime iş yaptım:** plan istendi, kod yazdım (01.10); istenmeyen büyük iş (08.10 Çekiç Merdiveni); "gönder"i SDK onayı sayıp ~560 satır olay ekledim (14.10); domuz torbasını sormadan değiştirdim (22.10) | Her biri bir geri alma + "niye kafana göre iş yapıyorsun" | Plan = yalnız belge; uygulama açık "tamam, yap" ile; çok okunuşlu büyük işte önce tek cümle teyit |
| 2 | **Test koşup kullanıcının işini geri aldım** — prefab işi 5 kez (29.09–26.10), kayıt 2 kez (21.10, 27.10) | Senin emeğin ve dev save'in; *"naptın"*, *"save'imi neden sildin"* | Commit'siz iş varken ana Editor'de test yok (hook), test kopyada, öncesinde PlayerPrefs yedeği |
| 3 | "Süpervizör ol" denmişken küçük işi kendim yaptım (26.10 akşam, 27.10); "sen yap" denmişken küçük işleri agent zincirine soktum (29.09) | *"neden kendin yaptın"* / *"neden bu kadar uzun sürüyor"* | Mod `docs/DURUM.md` ilk satırında; tahmin etmiyorum |
| 4 | Reddedilen öneriyi yeniden açtım (15.10 fatura eğrisi; 22.10 Workshop eşikleri üç kez) | Sabrın | Reddedilen öneri hafızada "reddedildi"; tekrar açılmaz |
| 5 | Sorulmayan bulguyu açıklamaya kattım (26.10) — sen onu benim uydurduğum yeni bir kural sandın | Güven | Yalnız sorulan davranışı anlatıyorum, terimi senin kelimenle |
| 6 | Koddaki yedek sayıyı gerçek sandım (22.10: kodda 90 sn, asset'te 0) — iki süpervizör sana yanlış soru sordu, ADR yazıldı | 1 tur + boşa karar | Bir sayıyı söylemeden önce asset'i okuyorum |
| 7 | Performans için görünümü kestim (19.10 coin parlaması; kazanç yoktu) | *"ESKİ GÖRÜNÜME GEL"* | Görseli kesmeden önce maliyetin gerçekten düştüğünü gösteriyorum |

### 7.2 Senin tarafın

| # | Ne oldu | Bedeli | 5 saniyelik çözüm |
|---|---|---|---|
| 1 | **Kural değişince kapsamı söylenmedi.** "Süpervizör kod yazar mı" beş kez değişti (03.09, 10.09, 29.09, 19.10, 26.10); hangisinin bu iş için, hangisinin kalıcı olduğu belli değildi — ben her birini "bundan sonra hep" diye yazdım | Her değişiklik bir sonraki bağlamda ters düştü | Tek kelime yeter: **"süpervizör ol"** ya da **"hızlı mod"**; ben DURUM.md'nin ilk satırına yazarım ve sen değiştirene kadar kalır |
| 2 | Bir akışı **parça parça** tarif ettin (26.10 ticket akışı aynı gün beş kez düzeltildi; *"of hadi ya düzgüne halledelim"*) | 5 tur | Kuralı bir kez bütün olarak söyle — ya da ben tabloya dökeyim, sen "tamam" de |
| 3 | **Commit'siz prefab işin varken** *"çalışmıyorum, bu dalda yap"* (26.10) | Test koşusu konfeti ayarlarını geri aldı (yedekten döndü) | "Commit'siz X var" — tek satır; ya da önce commit |
| 4 | **Başka PC'de / başka kişide yapılan değişiklik** haber verilmeden geldi (14.10: 50 commit; 26.10: "sandbox kapatıldı" dendi ama repoda hâlâ açıktı) | Devir notunda yok, çıkarım | "Şurada şu değişti / commit'lenmedi" — tek satır |
| 5 | **Brif Downloads'ta kaldı** (01.10: üç briften biri; 23 skill hiç yapılmadı) — benim de kopyalamamam | Bir gün | Brifi verirken "docs'a al" de; ben de her oturum başı Downloads'a bakıyorum |
| 6 | **Sayının birimi belirsizdi** ("8'de açılsın" — fatura mı, gün mü; 16.10) | 1 tur | "N. fatura ödenince" de; ben de sayıyı değiştirmeden önce birimi tek cümleyle teyit ediyorum |

> §2'deki 8. maddenin çözümü (*"bundan sonra hep böyle"* demek) yetmedi: kural sabit
> değil, bağlama bağlıydı. Yerine mod anahtarı geldi (7.2 madde 1).

### 7.3 Sözleşmeye ek

**Ben:**
8. Plan istendiyse **yalnız belge** yazarım; uygulama açık "tamam, yap" ile başlar.
9. Editörüne dokunan her koşudan önce **commit'siz işine ve kaydına** bakarım; olmuyorsa
   kopyada koşarım. Senin dosyana dokunmadan önce yedek alırım.

**Sen:**
10. Modu **tek kelimeyle** değiştirirsin: "süpervizör ol" / "hızlı mod". Commit'siz
    işin varsa ya da başka yerde bir şey değiştiyse **tek satır** yazarsın.

---

*Kaynak: Bills Must Be Paid / Piggy Payday · 21.09.2026 · v2 eki 27.10.2026*
