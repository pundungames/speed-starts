MOD: süpervizör
# DURUM — Speed Stars (speed-starts)

> Oturum başı ilk okunan dosya. En fazla 60 satır; her checkpoint'te BAŞTAN yazılır (eklenmez).
> İlk satır geçerli moddur: `MOD: süpervizör` ya da `MOD: hızlı` (bkz. docs/CALISMA-KURALLARI.md §14 A).
> Geçmiş: `docs/checkpoints/`. Alttaki OTOMATIK bölümü `scripts/tur-sonu.ps1 -DurumYaz` yazar; elle düzenleme.

Son güncelleme: 2026-11-03 17:20 · main 71bc3a48 (push kullanıcıda) · worktree yok (D:\Pundun\speed-starts-t093 klasörü silinebilir)

## Faz 8 (cila) — bugün main'e girenler (son kopya koşusu yeşil: EditMode 1198, PlayMode 52 + 7 atlanan, Zenject)
- T-081..T-090: ok tuşları, regresyon, ekonomi v2 (ADR `ADR-2026-11-03-ekonomi-v2-referans.md`), 200m/400m/60mH/400mH, ayarlar v2,
  emote v2, Race of the Day, Practice paneli · e7d269f2 Malik'in 03.11 işi (Watch Replay, kamera, mobil UI).
- T-091 Malik'in elle ayarları builder'da + EventModal 4. sekme · T-092 Practice'te sonuç/ödül/rekor yok · T-093 High Scores'ta Race
  of the Day · T-094 Watch Replay duman testleri · T-095 lisans envanteri · T-096 FPS toleransı %0.001 · T-097 ana menü kartı · T-098 replay prefab.
- T-099 çentik iki yana (inset = max(sol, sağ), yalnız Play'de) · T-100/T-101 TextMeshPro + American Captain: 243 yazı prefab'larda
  YERİNDE çevrildi (fileID aynı; menü: Tools → Speed Stars → Convert UI Text to TMP), 2 InputField legacy (TTF), builder var olanı ezmez.

## Verilen kararlar
- Kullanıcı 03.11: "referans oyun ne ise aynısı". Ekonomi: yarış Nakit'i bitirme +10, zorluk bonusu 10 × oyuncu zorluğu, rakip bonusu
  round(10 × geçilen/toplam); rakipsiz yarış Nakit ve Mor Mücevher vermez. Giyim 500 / kafa 1.000 Nakit (varsayılan kıyafet ücretsiz,
  giyili öğeler eski kayıtta sahipli sayılır), emote 3×2.500 / 3×5.000 / 3×7.500, reklam +200 Nakit (günde 3), Süreli Bilet 300 Nakit,
  Adet Bileti Nakit'le satılmaz. Erişim: 100m / Race of the Day / Practice biletsiz; diğer yarışlar biletli (PDF'ten sapma: 100m biletsiz).
- Gerçek gelir / reklam SDK'sı / IAP şimdilik YOK (oyun Çin müşterisine; kullanıcı 03.11). Eski rekorlar sıfırlanacak (iş yok).
- TextMeshPro'ya geçiş ve font American Captain (kullanıcı 03.11, `ADR-2026-11-03-textmeshpro.md`). Kullanıcı prefab'ları ELLE düzenliyor
  (4f38e016): mevcut UI prefab'larına / Home'a builder koşmaz; değişiklik yerinde (TUZAKLAR).
- Installer bağlamaları: kalıcı onay (03.11). Teslim belgesi, telefonda APK denemesi, ana menü hızı: kullanıcı "geç" dedi.
- Önceki: tema 0 fiyat = bedava; Sky Islands premium ama bedava; ten tonları 8; başlangıç Adet Bileti 3; gün sınırı UTC;
  günlük build Mono/ARMv7, mağaza build'i IL2CPP.

## Kullanıcıdan / başkalarından bekleyen
- KULLANICI: `docs/CALISMA-KURALLARI.md` §0 ve §9'daki "TextMeshPro YOK / KULLANILMAZ" satırları (hook korumalı; metin 03.11 mesajında).
- Kullanıcı 03.11: font lisansı ve Çince yedek font "şimdilik bizi ilgilendirmiyor" (bilgi: ücretsiz sürüm kişisel kullanım; TMP 3.0.7
  cihaz fontuna düşemez). Uygulama/paket adı, keystore, URL'ler, lisans envanteri: sonra.
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
- Race sahnesini `SceneBuilder.BuildRace` ile YENİDEN KURMA (Malik'in binaları); UI prefab'larına BuildAll koşma (kullanıcının ayarları).
- TMP Essentials batchmode'da `-importPackage <proje>/Library/PackageCache/com.unity.textmeshpro@3.0.7/Package Resources/TMP Essential Resources.unitypackage`.
- PowerShell'de `git` bozuk (system32 gölgesi): git Bash'ten. Kancada `git commit` + "main" aynı komutta olmaz.

<!-- OTOMATIK:BASLA -->
_tur-sonu.ps1 2026-11-03 17:17 - otomatik blok, elle yazilan bu blokta kaybolur._

- **KAPI TEMIZ (cikis 0)**
- Acik needs: 3 (DECISION 3)
- Review kuyrugu: 0
- Goz bekleyen (merged): 93 - T-100, T-101, T-099, T-097, T-098, T-095, T-093, T-094
- in-progress: 0 / blocked: 0
- Uyari: govde-needs 0, test 4, analytics 0 (kapsam: status in-progress/review/merged)
- GIT: 1 calisma agaci (ana: main); ana agacta commit'siz .prefab/.unity/.asset: 0
- KOPYALAR: 2 (kopya.ps1 -Liste -Json)
<!-- OTOMATIK:BITIR -->
