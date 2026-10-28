# Settings — Modüler UI kurulum rehberi

## Sağlanan kapsam

Varsayılan ekran: Music toggle, Language dropdown, Flag selector, Player Name input ve Done/Close. Müzik notası, dünya, bayrak, kullanıcı, dişli, kapatma ve dropdown ikonları ayrı dosyalardır. Görselde kapalı duran açılır listelerin ve seçicilerin kullanılacak zemin/satır/seçim parçaları da pakettedir.

Yazılar, oyuncu adı, ON/OFF ve dil adları PNG'ye gömülü değildir. Unity TMP ile eklenir. Önizleme dosyalarındaki yazılar yalnızca kompozisyonu göstermek içindir. Ana menü/sahne görüntüleri teslim edilen UI sprite'larının arka planına basılmamıştır.

## Uzatılabilir panel

Önerilen Unity hiyerarşisi:

```text
SettingsRoot
  Dimscreen (raycast target)
  Panel (VerticalLayoutGroup)
    Header (128px, sabit)
      GearIcon / TitleTMP / CloseButton
    Body (esnek yükseklik)
      BodyBackground
      ScrollRect
        Viewport (RectMask2D)
          Content (VerticalLayoutGroup + ContentSizeFitter)
            SettingRow_Music
            SettingRow_Language
            SettingRow_Flag
            SettingRow_PlayerName
            [gelecekte eklenen satırlar]
        VerticalScrollbar
    Footer (112px, sabit)
      DoneButton
  PopupLayer
    LanguagePopup
    FlagPopup
```

- Panel header, body ve footer ayrı sprite'lardır. Body'nin yalnızca yan kenarları vardır; dikey uzatınca ek yeri oluşturmaz. Header/footer ile arada boşluk bırakılmadan birleştirilir. Alternatif tek zemin `panel_single_9slice.png` da sağlanmıştır.
- `manifest.json` her parçanın 9-slice border değerlerini içerir. Texture Type Sprite, Image Type Sliced kullanın. Header/footer köşelerini ve kenar kalınlıklarını rastgele ölçeklemeyin.
- Gövde alanı ekran güvenli alanına kadar büyüyebilir. Maksimum panel boyu dolunca ScrollRect devreye girer; başlık ve footer kaymaz.
- Satır şablonu 752×112; önerilen aralık 8px. Yeni ayar için yeni panel çizmek yerine aynı satır prefab'ını çoğaltıp label, ikon ve kontrol slotunu değiştirin.
- Her satırın sol ikon/etiket alanı ve sağ kontrol slotu vardır. Toggle, dropdown, input veya slider bu slota bağlanabilir. Slider daha uzunsa satırın tüm alt bölümünü kullanacak iki satırlı varyantı aynı 9-slice zeminle oluşturun.
- Viewport/ScrollRect yüksekliğini içerik boyuyla birlikte sonsuza uzatmayın; maksimum yükseklik ve yalnızca Content üzerinde ContentSizeFitter kullanın. Taşma yoksa scrollbar gizlenebilir.
- Dimscreen tam ekran ayrı yarı saydam katmandır. Arka plandaki ana menüye tıklamayı keser. Popup'lar en üst katmanda açılır, panel viewport'unda kırpılmaz.

## Ortak tuval / katman kaydı

`manifest.json` içindeki her grubun dosyaları aynı tuval boyutundadır. Photoshop'ta aynı başlangıç noktasında %100 ölçekle birleştirin; transparan kenarları otomatik kırpmayın. Kontroller farklı gruplarda farklı tuval ölçülerine sahiptir; bütün proje tek tuval değildir.

- Row ikonları ve row durum zeminleri: 752×112.
- Toggle track ve ayrı thumb ON/OFF konumları: 192×88. İki track veya iki thumb aynı anda gösterilmez.
- Field durumları, dropdown oku, input caret, selection ve clear ikonu: 356×80.
- Gelecekte kullanılacak slider Empty/Fill/Handle: 480×80. Full dolgu tam genişlikte; oyun değerine göre maskelenir. Tutamaç başlangıçta merkezde, ayrı hareket eder.
- Scrollbar empty/handle: 32×512. Handle uzunluğu viewport/content oranına göre runtime değişir; bu sprite sabit kaydırma miktarı tanımlamaz.
- Bayrak hücresi ve tüm bayraklar: 160×112. Bayrak görseli içerideki ortak 128×80 alana yerleşir; bu UI standardizasyonudur, bütün ülke bayraklarının resmî fiziksel oranı değildir.

## Ayar davranışları

**Music:** toggle yalnızca müziği açar/kapatır; ses efektleriyle birleştirilmez. Değer kaydedilir ve yeniden açılışta okunur. Red track açık, navy track kapalı, gri pasif. ON/OFF TMP ayrı; thumb taraf değiştirir. Başlangıç değeri oyun yapılandırmasından gelir.

**Language:** tıklanınca language_popup_background ve list_row parçalarıyla liste açılır. Desteklenen gerçek dil kodları proje lokalizasyon tablosundan gelir; paket hazır çeviri içermez. Görünen adlar kendi dilinde gösterilebilir (English, Türkçe gibi). Seçim tüm yerelleştirilmiş metinleri günceller ve kaydedilir. Mevcut seçim işareti ayrı overlay'dir. Dil seçimi oyuncu bayrağını değiştirmez.

**Flag:** ayrı grid popup açılır, tıklanan ülke kodu kaydedilir ve kapalı kontrolün bayrağı yenilenir. Bayrak oyuncu temsilidir; dil veya konum tespiti değildir. Grid daha fazla ülke eklendiğinde aynı cell prefab'ı ile büyür; ScrollRect kullanır. Popup dışına tıklama veya Close ile kapanma desteği aynı close parçalarını tekrar kullanabilir.

**Player Name:** field_normal / focused / error / disabled sağlanmıştır. Yazı, placeholder, caret ve selection runtime'dır. Platform klavyesi açılır; Done/submit ile proje doğrulamasından geçen isim kaydedilir. Maksimum uzunluk, izin verilen karakterler ve varsa sunucu eşsizliği belirtilmediği için burada kural uydurulmamıştır. İsim girişinin error mesajı ayrı TMP alanıdır. Caret ve selection için verilen sprite'lar özel görsel seçeneklerdir; TMP'nin kendi caret/selection çizimi de kullanılabilir.

**Done / Close:** bu öneride Music/Language/Flag seçimleri anında kaydedilir; isim commit sırasında doğrulanıp kaydedilir. Done açık isim düzenlemesini commit eder ve paneli kapatır. X açık isim düzenlemesindeki commit edilmemiş metni iptal edip paneli kapatır. Gerçek oyunda farklı kaydetme politikası varsa tek yerde yapılandırılmalıdır.

## Bayrak seti

16 ayrı ülke ikonu: TR, US, GB, DE, FR, IT, NL, BE, PL, UA, JP, SE, NO, DK, FI, CH. İsim/kod eşlemesi manifest'te. Set genişletilebilir; bütün dünya bayraklarının tamamı sağlanmış değildir. İkonlar UI boyutunda basitleştirilmiş çizimlerdir; US yıldızları ve GB çaprazları bu ölçekte okunurluk içindir.

## Dosya durumları

- Normal, hover, selected, disabled row zeminleri.
- Normal, focused, disabled, error field zeminleri.
- Dropdown up/down; list normal/selected/hover/disabled ve ayrı check.
- Flag grid normal/selected/hover hücreleri ve ayrı check.
- Done normal/pressed/disabled. İkonla birleşik buton zeminleri yok.
- Önizleme açık listeleri ana ekrana üst üste yığmaz; `preview_flag_popup.png` gridin bağımsız kontrol görüntüsüdür, production sprite değildir.

Bu paket görsel asset ve kurulum tarifidir; hazır çalışan Unity prefab/script kurulumu içermez. Ek bir oyun özelliği aktif etmeden gelecekteki satır/slider eklemelerine hazır parçalar sağlar.
