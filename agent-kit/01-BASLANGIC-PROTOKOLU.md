# BAŞLANGIÇ PROTOKOLÜ — "GDD geldi, şimdi ne olacak?"

> Bu, bir `.md` GDD verildiğinde izlenecek yol. Amaç: ilk günü keşifle değil,
> **karar**la geçirmek. `00-DERSLER.md`'deki her kural burada bir adıma
> dönüşmüş durumda.
>
> Kısa hâli: **Gün 0 sözleşme, Gün 1 iskele, Gün 2 oynanabilir dilim,
> Gün 3+ genişleme.** İlk üç gün feature yazılmaz.

---

## Neden böyle

Önceki projede GDD'yi doğrudan task'lara böldük. 60 task çıktı, hepsi yatay
katmandı (önce tüm veri, sonra tüm sistem, sonra tüm UI). Oyun ilk kez ancak
haftalar sonra baştan sona oynanabildi ve o gün üç mimari kararın yanlış
olduğu ortaya çıktı: UI kodla kuruluyordu, sayılar üç ayrı yerdeydi,
kompozisyon kökü çatışma merkezi olmuştu. Üçü de **Gün 0'da bir sayfa yazıyla
önlenebilirdi.**

---

## GÜN 0 — Temel Sözleşme (kod YOK)

Çıktı tek dosya: **`docs/TEMEL-SOZLESME.md`** (şablon: `templates/TEMEL-SOZLESME.md`).
GDD okunur, ama task'a çevrilmez. Onun yerine şu 9 başlık doldurulur:

### 1. Dikey dilim tanımı
Baştan sona oynanabilen **en küçük** şey. Bir cümle + 5 kabul satırı.
GDD'nin tamamı değil, kalbi. Örnek: *"Oyuncu bir dalga başlatır, düşmana
vurur, düşman kırılır, para düşer, dalga biter ve sonuç ekranı açılır."*

> Kural: dikey dilim **her katmanı** deler (girdi → mantık → görsel → veri → UI).
> Yatay dilim (sadece veri katmanı) yasak.

### 2. İsim sözlüğü
Alan adları, 15–25 tane, yazımı sabit. Hangi kavram hangi `namespace`'te.
Bunu Gün 0'da sabitlemezsen üçüncü haftada "Wave mi Day mi Career mi" diye
yeniden adlandırma turu kaybedersin — bu projede aynen oldu.

### 3. Katman / assembly haritası
Hangi assembly hangisine referans **verebilir**, ok yönleriyle. Bir kez karar
verilir. (Bu projede geç karar verildiği için bir sınıf, iki assembly'ye birden
ihtiyacı olduğu ortaya çıkınca asmdef'siz klasöre kaçmak zorunda kaldı.)

### 4. Kompozisyon kökü
Dosya yolu + tek cümlelik kural:
*"Alt sistemler buraya yazmaz. Her sistem bir seam açar (`Func<>`, arayüz,
`Initialize(...)` parametresi); bağlamayı yalnız supervisor yapar."*
Bu yol hook'a da yazılır (`hook-guard-write.ps1`).

### 5. Sahne ve prefab sözleşmesi  ← **en çok atlanan, en pahalı madde**
- Sahnelerin listesi ve her birinin görevi
- Canvas hiyerarşisi ve **isim yolu** kuralı (`GameCanvas/RunHud/Objective` gibi)
- Liste satırı kalıbı: sahnede **satır prefab'ı** + **content parent**, kod
  `Instantiate` eder
- Prefab'ların nerede durduğu (`Resources/` mi Addressables mı — **karar**)
- Tek cümle, kalın: **UI asla runtime'da kurulmaz.** View bağlar, üretmez.

### 6. Config sahipliği
Hangi ScriptableObject'ler var, hangi sayı nerede. Ve şu cümle:
*"Tüm denge sayıları bu asset'lerde durur, sahibi kullanıcıdır. Kod varsayılanı
yalnız ilk oluşturmada geçerlidir; çalışma zamanı asset'i okur. Testler bu
sayıları sabit olarak assert etmez."*

### 7. Doğrulama kapısı
Tek komut + çıkış kodları. **Üç durum ayrılır:** yeşil / kırmızı /
doğrulanamadı(ortam). Doğrulama merkezîdir — paralel agent'lar kendi başına
çalıştırmaz (Library kilidi tekildir).

### 8. GDD'nin yetki süresi
GDD'nin başına yazılacak satır:
*"Bu belgenin yetkisi ilk oynanabilir dikey dilime kadardır. O günden sonra
doğruluk kaynağı çalışan kod + config asset'leridir."*

### 9. Belirsizlikler
Kod yazmadan önce cevaplanması gereken sorular, **tek listede**. Aşağıya bak.

---

## GÜN 0.5 — Tek soru turu (en fazla 7 soru)

GDD okunurken biriken soruları dağınık sormak yerine **bir kez** sor. Filtre:

> **Sadece mimariyi değiştiren soruyu sor.** Cevabı ne olursa olsun aynı kodu
> yazacaksan sorma — makul olanı seç, `TEMEL-SOZLESME.md`'ye varsayım olarak yaz
> ve devam et.

Sorulacak tipler: platform ve giriş yöntemi · kayıt/ilerleme var mı · çevrimiçi
bir şey var mı · ödeme/reklam var mı · sanat yönü hazır mı yoksa placeholder mı ·
hedef oturum süresi · "bu oyunun kalbi ne" (belirsizse).

Sorulmayacak tipler: renk, font, tam sayılar, isimler, menü sırası. Bunlar
varsayımla geçilir; kullanıcı zaten Inspector'dan değiştirecek.

Cevaplanamayanlar için kod yazma: `docs/decisions/OPEN-<tarih>-<konu>.md` aç,
o task'ı `blocked` yap, **geri kalanı yapmaya devam et.**

---

## GÜN 1 — İskele (hâlâ feature YOK)

Gün sonunda var olması gerekenler:

| # | Çıktı | Not |
|---|---|---|
| 1 | `CLAUDE.md` | `CLAUDE.md.template`'ten, proje adı/yolları doldurulmuş |
| 2 | `.claude/settings.json` + 3 hook | Kurallar **zorlanır**, sadece yazılı olmaz |
| 3 | `.claude/agents/*.md` | 6 agent, `owns_paths` mantığı promptlarında |
| 4 | `scripts/verify.ps1` | Üç çıkış durumu ayrı; `pipeline-logs/` altına yazar |
| 5 | `docs/TUZAKLAR.md` | Boş ama var; agent promptları "başlarken oku" diyor |
| 6 | `docs/tasks/_TEMPLATE.md`, `_backlog.md` | |
| 7 | **Boş sahneler + placeholder prefab'lar** | Her ekran birer gri kutu olarak sahnede DURUYOR |
| 8 | **Config asset'leri, GDD'den gelen sayılarla dolu** | Kodla değil, asset'le |
| 9 | Kompozisyon kökü dosyası — boş, seam'leri tanımlı | |
| 10 | İlk commit, `agent/iskele` branch'inde | `main`'e dokunulmaz |

**Gün 1'in tek başarı ölçüsü:** boş oyun açılıyor, ekranlar sahnede görülebiliyor
ve Inspector'dan düzenlenebiliyor. İçi boş olması normal.

Neden 7. ve 8. maddeler Gün 1'de: bu projede ikisi de sona bırakıldı ve
ikisinden de tur kaybettik (K3 ve K6).

---

## GÜN 2 — Dikey dilim

8–12 task, hepsi **tek bir oynanabilir döngüye** hizmet eder. Paralel çalışır
ama `owns_paths` kesişmez. Gün sonunda:

- Döngü baştan sona **oynanıyor** — çirkin ama gerçek
- Her parça kompozisyon köküne **bağlı** (`needs:`'te açık WIRING yok)
- Her yeni davranışın bir testi var, hiçbiri sabit denge sayısı assert etmiyor
- Doğrulama yeşil

Bu gün kapandığında **GDD'nin yetkisi biter** (Gün 0, madde 8). Bundan sonrası
kod + asset.

---

## GÜN 3+ — Faz döngüsü

Tekrarlayan ritim:

```
1. Tur başı  : verify çalıştır · ListAgents · git show --stat ile önceki turu doğrula
2. Seçim     : en fazla 3 task · depends_on yeşil · owns_paths kesişmiyor
               · touches_scene olan YALNIZ başına
3. Dağıtım   : agent'a task DOSYASININ YOLU verilir, özet değil
4. Denetim   : git diff --name-only ile owns_paths ihlali · test var mı
               · kabul kriterleri tek tek
5. Kapanış   : statüleri kapat · `scripts/tur-sonu.ps1` çıkış 0 (açık WIRING/SCENE yok)
               · öğrenilen tuzak TUZAKLAR.md'ye · rapor yaz
```

Faz sonu ek işleri:
- **Ölü ayar taraması**: her config alanının bir okuyucusu var mı?
- **Bayat task taraması**: `review` kuyruğu 10'u geçtiyse yeni feature yok
- **Denetim raporu**: ne çalışıyor, ne yazıldı ama oyuna girmedi

---

## İlk üç günde YAPILMAYACAKLAR

- ❌ GDD'yi bölüm bölüm task'a çevirmek
- ❌ Yatay katman task'ları ("tüm veri modelini yaz")
- ❌ Kodla UI hiyerarşisi kurmak — geçici olsa bile
- ❌ Sayıları koda gömmek
- ❌ 3'ten fazla paralel agent
- ❌ Sahneye dokunan iki task'ı aynı turda çalıştırmak
- ❌ 20 soruluk belirsizlik formu

---

## Kullanıcının yazacağı ilk mesaj

`prompts/00-ilk-mesaj.md` dosyasında hazır metin var. Özeti:

> "GDD: `docs/gdd/<dosya>.md`. `agent-kit/` kurulu.
> `agent-kit/01-BASLANGIC-PROTOKOLU.md`'yi uygula — Gün 0'dan başla.
> Kod yazma, önce `docs/TEMEL-SOZLESME.md`'yi çıkar ve mimariyi değiştiren
> soruları tek seferde sor."
