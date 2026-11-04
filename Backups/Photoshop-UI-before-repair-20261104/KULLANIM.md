# Photoshop UI paketi

Game HUD hariç 12 ekran ayrı PSD dosyasıdır. Tuval: 2400 × 1080 px.

## Katman kullanımı

- Her ekranın bir başlangıç görünümü açıktır. Alternatif durumların gözleri kapalıdır.
- `STATE - ...` grupları aynı öğenin alternatifleridir. Birini açarken aynı
  seviyedeki önceki durumun gözünü kapatın. Ortak ikon ve yazılar üst grupta kalabilir.
- `PHASE - READY`, `PHASE - SET`, `PHASE - GO!` yarış başlangıcının ayrı
  aşamalarıdır. Aynı anda yalnızca biri açık olmalıdır. Game Ready dosyasındaki
  Layer Comps panelinde bu aşamalar da kayıtlıdır.
- `CATEGORY - ...` gardırop kategorileridir; `SCROLL - ...` grupları listenin
  farklı kaydırma konumlarını gösterir. Aynı alanda yalnızca bir görünüm açın.
- Metinler Photoshop'un düzenlenebilir metin katmanlarıdır. `Fonts` klasöründe
  projede kullanılan fontlar ve beraberlerindeki lisans dosyaları bulunur.
- `Previews` klasörü her PSD'nin başlangıç görünümünün PNG karşılığıdır.

## İçerik

MainMenu, Edit Race, 7 Days Rewards, Map Select, Settings, Leaderboards,
Select Event, Game Finish, Game Ready, Analysis, Race Entry, Daily Gift.

Edit Race, projedeki `CosmeticsScreen` karakter/gardırop düzenleme ekranıdır.
Konumlar güncel prefab'lardan ve Unity'nin hesapladığı yerleşimlerden alınmıştır.
Arka plandaki 3D sahne ve koşucu UI katmanı olmadığından PSD'lere dahil değildir;
UI dışındaki alanlar şeffaftır.

Sıralama adları, süreler ve analiz eğrileri canlı yarış kaydına bağlı değildir;
bu alanlarda düzenlenebilir örnek içerik kullanılmıştır. Grafik serileri ayrı
görsel katmanlardır. `DURUM-ENVANTERI.json` dosyası paketlenen gizli grupları listeler.
