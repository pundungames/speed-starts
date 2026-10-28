# Theme Selection — Kullanım

Tema bütün oyun mekânıdır. Karakter, platform ve thumbnail bulunmaz. Solda dört kaynak, sol üstte geri, sağ altta tema adı/oklar ve aksiyon butonu bulunur.

## Buton durumları

| Durum | Zemin | Ayrı TMP metni | Para ikonu | Etkileşim |
| --- | --- | --- | --- | --- |
| Satın alınmamış, yeterli bakiye | theme_action_background.png | Gerçek fiyat | theme_action_icon.png | Satın al |
| Sahip olunan, aktif değil | theme_use_background.png | USE | Gizli | Etkinleştir |
| USE basılı | theme_use_pressed_background.png | USE | Gizli | Basılı görünüm |
| Aktif tema | theme_in_use_background.png | IN USE | Gizli | Pasif |
| Bakiye yetersiz | theme_disabled_background.png | Gerçek fiyat | theme_action_icon.png | Pasif |

Satın alma ve USE krem-lacivert; aktif tema lacivert/krem, pasif durum gri. BUY yazısı yok. Bütün yazı ve fiyatlar Unity TMP ile ayrı yerleştirilir. button_states.json eşlemeyi içerir.

## Akış

Açılışta aktif tema seçilir. Sağ/sol oklar tüm mekânın önizlemesini değiştirir; para harcamaz veya kalıcı seçim yapmaz. Fiyat butonu sahiplik ve bakiye kontrolünden sonra tek işlemle satın alır; tekrar tıklama ikinci kesintiye yol açmaz. Satın alma sonrası USE görünür. USE tıklanınca aktif tema kalıcı kaydedilir ve IN USE olur. Geri dönülürse uygulanmamış önizleme iptal edilir ve kayıtlı aktif ortam geri yüklenir. Hata halinde bakiye/sahiplik/aktif tema değişmez. Bu etkileşim akışı öneridir; gerçek oyun kodu pakette bulunmaz.

## Katmanlar

Tüm PNG'ler gerçek alpha transparan. Aynı grubun zemin ve ikonlarını aynı konuma yüzde 100 ölçekte koyun; transparan kenarları kırpmayın. Aksiyon butonunun bütün durumları aynı 467x117 tuvali ve alpha siluetini kullanır. Bir anda tek durum zemini görünür. USE / IN USE durumlarında para ikonu kapalı, metin ortalıdır. layout.json yerleşimi içerir. design_preview.jpg satın alma; preview_use_state.jpg USE önizlemesidir, sprite değildir.

Gerçek tema adları/fiyatları/adetleri ve ödeme birimleri henüz verilmedi. CITY örnek addır; fiyat boş bırakıldı. Elmas taslak ödeme ikonudur; tema verisiyle belirlenir. Liste uçlarında okların pasif olması önerilir, sarma davranışı kesinleşmemiştir.
