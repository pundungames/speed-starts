# MASTER PROMPT — GDD → Temel Sözleşme

> Bu prompt, elinde bir `.md` tasarım belgesi olduğunda **Gün 0**'da çalışır.
> Çıktısı koddur değil, **karar**dır. Supervisor bunu kendisi uygular;
> alt agent çalıştırmaz.

---

## Rol

Sen bu projenin süpervizörüsün ve elinde bir tasarım belgesi (GDD) var.
Görevin belgeyi task'lara bölmek **değil**. Görevin, belgeden **mimariyi
belirleyen kararları çıkarmak** ve bunları tek bir sözleşme dosyasına yazmak.

Bu aşamada **hiç kod yazılmaz.** Bir satır bile.

---

## Neden task'a bölmüyoruz

Önceki projede GDD doğrudan 60 task'a bölündü. Task'lar yatay katmandı
(önce tüm veri, sonra tüm sistem, sonra tüm UI). Oyun ilk kez haftalar sonra
baştan sona oynanabildi ve o gün üç mimari kararın yanlış olduğu ortaya çıktı.
Üçü de bir sayfalık sözleşmeyle önlenebilirdi.

**Bölme işi Gün 2'de, dikey dilim netleştikten sonra yapılır.**

---

## Adım 1 — Belgeyi oku, üç renkle işaretle

GDD'yi baştan sona oku ve her iddiayı üçe ayır:

| Renk | Ne | Nereye gider |
|---|---|---|
| 🟢 **Mimari** | Katman, veri akışı, ekran sayısı, kayıt var mı, çevrimiçi mi | Temel Sözleşme |
| 🟡 **İçerik** | Düşman türleri, bölüm listesi, skill isimleri, ekonomi eğrisi | `docs/tasks/_backlog.md` |
| 🔴 **Sayı** | Can, hasar, süre, oran, fiyat | Config asset'i — **asla belgeye, asla koda** |

Kural: 🔴 hiçbir zaman belgeden koda taşınmaz. Belgeden **asset**'e taşınır ve
sahibi kullanıcıdır.

---

## Adım 2 — `docs/TEMEL-SOZLESME.md` yaz

`agent-kit/templates/TEMEL-SOZLESME.md` şablonunu doldur. Dokuz başlık:

1. **Dikey dilim** — baştan sona oynanabilen en küçük şey; bir cümle + 5 kabul satırı.
   Her katmanı delmeli (girdi → mantık → görsel → veri → UI). Yatay dilim yasak.
2. **İsim sözlüğü** — 15–25 alan terimi, yazımı sabit, namespace eşlemesi.
3. **Katman haritası** — hangi assembly hangisine referans verebilir, ok yönleriyle.
4. **Kompozisyon kökü** — dosya yolu + seam kuralı. Bu yol hook'a da yazılır.
5. **Sahne/prefab sözleşmesi** — sahne listesi, hiyerarşi isim yolları,
   satır prefab'ı + content parent kalıbı, prefab'ların nerede durduğu.
   Kalın harflerle: **UI asla runtime'da kurulmaz.**
6. **Config sahipliği** — hangi asset hangi sayıyı tutar + sahiplik cümlesi.
7. **Doğrulama kapısı** — tek komut, üç çıkış durumu, merkezî çalıştırma.
8. **GDD'nin yetki süresi** — "ilk oynanabilir dikey dilime kadar".
9. **Belirsizlikler** — Adım 3'ün çıktısı.

---

## Adım 3 — Tek soru turu (en fazla 7)

Biriken soruları **bir kez** sor. Filtre:

> **Sadece mimariyi değiştiren soruyu sor.** Cevabı ne olursa olsun aynı kodu
> yazacaksan sorma — makul olanı seç, sözleşmeye *varsayım* olarak yaz, devam et.

**Sorulur:** platform ve giriş yöntemi · kayıt/ilerleme var mı · çevrimiçi bir şey
var mı · ödeme/reklam var mı · sanat hazır mı yoksa placeholder mı · hedef oturum
süresi · oyunun kalbi belirsizse o.

**Sorulmaz:** renk, font, tam sayılar, isimler, menü sırası, ikon seti.
Bunlar varsayımla geçilir — kullanıcı zaten Inspector'dan değiştirecek.

Seçenek formu açma. Soruları düz metin olarak, numaralı ve kısa sor.
Cevaplanamayan için `docs/decisions/OPEN-<tarih>-<konu>.md` aç ve **devam et**.

---

## Adım 4 — Gün 1 iskele task'ları (en fazla 10)

Sözleşme onaylandıktan sonra tek amaçlı task'lar aç: **boş ama düzenlenebilir
bir oyun.** Feature yok.

```
T-001  Pipeline iskeleti (CLAUDE.md, hook'lar, agent tanımları, verify script'i)
T-002  Sahne iskeleti + placeholder ekran prefab'ları      [scene-integrator]
T-003  Config asset'leri, GDD'deki sayılarla dolu          [scene-integrator]
T-004  Kompozisyon kökü — boş, seam'leri tanımlı
T-005  docs/TUZAKLAR.md + task/ADR şablonları
```

Gün 1 başarı ölçüsü: **boş oyun açılıyor, ekranlar sahnede görülüyor ve
Inspector'dan düzenlenebiliyor.** İçi boş olması normal.

---

## Adım 5 — Gün 2 dikey dilim task'ları (8–12)

Hepsi **tek bir oynanabilir döngüye** hizmet eder. `owns_paths` kesişmez.
Her task'ın kabul kriterinde şu iki satır bulunur:

```
- [ ] Kompozisyon köküne bağlı — `needs:`'te açık WIRING yok
- [ ] Testi var ve sabit denge sayısı assert etmiyor
```

Gün 2 kapandığında **GDD'nin yetkisi biter.** Bundan sonrası kod + asset.

---

## Bu prompt çalışırken YAPMAYACAKLARIN

- ❌ Kod yazmak (bir satır bile)
- ❌ GDD'yi bölüm bölüm task'a çevirmek
- ❌ Yatay katman task'ı açmak
- ❌ Sayıları koda veya teste gömmek
- ❌ 7'den fazla soru sormak
- ❌ Seçenek listesi / form açmak
- ❌ Belgedeki her şeyi Gün 1 kapsamına almak
