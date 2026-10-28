# OPEN — GDD'de tanımlı olmayan ürün kararları (2026-10-28)

> Kaynak: PDF, MD §18, HTML "Açık kararlar". Bunlar uydurulmaz; cevap gelene kadar config'te boş/varsayılan kalır,
> ilgili task `blocked` değil, varsayımla ilerler ve varsayım config'te tek yerden değişir.

| # | Karar | Nerede | Çalışma varsayımı | Kimden |
|---|---|---|---|---|
| 1 | 7 günlük ödülün **6. günü** | PDF §2.4 | Config'te boş; panelde "?" | Tasarımcı / kullanıcı |
| 2 | Günlük sıfırlama saati (yerel mi UTC mi), kaçırılan gün, 7. günden sonra döngü | PDF §2.1, §2.4; MD §8, §11 | Yerel gece yarısı; kaçırılan gün sırayı bozmaz (her giriş günü bir sonraki gün); 7'den sonra başa döner | Kullanıcı |
| 3 | Orijinalde hangi yarışlar **biletli** (`requiresTicket`) | PDF §1; MD §6.1, §12 | Katalogda hepsi `false`, 100m test için `true` | Orijinali oynayarak doğrulama |
| 4 | Adet/Süreli Bilet'in Nakit fiyatı ve reklam oranı | MD §18.4 | Config boş; butonlar görünür ama pasif | Kullanıcı |
| 5 | Nakit kazanım kaynakları ve mağaza fiyatları | MD §18.5 | Yarış sonu Nakit yok; yalnız 7 gün ödülü | Kullanıcı |
| 6 | Kozmetik: normal öğelerin fiyatı, ten rengi ayrı kategori mi, hangi kategoride "yok" seçeneği, "Primary color" hangi materyal, 7. gün hangi premium kafa öğesi | MD §10, §18.6 | Kategori = prefab'taki 8 yuva (Headwear, Glasses, Tops, Gloves, Shorts, Leggings, Socks, Shoes), her birinde 7 normal + 2 premium hazır | Kullanıcı + Serhat |
| 7 | Saat güvenliği (cihaz saati değişimi, çevrimdışı) | MD §6.4, §14 | Cihaz UTC + geri alınan saat tespiti; sunucu saati yok | Kullanıcı |
| 8 | Hedef cihaz / FPS / minimum OS | MD §16, §18.11 | Android, 60 FPS, yatay (yatay kesin: 2026-10-28) | Kullanıcı |
| 9 | Engelli, 4×100m, Medley kuralları | MD §12, §18.8 | Katalogda var, oynanamaz ("yakında") | Malik + orijinal |
| 10 | Leaderboard backend | MD §15, §18.9 | **KARAR (28.10): yerel kalacak**, sahte bot süre dağılımı config'te | — |
| 11 | Ödüllü reklam SDK'sı (Adet Bileti için, PDF §1.2) | PDF §1.2 | Sahte uygulama (editörde anında ödül) | Kullanıcı |
| 12 | Edit racer "Arms" kategorisi (prefab'ta yok), "Body" = yalnız ten rengi mi | referans ekran | Arms yok; Body = ten rengi | Kullanıcı + Serhat |
