# ADR-2026-11-03-ekonomi-v2-referans

Durum: kabul edildi
Tarih: 03.11.2026
Karari veren: kullanici ("verdigin md ayni sekilde gecir, adamlarin ekonomisini yap direkt")

## Baglam
GDD (MD §18.4-18.5) Nakit kazanim kaynaklarini ve magaza fiyatlarini acik birakti. Oyunda yaris kazanmak hicbir sey vermiyordu, Nakit'in
tek kaynagi 7 gun takvimiydi, Mor Mucevher veren etkinlikler (400m, 60mH, Medley) kapaliydi. Referans oyun telefonda olculdu:
`docs/analiz/2026-11-03-referans-oyun-ekonomi.md`.

## Karar
Bolum 4'teki tablo oldugu gibi uygulanir: biletli yarista bitirme +10, zorluk bonusu +10, rakip bonusu round(10 x gecilen / toplam rakip);
normal kozmetik giyim 500 / Headwear 1.000 Nakit (varsayilan kiyafet ve hazir preset ogeleri ucretsiz); emote 2.500 / 2.500 / 5.000 / 7.500;
Adet Bileti 100, Sureli Bilet 1.000 Nakit; odullu reklam +200 Nakit, gunde 3. Gercek para (IAP, tam surum) bu kararda yok.

## Gerekce
Referansin ucretsiz oyuncu degerleri (yaris 10-30, kiyafet 500-1.000, emote 2.500-7.500, reklam 200 x 3) olculdu; kullanici dogrudan
alinmasini istedi. GDD sabitleri (bilet 3 / 30 dk, Sureli Bilet 1 saat, 7 gun odulleri, Mor Mucevher 3/2/1, premium 50) korunur.

## Sonuclari
- 28.10 "normal kozmetik bedava" karari geri alindi (DURUM "Verilen kararlar").
- Biletsiz etkinlik (Practice, bugun 110mH) Nakit vermez; hangi etkinliklerin biletli oldugu ayri karar (kullanici).
- PDF disi: reklamla Nakit (PDF'te reklam yalniz bilet icin). Kullanici karariyla eklendi.
- Etkilenen: EconomyConfig, yeni yaris Nakit odulu config'i, CosmeticCatalog/EmoteCatalog fiyatlari (seeder), sonuc ekrani, kaynak cubugu (T-084, T-085).

## Geri alma sarti
Cihazda oynanista ilerleme cok yavas/hizli bulunursa ya da gercek para (IAP) eklenince fiyatlar yeniden olculur.
