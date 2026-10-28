# Post-Race Results UI

## Katmanlar

- Bütün production PNG'ler alpha transparandır; yazı, sıra numarası ve süreler sprite'a gömülü değildir. TMP ile ayrıca yerleştirin.
- Altı sol butonun zemin/ikonları 391×104 ortak ölçüdedir. Normalde Race Again kırmızı; diğerleri krem. Ek pressed/disabled zeminleri aynı ölçüdedir. High Scores podium ikonunda sayılar gömülü değildir.
- Her üst bilgi kartının kendi zemin/ikon katmanları ortak tuvaldedir. Kartlar birbirinden farklı genişlikte olabilir.
- Sonuç satırının zemin, divider, oyuncu vurgusu ve kupa katmanları 566×64 ortak tuvaldedir. İlk üç sıra gold/silver/bronze cup kullanır. Kupalar madalya değildir. Aynı anda bir satırda bir kupa gösterilir.
- `trophy_*_standalone` dosyaları alternatif 80×72 küçük ikonlardır; sonuç satırına doğrudan bindirmek için `result_row_trophy_*` dosyalarını kullanın. `trophy_gold_large` isteğe bağlı büyük kaynak görseldir.
- Günün en iyisi dış panel, başlık ve ayrı taç, tek oyuncu satırı olarak bölünmüştür. Başlık ve taç 570×67 ortak tuvalde; satır ayrı 570×58.
- Dimscreen, scene üstüne UI altına konan ayrı yarı saydam tam ekran sprite'tır. Karakter ve platform bu ekranda bulunmaz.
- Photoshop: aynı grubun dosyalarını aynı başlangıç noktasına yüzde 100 ölçekte koyun. Transparan boşlukları kırpmayın. `layout.json` referans koordinatlarını içerir.

## İçerik ve davranış

- Sol sıra: Race Again, Watch Replay, High Scores, Stats, Main Menu, Practice. Her etiket ayrı TMP'dir. İkonlar ayrı sprite.
- Elmas alanı oyuncunun mevcut bakiyesini gösterir; örnekteki 250 gerçek bakiye veya yarış ödülü kuralı değildir.
- Sonuç tablosu yalnızca mevcut yarışı gösterir: rank, oyuncu adı ve bitiriş süresi. YOU metni veya oyuncu adı runtime belirlenir. Oyuncu satırı rank'tan bağımsız olarak `result_row_player_highlight` ile vurgulanır.
- İlk üçte altın, gümüş ve bronz kupa görünür; 4 ve sonrasında kupa katmanı kapalıdır. Beraberlik / DNF sıralaması oyun yarış verisinden gelir; bu paket yeni kural belirlemez.
- BEST OF THE DAY mevcut yarıştan ayrı bir günlük liderlik alanıdır. Yalnızca tek kişinin adı ve derecesi gösterilir. Kaynak veri yoksa gerçekmiş gibi örnek isim/süre kullanılmaz; alan boş durum metniyle veya gizli gösterilebilir.
- PERSONAL BEST kartında seçili yarış türüne ait kayıtlı kişisel en iyi süre görünür. Yeni rekor olup olmadığı runtime karşılaştırılır; her yarış otomatik olarak yeni rekor sayılmaz. Alev yerine yıldırım kullanılır.
- WORLD POSITION dünya liderlik tablosundan gelen sırayı `#128` gibi gösterir. Mevcut yarıştaki 1–6 sırasıyla karıştırılmaz.
- PERCENTILE, dünya liderlik verisinden gelen Top yüzdesini gösterir. Örnekteki Top 8% tasarım örneğidir; sıra #128 tek başına yüzdeyi belirlemez. Hesap için uygun toplam oyuncu ve yarış bazındaki sunucu kuralı gerekir.
- 21.50s, örnek oyuncu adları ve diğer süreler yalnızca tasarım referansıdır; veritabanı veya kalıcı oyun verisi olarak kullanılmaz.
- Race Again mevcut etkinliği yeniden başlatma akışına; Replay yarış tekrarına; Stats istatistik ekranına; High Scores liderlik listesine; Main Menu ana menüye; Practice antrenmana bağlanır. Bilet/erişim kontrolleri mevcut oyun kurallarıyla yeniden yapılır; bu UI ücretsiz giriş yaratmaz.

## Pakette

41 production PNG, metinsiz yerleşim önizlemesi, onaylı ekran referansı, yerleşim JSON'u ve tekrar üretim kodu. Hazır çalışan Unity prefab veya oyun mantığı kodu içermez.
