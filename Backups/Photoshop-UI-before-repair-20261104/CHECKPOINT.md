# Photoshop UI paketi — checkpoint

Tarih: 4 Kasım 2026, Europe/Istanbul.

## Kullanıcı isteği ve kapsam

Game HUD **hariç** aşağıdaki 12 ekran, ayrı PSD olarak teslim edilecek:
MainMenu, Edit Race (CosmeticsScreen), 7 Days Rewards, Map Select, Settings,
Leaderboards, Select Event, Game Finish, Game Ready, Analysis, Race Entry, Daily Gift.

Yalnızca statik görünüm yeterli değil. Oyunda açılıp kapanan, değişen ve sırayla
gösterilen tüm mevcut UI durumları doğru konumda, adlandırılmış ve gruplanmış
katmanlarda bulunacak. Alternatifler başlangıçta gizli olacak. Özellikle ses
açık/kapalı ve READY / SET / GO ayrı açılabilir olmalı. Metinler düzenlenebilir
Photoshop metin katmanları olarak oluşturulacak.

## Tamamlanan hazırlık

- Güncel UI prefab ve görselleri bulundu; oyun kaynakları değiştirilmedi.
- 12 prefabın hiyerarşisi ve iç içe prefab örnekleri okundu.
- Unity MCP ile geçici preview scene kullanılarak 2400×1080 tuvalde gerçek
  LayoutGroup yerleşimleri hesaplandı; preview kapatıldı, sahne/prefab kaydedilmedi.
- 538 görsel katman için hazırlık PNG'leri üretildi. Henüz teslim PSD'leri yok.
- Photoshop COM kaydı/tür kitaplığı sorununa, sistem kaydını değiştirmeden,
  ScriptingSupport.8li içindeki IDispatch metodunu çağıran yerel köprü hazırlandı.
  Probe betiği başarıyla çalıştırıldı; uygulama özelliklerinin doğrulaması sürüyor.

## Çalışma dosyaları

- `Temp/psd-export/export_ui.py`: prefab çözümleme ve katman manifesti oluşturma.
- `Temp/psd-export/base-manifests.json`: ilk 12 ekran manifesti.
- `Temp/psd-export/unity-layout.json`: Unity'den doğru hesaplanmış konumlar.
- `Temp/psd-export/tree2.txt`: ilk hiyerarşi dökümü (layout override uygulanmadan).
- `Temp/psd-export/layers/`: hazırlık PNG'leri.
- `Temp/psd-export/ps-bridge.ps1`: Photoshop betik çalıştırma köprüsü.
- `Temp/psd-export/probe.jsx`, `photoshop-probe.json`: bağlantı kontrolü.
- Python 3.11 kütüphaneleri: `Temp/psd-export-py311`.

## Sıradaki işler

1. Unity yerleşimini manifesterin tüm düğümlerine uygula, görsel katmanları yeniden üret.
2. Görünüm scriptlerini okuyup varsayılan durumları ve gizli alternatifleri düzenle.
3. Dinamik listelerin satırlarını ve mevcut alternatif durumlarını ekle.
4. READY / SET / GO grupları, ayar popup'ları, ödül ve seçim durumlarını doğrula.
5. Photoshop'ta düzenlenebilir metinlerle 12 PSD üret, aç/kaydet ve PNG önizlemelerini kontrol et.
6. Paket, fontlar, durum envanteri ve kısa kullanım notunu teslim et.

Git commit/push yapılmadı; kullanıcının açık talebi yok.

## Devam checkpoint'i

- 12 ekranın PSD temelleri `PSD/` altına yazıldı. Gizli durumlar ve dinamik
  liste örnekleri dahil; son manifest `Temp/psd-export/final-manifests.json`.
- Photoshop'ta iki font doğrulandı: AmericanCaptain ve Kanit-BlackItalic.
- MainMenu gerçek metin katmanlarıyla kaydedildi; PNG'si açılıp kontrol edildi.
- Gardırop ilk oluşturulduğunda aşırı katmanlıydı. Ortak kart durumları
  paylaşılacak şekilde 3047 katmandan 475 katmana indirildi; tüm eşya ikonları korundu.
- Devam eden Photoshop betiği: `Temp/psd-export/finalize.jsx`.
  `native-done/` altındaki işaretler biten ekranları gösterir; betik yeniden
  çalıştırıldığında bunları atlar. `photoshop-build.log` aşamaları kaydeder.
- Photoshop bazen "nested layer groups ... older than Photoshop CS6" bilgi
  penceresi gösteriyor. CUA ile ilgili alt pencere Raise, ardından Return ile
  kapatılabiliyor. Kullanıcı dosyaları değiştirilmedi.
- Son doğrulama: `Temp/psd-export/verify_package.py`. Beklenen toplam 317 metin
  katmanı; sonradan eklenen alternatiflerle sayı artabilir. 12 PNG önizlemesi
  ve sıfır `__TEXT__` yer tutucusu zorunlu.
- Son paket yalnızca `PSD/`, `Previews/`, `Fonts/`, kullanım ve doğrulama
  dosyalarını içermeli. Ana klasördeki ilk deneme PSD'leri teslim ZIP'ine alınmamalı.

## /compact sırasında kritik devam bilgileri

- Kullanıcı en son "nerdeler", "nereye gittin", `/compact` dedi. Dosya konumları
  commentary'de paylaşıldı. Görev bitmedi; çalışmayı sürdür, bitmiş gibi sunma.
- Asıl teslim PSD'leri `Deliverables/Photoshop-UI/PSD/` içinde; üst klasördeki
  PSD'ler ilk denemelerdir, ZIP'e girmemeli.
- Photoshop arka plan COM betiği devam ediyor: exec session `86973`.
  Köprü `ps-bridge.ps1`, final betik `finalize.jsx`; günlük `photoshop-build.log`.
- Bitmiş işaretleri `Temp/psd-export/native-done/` içindedir.
- CUA persistent session: `psAppFallback` şu anda Photoshop window 463090'a bağlı.
  Gerektiğinde `cua.listWindows()` ve `cua.getApp({windowId:...})` ile yenile.
  Bilgi penceresi metni tam olarak: "This document contains nested layer groups
  that may change in appearance if opened in applications older than Photoshop CS6."
  Son AX gözleminden doğru pencere index'ine `performSecondaryAction(index,'Raise')`,
  sonra `pressKey('Return')` ile kapanıyor. Zaten OK odaklıysa sadece Return yeterli.
  Ekran minimize olduğundan screenshot/click geometry çalışmıyor, AX+klavye çalışıyor.
- İlk Photoshop ağır gardırop PSD'sini açarken bellek tüketti; yalnızca bize ait
  belge varken süreç durduruldu. `optimize_edit.py` 475 katman / yaklaşık 13 MB
  sürümü üretti. Bu sürüm native olarak kaydedildi ve 21 gerçek metin katmanı,
  sıfır yer tutucu doğrulandı. Eski 3047 katmanlı sürümü yeniden açma.
- Python: `C:/Program Files/Python311/python.exe`; bağımlılıklar scriptlerde
  `Temp/psd-export-py311` üzerinden ekleniyor. `py` elevated ortamda farklı sürüm
  seçebiliyor, mutlak executable kullan.
- Son manifest `final-manifests.json`; tek ekran cache'leri `manifests/*.json`.
  MainMenu 12, Edit Race 21, Rewards 22, Map 6, Settings 47, Leaderboards 68,
  Select Event 16, Finish 49, Ready 3, Analysis 41, Entry 12, Daily Gift 20
  olmak üzere 317 düzenlenebilir metin hedefleniyor.
- `verify_package.py` PSD'leri, metin sayılarını, yer tutucusuz olmalarını ve
  PNG'leri doğrular; durum envanteri ve All Screens.jpg üretir.
- `check_states.jsx` 6 alternatif görünümü PNG'ye kaydedip asıl PSD varsayılanlarını
  değiştirmeden kapatır. Tüm native dönüşüm bittikten sonra çalıştırılmalı.

### Görsel QA'da tespit edilen ve henüz düzeltilmeyenler

1. Rewards önizlemesinde gün miktarları görünmüyor. Metin katmanları var ama
   kaynak TMP component'leri disabled olabilir. Her Day*/Amount içindeki gerçek
   metin layer'ının görünürlüğünü aç; 100,200,x1,200,x1,300 değerleri manifestte var.
2. Map Select önizlemesinde fiyat düğmesi boş. Fiyat metni/ikonunun hem kendi
   katman görünürlüğünü hem STATE - purchaseSprite alt grubunu denetle. Üstteki
   purchase senaryosunda PriceLabel text ve currency icon görünür olmalı; diğer
   senaryolar gizli kalmalı. İlgili düzeltmeyi native PSD üzerinde yaptıktan sonra
   yeniden kaydet ve PNG'yi yenile. IN USE alternatifi de kontrol edilmeli.
3. Bazı dinamik sayılar (gardırop para alanları) kaynakta boş olduğundan dönüştürülmedi.
   Tam paket için örnek değerleri uygun kutularda native text olarak eklemek gerekebilir.
4. MainMenu CharacterCard/Play örtüşmesi ve Edit Race alt kategori taşması kaynak
   prefab konumlarında mevcut; Home.unity CosmeticsScreen instance'ı yalnızca kök
   transform override'ları içeriyor, iç konumları değiştirmiyor. Kaynak yerleşimini
   keyfi tasarımla değiştirme; gerekirse kullanım notunda mevcut sınırı açıkla.

Kullanıcı sık status bekliyor; 60 saniyeyi aşmadan kısa, somut ilerleme bilgisi ver.
