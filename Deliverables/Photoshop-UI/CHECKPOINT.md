# Photoshop UI — güncel checkpoint

4 Kasım 2026. Kapsam artık Game HUD dahil 13 ekran.

Orijinaller: Backups/Photoshop-UI-before-repair-20261104. Kullanıcının mevcut dosyaları bu yedekte korunuyor. Unity kaynakları değiştirilmedi. Commit/push yok.

Eski Temp otomasyonu yoktu; Temp/psd-export yeniden oluşturuldu. Python bağımlılıkları Temp/psd-export-py311.

İlk denetim: 12 PSD, yalnız 5 PNG. Yedi PSD içinde toplam 209 __TEXT__ yer tutucusu tespit edildi. Henüz tamamlanmış teslim yok.

Çalışan köprü: run_js.py. Photoshop COM çağrıları sandbox dışında çalıştırılmalı; kayıt defteri değişikliği gerekmedi. DoJavaScript IDispatch kimliği 1147828311, parametreler code, [], 1.

Kaynak envanterleri: source-texts.json, hud-source.json. İlk PSD envanteri audit.json.

Düzeltme: batch.py + repair.jsx; ekran bazında OPEN/SAVED kayıtları repair.log. Bunlar yalnız bu yeni çalışmanın kayıtlarıdır. Photoshop dosyaları sırayla açar, metin oluşturur ve PSD/PNG kaydeder.

MainMenu: eski StatsStrip/PowerLabel ve PowerIcon kaldırıldı; yeni ReferenceStats/Power korundu. 7 Days Rewards: eksik altı Amount eklendi. Diğer ekranlar ve son QA halen sürüyor.

HUD: build_hud.py ile onaylı V3 parçaları güncel prefab konumlarında ayrı katmanlar olarak hazırlandı. Game HUD-base.psd çalışma dosyasıdır, teslim değildir. hud.jsx gerçek metinleri ekleyip teslim PSD/PNG oluşturur; henüz çalıştırılmadı.

Sıradaki: batch tamamlanmasını izle; HUD oluştur; tüm PNG görünümlerini incele ve hataları düzelt; yer tutucu/metin/durum denetimi; alternatifleri render et; kullanım notları ve yalnız PSD/Previews/Fonts/notlar içeren ZIP.

## İlerleme güncellemesi
İlk dönüşüm turunda MainMenu, Rewards, Map, Settings, Edit Race, Ready, Entry, Daily Gift, Select Event ve Game Finish kaydedildi. Analysis/Leaderboards turu sürüyor. Bu kayıtlar son QA değildir.
Son tur: finish_batch.py --hud (HUD + 12 ekran finalfix.jsx); ardından run_variants.py, rename_layers.py, native_verify.jsx ve verify.py. finalfix metinleri düzeltilmiş esneyen kutu ölçülerine ve doğrudan nokta metni koordinatlarına göre yeniden hizalar. Settings bozuk 113 grup kapanış işaretini temizler; ON/OFF ve nickname yeniden kurulur. Map fiyat/ikon üst sıraya taşınır. Game Ready tekrar hizalanır.
Önemli: İlk tur PNG’lerinde sağa kaymış metinler hâlâ görülebilir; finalfix çalışmadan teslim etme.
