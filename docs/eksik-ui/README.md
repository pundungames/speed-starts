# Eksik UI listesi (30.10.2026)

Orijinal Speed Stars telefonda (Galaxy S23) baştan sona gezildi ve bizim oyunla karşılaştırıldı. **Bizde zaten olan ekranlar
listede yok**; burada yalnız bizde hiç olmayan ekranlar ve parçalar var. Bizim tasarımlar kendi düzenimizle devam eder, görüntüler
yalnız "neyin eksik olduğunu" göstermek için. Görüntülerde telefonun durum çubuğu kırpıldı, kişisel ad alanları bulanık.

## A. Bizde hiç olmayan ekranlar

| # | Görüntü | Eksik UI | Ne içeriyor | Not |
|---|---|---|---|---|
| 1 | `03-elmas-magazasi.png` | **Para mağazası** (kaynak çubuğundaki "+") | Reklam izle → para (günde 3 hak, sayaçlı), gerçek parayla 3 paket | PDF'te yalnız bilet için ödüllü reklam var; para paketi ürün kararı |
| 2 | `01-upgrade-tam-surum.png` | **Upgrade / Tam sürüm** | Tek seferlik satın alma: sınırsız yarış, tam kişiselleştirme + paylaşma, 2× kazanç, reklamsız; "No thanks" | Bizde Upgrade butonu pasif |
| 3 | `04-emote-secimi.png` | **Emote seçimi** | Emote listesi (kilitli/fiyatlı), karakterde canlı önizleme, satın al butonu; yarış içinde emote butonu | Bizde Emote butonu pasif; animasyon içeriği gerekir |
| 4 | `02-oyuncular-arkadaslar.png` | **Oyuncular / Arkadaşlar** | Arkadaş listesi (0/50), oyuncu arama (ad veya ID), arkadaş ekle, "My ID" | Çevrimiçi özellik; bizim liderlik yerel. Şimdilik kapsam dışı önerisi |
| 5 | — | **Tekrar izle (Watch Replay) oynatıcısı** | Yarış tekrarı | Bizde buton pasif, tekrar sistemi yok |

## B. Var olan ekranlarımızda eksik parçalar

| # | Görüntü | Ekran | Eksik parça |
|---|---|---|---|
| 6 | `05-gunun-yarisci-karti.png` | Ana ekran | **Race of the Day modu**: kart "Racer of the day" hazır koşucuyu (kendi kıyafeti, Power/Stamina/Height) gösterir; Edit racer / Emote / Custom okları gizlenir |
| 7 | `06-yaris-geri-sayim-ve-hud.png` | Yarış | **Başlangıç geri sayımı** (hazır → GO!), **sıra göstergesi** (sol alt "1st"), koşucuların üstünde **bayraklı isim etiketi**, **pist ilerleme halkası** (200/400 m için), yarış içi **emote butonu**. Etiket ve sıra rakip koşucularla birlikte gelir (Malik) |
| 8 | `07-sonuc-kazanc-dokumu.png` | Yarış sonu | **Kazanç dökümü** (sağ üst: "+16 = yarışı bitirdin +8, zorluk bonusu +8") ve oyuncu satırında **DB / PB rozetleri** (günün en iyisi / kişisel rekor) |
| 9 | `08-ayarlar-1.png` `09-ayarlar-2.png` `10-ayarlar-3.png` | Ayarlar | Bizde yalnız Müzik, Dil, Bayrak, Ad var. Eksik bölümler: **Rakipler** (rakip türü, hayaletimle yarış, zorluk, beceri aralığı), **Oynanış** (şerit tercihi, engel dokunma hassasiyeti), **Görsel** (kamera mesafesi, ekran uzatma, partikül, klasik animasyon, kadans göstergesi, ipucu bildirimleri), **Emote** (butonu göster, otomatik emote), **Sıfırlama** (özel yarışçıları sil, kişisel rekoru sıfırla, tüm ilerlemeyi sıfırla), **Diğer** (Destek, Kullanım şartları, Gizlilik politikası, Hesabı sil) |

## C. Bizim kapsamda olup tasarımı henüz gelmeyenler (şu an yer tutucu görünümde)

| Ekran | Durum |
|---|---|
| **Giriş kapısı / bilet penceresi** (Play'e basınca) | Çalışıyor ama sanatı yok (düz yer tutucu) |
| **Günlük giriş hediyesi** popup'ı (+ kartın kaynak çubuğuna uçuşu) | Faz 4; sanat yok |
| **7 günlük giriş ödülü** paneli (ana ekrandaki takvim) | Faz 4; sanat yok |

## Öncelik önerisi

1. **Mağazaya çıkmak için şart:** Ayarlar'a *Gizlilik politikası* ve *Kullanım şartları* bağlantıları (Google Play ve Çin mağazaları ister);
   *Hesabı sil / tüm ilerlemeyi sıfırla* da mağaza kurallarında sık istenir. Bunlar için yalnız satır tasarımı yeter (ayarlar satır prefab'ımız hazır).
2. **Bizim PDF kapsamı:** Giriş kapısı, Günlük hediye, 7 gün paneli sanatı.
3. **Oynanışı tamamlayan:** Yarış geri sayımı, sıra göstergesi, isim etiketleri, kazanç dökümü + DB/PB, Race of the Day kartı.
4. **Ürün kararı gerektiren:** Para mağazası, Upgrade/tam sürüm, Emote, Oyuncular/Arkadaşlar, Tekrar izle.
