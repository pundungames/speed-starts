# Referans oyunda rakipler ve zorluk (30.10.2026, telefonda ölçüm)

> Kaynak: orijinal oyun (Galaxy S23, sürüm 2.55), adb + monkey ile 11 yarış, ekran kaydı kare kare.
> Amaç: bizim rakip AI'ımızı tasarlamak için davranışı anlamak. Sayılar bizim için hedef değil, gözlem.

## 1. Rakip türü ayarda, oyuncu seçiyor
Ayarlar → Competitors:
- **Competitor type:** Global players · Bots · Time trial · Friends
- **Race my ghost:** kendi hayaletine karşı koşma
- **Difficulty** (kaydırıcı) ve **Skill range** (kaydırıcı, rakiplerin yayılımı)
Sonuçta "Difficulty bonus +10" ödülü var: zor ayar ödülle teşvik ediliyor.

## 2. Global players = kayıtlı koşular (hayalet)
- "Finding competitors" ile 3-6 rakip gelir; isimler gerçek oyuncu adı + bayrak.
- **Aynı grup her koşuda birebir aynı süreyi koşar**, oyuncudan bağımsız (4 kez doğrulandı: biz 15.44 / 41.26 / 21.11 / 13.49
  koşarken rakipler hep aynı). Geri kalınca bekleme, öne geçince hızlanma YOK.
- Restart ve Race again aynı grubu tutar; ana menü → Play yeni eşleşme.
- Eşleşme son yarışa göre hemen değişmiyor: oturum başında 12-18 sn'lik grup, iki kötü yarıştan sonra ~23-28 sn'lik gruplar;
  sonraki 4 galibiyette (13-21 sn) grup ~28 sn bandında kaldı. Gruplar kendi içinde dar (0.4-0.6 sn).

## 3. Bots = yarış içinde simüle edilen AI
- 6-7 bot, gerçekçi isim + bayrak. Kalkışta hızlılar (iyi tepki, sert ivme), en yüksek hızları düşük; biz 5-10. sn'de geçtik.
- **Her yarışta süreleri değişiyor**; sıra (beceri) büyük ölçüde korunuyor:

| Bot | Yarış 1 (biz 17.68) | Yarış 2 (biz 21.04) |
|---|---|---|
| Enoch | 18.99 | 16.95 |
| Lourenco | 19.83 | 18.78 |
| Theodore | 22.32 | 20.78 |
| James | 26.10 | 23.76 |
| Francois | 28.67 | 26.03 |
| Tyreek | 32.19 | 25.11 |

  İkinci yarışta hepsi 1-7 sn hızlandı. Sebep (yarış başı rastgelelik / oyuncuya göre ayar / galibiyetle zorlaşma) iki örnekle
  ayrılamadı; üçüncü ölçüm bağlantı koptuğu için alınamadı.

## 4. Yarış akışı ve ekran
- READY ~3 sn → SET ~2 sn → GO; saat GO'da başlar. GO'dan önce basmak false start: ekran kırmızı yanar, READY baştan.
  Tepki süresi ekranda.
- Ekranda: süre, hız (mph), enerji (yarışta 100 → 62-85), cadence, 10/30/60 m ara süreleri (kişisel rekora göre +/-),
  "adımlarını biraz yavaşlat" ipucu (çok hızlı basınca).
- Sonuç ekranı oyuncu bitirince açılır, liste rakipler bittikçe dolar. Ödül: bitirme +10, sıra (1. +10 civarı, 4. +2),
  zorluk bonusu +10 (galibiyet toplam +30).
- Logcat: release IL2CPP, oyun kendi logunu yazmıyor; yalnız reklam SDK'ları.

## 5. Bizim için çıkarım (öneri, karar kullanıcıda)
- Çevrimdışı ve sunucusuz başlamak için önce **Bots**: her bot bizim koşucu fiziğini (RunnerSim) basış üreten bir sürücüyle koşar
  (ritim + gürültü + beceri), yarış başına rastgelelik; zorluk ve yayılım config asset'inde.
- Sonra **kendi hayaletin**: oyuncunun basışları kaydedilir, tekrar oynatılır (sunucu gerekmez).
- Global hayaletler sunucu ister; ileri iş.
