MOD: süpervizör
# DURUM — Speed Stars (speed-starts)

> Oturum başı ilk okunan dosya. En fazla 60 satır; her checkpoint'te BAŞTAN yazılır (eklenmez).
> İlk satır geçerli moddur: `MOD: süpervizör` ya da `MOD: hızlı` (bkz. docs/CALISMA-KURALLARI.md §14 A).
> Geçmiş: `docs/checkpoints/`. Alttaki OTOMATIK bölümü `scripts/tur-sonu.ps1 -DurumYaz` yazar; elle düzenleme.

Son güncelleme: 2026-11-03 gün sonu · main 02b5c13f (origin'den 1 önde) · checkpoint `docs/checkpoints/2026-11-03.md`
**YARIN İLK İŞ:** T-103 (Edit modda çentik önizlemesi + Tools → Speed Stars → Notch Preview aç/kapa) dalda bekliyor: worktree
`D:\Pundun\speed-starts-t103`, 44e78be3, DOĞRULANMADI → kopyada derleme + EditMode (`SafeAreaEditPreviewTests`, kayıt testi KANIT) +
PlayMode `SafeAreaPlayTests`, .cs.meta'ları kopyadan al, ff → kullanıcı Home'da aç/kapa + kaydet → sahnede diff yok mu. (D:\Pundun\speed-starts-t093 klasörü git'e bağlı değil, silinebilir.)

## Faz 8 (cila) — bugün main'e girenler (son kopya koşusu yeşil: EditMode 1198, PlayMode 52 + 7 atlanan, Zenject)
- T-081..T-090: ok tuşları, regresyon, ekonomi v2 (ADR `ADR-2026-11-03-ekonomi-v2-referans.md`), 200m/400m/60mH/400mH, ayarlar v2,
  emote v2, Race of the Day, Practice paneli · e7d269f2 Malik'in 03.11 işi (Watch Replay, kamera, mobil UI).
- T-091 Malik'in elle ayarları builder'da + EventModal 4. sekme · T-092 Practice'te sonuç/ödül/rekor yok · T-093 High Scores'ta Race
  of the Day · T-094 Watch Replay duman testleri · T-095 lisans envanteri · T-096 FPS toleransı %0.001 · T-097 ana menü kartı · T-098 replay prefab.
- T-099 çentik iki yana (inset = max(sol, sağ), yalnız Play'de) · T-100/T-101 TextMeshPro + American Captain: 243 yazı prefab'larda
  YERİNDE çevrildi (fileID aynı; menü: Tools → Speed Stars → Convert UI Text to TMP), 2 InputField legacy (TTF) · T-102 bütün builder
  kayıtları `HandEditGuard`'dan: var olan prefab/sahne ezilmez, yamalar yalnız eksiği ekler (kopyada 10 giriş noktası, 36 dosyada fark 0).

## Verilen kararlar
- Kullanıcı 03.11: "referans oyun ne ise aynısı". Ekonomi: yarış Nakit'i bitirme +10, zorluk bonusu 10 × oyuncu zorluğu, rakip bonusu
  round(10 × geçilen/toplam); rakipsiz yarış Nakit ve Mor Mücevher vermez. Giyim 500 / kafa 1.000 Nakit (varsayılan kıyafet ücretsiz,
  giyili öğeler eski kayıtta sahipli sayılır), emote 3×2.500 / 3×5.000 / 3×7.500, reklam +200 Nakit (günde 3), Süreli Bilet 300 Nakit,
  Adet Bileti Nakit'le satılmaz. Erişim: 100m / Race of the Day / Practice biletsiz; diğer yarışlar biletli (PDF'ten sapma: 100m biletsiz).
- Gerçek gelir / reklam SDK'sı / IAP şimdilik YOK (oyun Çin müşterisine; kullanıcı 03.11). Eski rekorlar sıfırlanacak (iş yok).
- TMP + American Captain her yerde, bundan sonra da (kullanıcı 03.11; kurallar §9, ADR). Prefab'lar elle düzenleniyor: builder var olanı ezmez.
- Installer bağlamaları: kalıcı onay (03.11). Teslim belgesi, telefonda APK denemesi, ana menü hızı: kullanıcı "geç" dedi.
- Önceki: tema 0 fiyat = bedava; Sky Islands premium ama bedava; ten tonları 8; başlangıç Adet Bileti 3; gün sınırı UTC;
  günlük build Mono/ARMv7, mağaza build'i IL2CPP.

## Kullanıcıdan / başkalarından bekleyen
- Font lisansı ve Çince yedek font: kullanıcı 03.11 "şimdilik bizi ilgilendirmiyor". Uygulama/paket adı, keystore, URL'ler: sonra.
- Malik: UI üstündeki dokunma adım sayılmasın; engel teması (`docs/ENGEL-TEMAS-NOTU-malik.md`); 4×100m ve Medley; Practice için
  `RunnerSim` enerji API'si ve ayakta çıkış (T-090); dev bitiş tuşu (F) replay'i bozuyor (T-094); ayak kilidi (soru); bilgi: ReplayControlsView
  prefab bağlıyor (T-098); `LateStepFeedback` runtime legacy Text'i American Captain değil (TMP'ye geçirirse font tek asset'ten).
- Serhat: 4abc876a `BridgeCanalRiverside.prefab`'ı yeniden yazdı; `PastelBuildingRow` sahne açılınca bina üretiyor.
## Gözle bakılacaklar (Editor'de Play)
- YENİ: her ekranda American Captain (boyut/taşma, BestFit → auto size), 4 kontur preset'i (kalınlık tahmini), READY/SET/GO konturu
  (`RaceStartConfig` OutlineWidth 0.2), yazılar düz (italik kalktı), Device Simulator'da çentik iki yana (yalnız Play'de).
- Kazanç dökümü, Nakit "+" reklam penceresi, premium "Spend 300", 200m/400m/engelli, ayarlar, emote mağazası, Practice bitişi,
  High Scores → DAILY, Watch Replay, Close konumları, ana menü kartı; 30.10'dan beri: tema, karakter döndürme, botlar, liderlik, kostüm.

## Dikkat
- Builder'lar artık var olanı ezmiyor (T-102); `-forceOverwriteHandEdits` bayrağını kullanma (Malik'in binaları + kullanıcının ayarları gider).
- PowerShell'de `git` bozuk (system32 gölgesi): git Bash'ten. Kancada `git commit` + "main" aynı komutta olmaz.

<!-- OTOMATIK:BASLA -->
_tur-sonu.ps1 2026-11-03 17:55 - otomatik blok, elle yazilan bu blokta kaybolur._

- **KAPI TEMIZ (cikis 0)**
- Acik needs: 3 (DECISION 3)
- Review kuyrugu: 0
- Goz bekleyen (merged): 94 - T-102, T-100, T-101, T-099, T-097, T-098, T-095, T-093
- in-progress: 0 / blocked: 0
- Uyari: govde-needs 0, test 4, analytics 0 (kapsam: status in-progress/review/merged)
- GIT: 2 calisma agaci (ana: main); ana agacta commit'siz .prefab/.unity/.asset: 0
- KOPYALAR: 2 (kopya.ps1 -Liste -Json)
<!-- OTOMATIK:BITIR -->
