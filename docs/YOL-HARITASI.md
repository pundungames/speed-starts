# YOL HARİTASI — Speed Stars V1 (oyunun geri kalanı)

> Kaynak: `docs/gdd/` — `Speed_Stars_V1.0_Turkce.pdf` (**hükümleri üstün**), `..._Unity_IK_Teknik_Tasarim.md`,
> `..._Developer_Yol_Haritasi_Checklist.html`. Ölçek **kilitli**: yalnız bunlar yapılır (kullanıcı, 2026-10-28).
> Mimari kararlar `docs/TEMEL-SOZLESME.md`'de; açık ürün kararları `docs/decisions/OPEN-2026-10-28-gdd-acik-kararlar.md`.
> Sayılar (3 bilet, 30 dk, 60 dk, 3/2/1, 50 …) **config asset'inde** durur; bu belgede yalnız referans.

## Kim ne yapar

| Kim | Kapsam (GDD karşılığı) |
|---|---|
| **Malik** | Koşu sistemi ve IK: girdi→adım→hız→mesafe, bacak/gövde/kol hareketi (HTML Faz 02 çekirdek, Faz 03 tamamı), rakip koşucular/NPC (Faz 04 "Rakipler"), engelli/bayrak/Medley koşu kuralları. **Girmiyoruz.** |
| **Biz** | Geri kalan her şey: iskelet, sahne akışı, bütün UI prefab'ları, ekonomi (Nakit, Mor Mücevher, Adet Bileti, Süreli Bilet), giriş kapısı, günlük hediye, 7 gün ödülü, kozmetik/karakter düzenleme, High Scores, kayıt, saat, HUD, sonuç ekranı, cila/QA. |
| **Sona (Malik ya da biz)** | Yarış başladı/bitti, SET/geri sayım, finish anında sonuç + sıra, Watch Replay, UI üstüne dokunmanın adım sayılmaması. Bağlantı `docs/ARAYUZ-MEKANIK.md` ile yapılır (Faz 7). O zamana kadar `Dev/` altında geçici tetikleyici. |

**Leaderboard var mı?** Evet. Referans ekranlarda ana ekranda ve sonuç ekranında "High scores" butonu var. PDF'te
orijinalde günlük/haftalık/sezonluk sıralama olduğu geçiyor (bunlar ödül vermiyor). Backend açık (MD §18.9) →
V1: Sticker Patch'teki modül, yerel + sahte bot oyuncularla (`reports/leaderboard-tasima-2026-10-28.md`).
**Karakter özelleştirme var mı?** Evet. PDF §2.3 kozmetik ekranında "Edit racer", kategori listesi, "Primary color"
renk seçici, kategori başına 2 premium (50 Mor Mücevher) ve 7. gün premium kafa kozmetiği var. Karakterde zaten aksesuar
yuvaları var (Serhat'ın aksesuar araçları) → `reports/kozmetik-hazirlik-2026-10-28.md`.

---

## Faz 0 — Gün 0: karar (kod yok) · durum: sürüyor
- [x] Tasarım belgeleri `docs/gdd/` altında
- [ ] `docs/TEMEL-SOZLESME.md` GDD'ye göre: dikey dilim, isim sözlüğü, katman haritası, sahne/prefab sözleşmesi, config sahipliği
- [ ] Leaderboard taşıma raporu · kozmetik hazırlık raporu
- [ ] Kullanıcının 5 sorusu + OPEN listesi (aşağıda)
- **Kapı:** sözleşme onaylı → Faz 1

## Faz 1 — İskele: boş ama düzenlenebilir oyun (≤10 task)
| # | İş | Kim | Kabul |
|---|---|---|---|
| T-001 | Ekran yönü **yatay** (Landscape L/R), referans çözünürlük 1920×1080, CanvasScaler + safe area kuralı | scene-integrator | Telefonda yatay açılır; dikeye dönmez |
| T-002 | Kompozisyon kökü: `ProjectInstaller` (ProjectContext) + sahne installer'ları, boş seam'ler | süpervizör | Boş sahneler hatasız açılır |
| T-003 | Sahneler: `Scenes/Menu/Home.unity` (Event Select, Entry Gate, hediye, 7 gün, kozmetik, High Scores = paneller) ve `Scenes/Game/Race.unity` (Malik'in koşucusu + HUD + Results paneli); Bootstrap yok (ProjectContext sahnesiz kurulur); Build Settings | scene-integrator | Home↔Race geçişi çalışır |
| T-004 | Config asset'leri PDF değerleriyle: `EconomyConfig`, `RaceEventCatalog` (10 etkinlik), `GemRewardConfig`, `SevenDayRewardConfig` (6. gün boş), `CosmeticCatalog` (iskelet) | systems + scene-integrator | Inspector'dan düzenlenir; kodda sayı yok |
| T-005 | UI çerçevesi: `UiMotionConfig` + ekran/popup yöneticisi + buton geri bildirimi (DOTween, `agent-kit/05`) | ui-agent | Panel aç/kapa reçeteyle, testli |
| T-006 | Kayıt + saat: `SaveService` (JSON dosya, atomik yazım + yedek), `ClockService` (UTC, geri alınan saati tespit) | systems-agent | Kill/restart testi; saat geri alınınca bilet üretmez |
| T-007 | Test yerleşimi (asmdef) + ilk EditMode testi | test-agent | Kopyada `unity-verify` yeşil |
| T-008 | Yer tutucu ekran prefab'ları: Home, EventSelect, EntryGate, RaceHUD, Results, DailyGift, SevenDay, Cosmetics, HighScores, ResourceBar | scene-integrator | Hepsi sahnede/prefab'ta, Inspector'dan düzenlenir |
- **Kapı:** boş oyun açılıyor, bütün ekranlar sahnede görülüyor, doğrulama yeşil.

## Faz 2 — Dikey dilim (8–12 task)
**Tek cümle:** Ana ekrandan 100m'e Adet Bileti harcayarak gir → Race sahnesinde Malik'in koşucusu + bizim HUD → (geçici)
bitiş → Sonuç ekranı → Ana ekran; bilet sayısı ve 30 dk sayacı uygulama kapanıp açılınca doğru.
- [ ] `EntryTicketService` — 0..3, 30 dk yenilenme, doluyken sayaç durur, 3'ün altına inince yeni 30 dk (PDF §1.2) + MD §6.5 testleri
- [ ] `RaceEntryGate` — tek, idempotent giriş; ücretsiz yarışta düşmez; çift tıklama iki bilet harcamaz
- [ ] Ana ekran kaynak çubukları (Mor Mücevher · Adet Bileti `2/3` + geri sayım · Süreli Bilet · Nakit)
- [ ] Etkinlik seçimi (katalogdan satır prefab'ı; biletli rozeti)
- [ ] Giriş kapısı modalı ("Adet Bileti Kullan"; Süreli Bilet aktifse gizli/gri)
- [ ] Sahne akışı Home → Race → Results → Home (önceki state sızmaz)
- [ ] Yarış HUD'u: süre/mesafe — Malik'in `RaceController`/`RunnerSim`'inden **salt okuyan** adaptör
- [ ] Sonuç ekranı: Race again · Menu · Practice · High scores · Watch replay (son ikisi şimdilik pasif)
- [ ] `Dev/` geçici bitiş tetikleyicisi (`IRaceSession` seam'inin sahte uygulaması, `#if UNITY_EDITOR || DEVELOPMENT_BUILD`)
- **Kapı:** döngü baştan sona oynanır; bilet testleri yeşil. **Burada GDD'nin yetkisi biter** (sonrası kod + asset).

## Faz 3 — PDF ekonomisi (tamamı)
- [ ] `TimedTicketService` — gerçek zamanda 60 dk, oyun kapalıyken de akar; aktifken ikinci kullanım yok; yarış ortasında dolarsa yarış sürer
- [ ] `CashWallet` + `PurpleGemWallet` — ayrı bakiye, işlem günlüğü, `PayoutTransactionId` ile çift ödül yok
- [ ] `RaceRewardResolver` — Mor Mücevher yalnız 400m / 60mH / Medley tek yarış sırası 1-2-3; diğer etkinlikler 0
- [ ] Adet Bileti doluyken günlük/reklam/Nakit edinimi pasif + "Adet Bileti Dolu" (fiyatlar OPEN → config boş)
- [ ] Ekonomi uç durum regresyonu (HTML Faz 08: 3 sınırı, 30 dk, 1 saat, yarış ortası dolma, 50 fiyat, çift işlem)

## Faz 4 — Günlük ödüller
- [ ] Giriş Hediyesi popup'ı: günün ilk girişi, CLAIM → 1 Adet Bileti (doluysa yok), kapanınca bilet ortadan kaynak çubuğuna **bir kez** uçar
- [ ] 7 Günlük Giriş Ödülü: ana ekran ikonu + kırmızı nokta, panel, G1-G5 + G7 config'ten; **G6 boş (OPEN)**
- [ ] Gün sıfırlama / kaçırılan gün kuralı (OPEN; varsayım config'te değiştirilebilir)

## Faz 5 — Kozmetik ve karakter düzenleme
> Hazır olan (`reports/kozmetik-hazirlik-2026-10-28.md`): karakter prefab'ında `Accessories/` altında **8 kategori** —
> Headwear, Glasses, Tops, Gloves, Shorts, Leggings, Socks, Shoes — her birinde **7 normal + 2 premium** set, hepsi aynı
> iskelete skin'li (IK'yi kendiliğinden izler), renk 3 toon materyalinin `_Color`'ından (MaterialPropertyBlock ile).
> GDD'nin "kategori başına 2 premium" kuralına birebir uyuyor. **Risk:** eski temel kıyafet (Tshirt, Short, Leggings,
> Shoes, Glove, Helmet) FBX'ten çıkmış olabilir → Unity'de Missing mesh kontrolü (Serhat/Malik).
- [ ] `CosmeticCatalog`: bu 8 kategori × 9 set; premium bayrağı ve fiyat asset'te
- [ ] Edit racer ekranı: kategori sütunu, öğe ızgarası (kilitli/sahip), Primary color seçici + hazır renkler, önizleme kaidesi, Back
- [ ] Premium: listede en üstte, parlak panel, 50 Mor Mücevher, tek seferlik idempotent satın alma
- [ ] `CosmeticApplier` — **Malik'in prefab'ına dokunmadan** yuvaları açar/kapar, renk atar; seçim kayıtta
- [ ] "Custom 1" yarışçı ön ayar seçici (ana ekran)
- [ ] Öğe ikonları: yok — Serhat'ın stüdyosu tam boy PNG çekiyor, ikon değil → ikon çekim aracı (biz) ya da Serhat
- [ ] Premium trail/partikül: çalışma anında kemiğe (B-foot, B-head) bağlanır, `RunReset`'te temizlenir; efekt asset'leri yok (Serhat)
- [ ] 7. gün ödülü = premium kafa kozmetiği bağlantısı

## Faz 6 — High Scores (leaderboard)
> `reports/leaderboard-tasima-2026-10-28.md`: alınabilirlik **orta**. Arayüz ve sanat temiz alınır (satır, liste, isim ve
> bayrak penceresi, 68 bayrak, Nunito). Ama Sticker Patch'in "sahte tablosu" bir kayıt listesi değil, toplam puandan
> hesaplanan sıra (`StickerPatchFakeLeaderboard.cs:34-37`) → **çekirdek yarış süresine göre yeniden yazılır**. Kaynakta
> 5 yerde UI koddan kuruluyor, panel dikey (1080×2400) — ikisi de bizde düzeltilerek alınır. Ücretli UI paketi
> referansları (GUI PRO Kit, CasualFantasy; 25 yer) lisans kontrolü ister.
- [ ] L-1 Alan katmanı: kayıt = etkinlik + süre (düşük iyi) + dönem (günlük/haftalık/tüm zamanlar), yerel backend, sahte bot süre dağılımı (dağılım değerleri config'te, kullanıcı verir) + testler — systems-agent
- [ ] L-2 Sanat taşıma: TMP Essentials, Nunito, 68 bayrak, sprite'lar .meta'larıyla (329 GUID, çakışma yok) — scene-integrator
- [ ] L-3 View'lar: panel, ilk üç, satır, kaydırmalı liste, isim düzenleme, bayrak seçici — kod UI kurmaz — ui-agent
- [ ] L-4 Prefab'lar yatay yerleşimle + Home sahnesine bağlama — scene-integrator
- [ ] L-5 Installer bağlamaları, Home/Results "High scores" butonları, yarış sonucu adaptörü (gerçek süre Faz 7'ye bağlı) — süpervizör

## Faz 7 — Mekanikle bağlama (sona bırakıldı)
- [ ] `docs/ARAYUZ-MEKANIK.md`: `IRaceSession` (yarışa hazırla → SET/geri sayım → başladı → bitti(süre, mesafe, sıra)), girdi kilidi, mesafe (`RaceEventDefinition` → koşu sistemi)
- [ ] Adet Bileti düşme anı = giriş onayı / geri sayım başı (PDF §1.2) — `RaceEntryConfirmed`
- [ ] Sonuç ekranına gerçek süre ve sıra; Mor Mücevher gerçek sıradan
- [ ] UI üstüne dokunma adım sayılmaz (Malik'in `RunnerInput`'u) · Watch Replay (Malik'in `RunnerReplay`'i?)
- [ ] 200m / 400m ve engelli/bayrak/Medley etkinlikleri — kuralları Malik'le (GDD'de AÇIK)

## Faz 8 — Cila, cihaz, teslim
- [ ] Ayarlar/ses/grafik butonları (ana ekrandaki üç ikon), ses ve titreşim geri bildirimi
- [ ] Safe area / farklı en-boy oranları, 30/60/120 FPS'te UI, kill/restart, saat kayması, çevrimdışı
- [ ] Mobil profil (UI, efekt), APK öncesi tam test turu
- [ ] Teslim: build, README, test raporu, kalan açık kararlar

---

## Kapsam dışı (GDD'de tanımı yok — soru 1-2)
Upgrade (Power/Stamina/Height) · Players (çok oyunculu) · Emote · online leaderboard · gerçek reklam/IAP SDK.
Ekran görüntülerinde butonları var; PDF/MD bunları tanımlamıyor.

## Sorular (mimariyi değiştirenler)
1. **Upgrade + Power/Stamina** V1'de var mı? Öneri: buton görünür ama pasif; sistem yok (koşu sistemine dokunur, GDD'de yok).
2. **Players** butonu? Öneri: yok ya da pasif.
3. **High Scores** yerel + sahte bot (Sticker Patch gibi), online sonra — doğru mu?
4. **Kozmetik**: setler hazır (8 kategori × 7+2). Eksik olan **ikonlar** ve **premium trail/partikül** — Serhat mı yapar,
   ikon çekim aracını biz mi yazalım?
5. **Reklam/IAP**: V1'de gerçek SDK mı? Öneri: arayüz + sahte uygulama; SDK sonra.
