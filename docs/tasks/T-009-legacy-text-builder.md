---
id: T-009
title: UI prefab builder'i TextMeshPro'dan legacy Text'e, metinler Ingilizce
status: merged
owner: ui-agent
depends_on: [T-007]
owns_paths:
  - "Assets/Speed-Staes/Tools/Editor/Builders/Ui/**"
touches_scene: false
branch: agent/faz2-dilim
commits: [bb43cdb, dc7db07]
tests:
  - "gerekmiyor: editor builder'i; kabul supervizorun batchmode kosusu (overwrite) + gozle"
analytics:
  - "yok: projede analytics yok"
needs: []
started: 2026-10-28
finished: 2026-10-28
blocked_reason:
---

## Hedef
Kullanıcı kararı (28.10): **TextMeshPro yok, her yerde legacy `UnityEngine.UI.Text`**; oyun Çin pazarına (legacy Text'in dinamik fontu
CJK'yı işletim sistemi fontundan alır). Oyuncuya görünen metin **İngilizce**. TMP paketi ve `Assets/TextMesh Pro` bu dalda silindi
(`Packages/manifest.json`, `SpeedStars.UI.asmdef`). `UiBuildKit.cs`, `UiRowPrefabs.cs`, `UiScreenPrefabs.cs` TMP'ye bağlı — derlenmiyor.

## Kabul kriterleri
- [ ] Builder'da `TMPro`/`TextMeshProUGUI`/`TMP_Settings` referansı kalmaz; etiketler `Text` (font `Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")`,
      hizalama/boyut eşdeğer, `resizeTextForBestFit` yalnız gerekli yerde). "TMP Essentials eksik" kontrolü kalkar.
- [ ] Bütün metinler İngilizce (ör. "Use Entry Ticket", "Timed Ticket", "7-Day Rewards", "Day N", "Watch ad", "Get tickets", "Daily / Weekly /
      All time", "Welcome back! Claim it and go to start your challenge!", "CLAIM"). Orijinal referans: `docs/gdd/referans-ekranlar/`.
- [ ] Yerleşim yatay **2400×1080** referansına göre (CanvasScaler yükseklik eşleşmesi = 1; genişlik telefonlarda değişir): kenara yapışan öğeler
      anchor'la köşeye bağlı, orta öğeler ortalı; 1920 genişliğinde de taşmaz.
- [ ] `overwrite` davranışı aynen (false: var olana dokunma; true: aynı yola yaz, GUID korunur).
- [ ] Hiyerarşi adları değişmez (Faz 2 view'ları ve T-014 bağlaması bu adlarla `[SerializeField]` bağlanacak); değişen ad varsa çalışma notuna yaz.

## Çalışma kuralları
- Çalışma klasörü MUTLAK: `D:\Pundun\speed-starts-faz2` (worktree, dal `agent/faz2-dilim`). Ana klasöre yazma.
- Önce oku: `docs/CALISMA-KURALLARI.md` §9 (legacy Text kuralı), `docs/TUZAKLAR.md`, mevcut `Tools/Editor/Builders/Ui/*.cs`, `UiPrefabPaths.cs`.
- Paralelde: systems-agent `Scripts/Meta/Tickets|Race|Wallet/**`, ui-agent'lar `Scripts/UI/Home/**` ve `Scripts/UI/Race/**`, gameplay-agent
  `Scripts/Bridge/**`. Onlara yazma.
- Unity/batchmode/verify YASAK, `git commit` YASAK, yalnız `.cs`. Kendi derleme kontrolünü scratchpad'de Unity'nin Roslyn'iyle yapabilirsin
  (önceki T-007 betiği: `C:\Users\Monster\AppData\Local\Temp\claude\d--Pundun-speed-starts\010558a9-f012-4284-b1f2-1285cf76ea87\scratchpad\compile\builder.rsp`).
- Bitince `status: review`, `## Çalışma notu` 5 satır.

## Çalışma notu
- UiBuildKit/UiRowPrefabs/UiScreenPrefabs: TMP kalktı, Label artık legacy Text (LegacyRuntime.ttf, BoldAndItalic, best fit yalnız >2 karakterli metinde, Wrap); TMP Essentials kontrolü silindi.
- TMP margin karşılığı yok: yeni Kit.Margin(Text,l,t,r,b) rect ofsetini içeri çeker; TextButton/IconButton aynı toplam iç boşluğu korur.
- Metinler İngilizce (Day N, 7-Day Rewards, Use Entry Ticket, Left, Timed Ticket, Get tickets, Cash, Watch ad, Ticket, uzun Welcome back metni). DailyGift penceresi 900x700, başlık iki satır.
- Yerleşim zaten köşe/orta anchor'lıydı; 2400x1080 yorumu güncellendi, 1920'de taşan sabit öğe yok. Hiyerarşi adı DEĞİŞMEDİ.
- Roslyn ile Ui/*.cs + UiPrefabPaths derlendi, hata 0 (Unity çalıştırılmadı). Risk: best fit + Wrap bazı çok kelimeli etiketleri tek satır yerine iki satıra bölebilir; gözle bakılmalı.
