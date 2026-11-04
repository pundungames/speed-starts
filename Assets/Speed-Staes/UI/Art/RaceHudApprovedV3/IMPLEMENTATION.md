# Onaylı HUD — ayrı parçalar v3

Onaylı referans: approved_reference.png. assembled_no_text_preview.png paket içindeki gerçek parçaların birleşimidir. Bu iki dosya yalnız karşılaştırma içindir; çalışma anında ayrı parçaları kullan.

## Katmanlar
7 boş panel zemini, 7 ayrı ikon ve Energy/Cadence için ayrı boş/dolu bar katmanları bulunur. Her bileşenin tüm katmanları aynı canvas boyutunu ve merkez pivotunu kullanır. İkonları görünür sınıra göre kırpma. Photoshop'ta %100 aynı köşeye yerleştirildiğinde hizalanırlar.

layout.json referans ekran ölçüsünü, ekran üzerindeki dikdörtgenleri ve yerel dolgu alanlarını içerir. Ekran büyütülürken tüm ilgili katmanları aynı oranda ölçekle. Tuvaldeki boş alan hizalama içindir.

## Unity
Sprite (2D and UI), RGBA alpha açık, mipmap kapalı. UI Image Preserve Aspect. Panel zeminini, ikonları, barları ve TMP yazıları kardeş nesneler olarak yerleştir. Dolguları yalnız fill_rect içinde soldan sağa kırp; bütün bileşen canvasını Filled Image ile kırpmak yanlış sonuç verir. fill_rect içeriğini maskele veya bu alana ayrı bir Image yerleştir. Boş zemin ve ikonları dolgu ile beraber kırpma.

Referanstaki Energy ve Cadence sürüklenebilir slider değil, gösterge barıdır. Görünmeyen bir tutacak eklenmedi. Yeni tutacak veya farklı cadence mekaniği bu paketin parçası değildir.

## Ayrı yazılar
Exit, Restart, para miktarı, geçen süre (2.80 s örneği), hız (16.6 mph örneği), Energy, Cadence, değerler ve sıra (8 ile küçük th örneği) TMP ile ayrı oluşturulur. Değerleri sabitleme. Süre beyaz, hız açık pembe, sıra ana sayısı sarıdır; kaynakta kalın dar italik yazı görünür. Orijinal font dosyası bulunmadığından pakete font eklenmedi. Oyundaki mevcut uygun fontu kullan; lisanslı orijinal font mevcutsa onu tercih et.

## İşlev
Exit ve Restart mevcut oyun akışlarına bağlı kalır. Para gerçek bakiyeyi gösterir. Süre, hız, enerji, cadence ve sıra gerçek yarış verisinden alınır. Örnek sayılar davranış tanımlamaz. Normal görünüm referans alınmıştır; kaynakta görünmeyen pressed/disabled tasarımlar üretilmedi.

## Kaynak ve sınırlar
İkonlar onaylı düzleştirilmiş görselden ayrıldı; koyu ve beyaz ikonların kenar alfası zemin renginden temizlendi. Panel zeminleri aynı referansın imagegen ile yazı/ikonları temizlenmiş sürümünden ayrıldı. Bu nedenle özgün katmanlı kaynak dosyası veya piksel piksel aynı saklı panel dokusu iddiası yoktur. Oranlar ve yerleşim onaylı referansa kaydedilmiştir. Glow ve sahne pikselleri eklenmemiştir.
