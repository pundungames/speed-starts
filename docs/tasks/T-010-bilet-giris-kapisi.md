---
id: T-010
title: EntryTicketService, Wallet, RaceSelection, RaceEntryGate (Meta, testli)
status: merged
owner: systems-agent
depends_on: [T-003, T-004]
owns_paths:
  - "Assets/Speed-Staes/Scripts/Meta/Tickets/**"
  - "Assets/Speed-Staes/Scripts/Meta/Race/**"
  - "Assets/Speed-Staes/Scripts/Meta/Wallet/**"
  - "Assets/Speed-Staes/Tests/EditMode/Meta/Tickets/**"
  - "Assets/Speed-Staes/Tests/EditMode/Meta/Race/**"
  - "Assets/Speed-Staes/Tests/EditMode/Meta/Wallet/**"
touches_scene: false
branch: agent/faz2-dilim
commits: []
tests:
  - "Assets/Speed-Staes/Tests/EditMode/Meta/Tickets/EntryTicketServiceTests.cs"
  - "Assets/Speed-Staes/Tests/EditMode/Meta/Race/RaceEntryGateTests.cs (gate, RaceSelection, WalletService)"
analytics:
  - "yok: projede analytics yok"
needs:
  - tip: WIRING
    durum: closed
    not: "EntryTicketService -> IEntryTickets, WalletService -> IWallet, RaceSelection -> IRaceSelection, RaceEntryGate -> IRaceEntryGate (ProjectInstaller AsSingle, T-014) -> T-014a/b builder + Installers (agent/faz2-dilim), Zenject validate 2 sahne OK"
started: 2026-10-28
finished: 2026-10-28
blocked_reason:
---

## Hedef
Dikey dilimin Meta çekirdeği. Arayüzler HAZIR ve sabit: `Assets/Speed-Staes/Scripts/Meta/Contracts/MetaContracts.cs` (DEĞİŞTİRME;
değişmesi gerekiyorsa BLOCKED yaz). Uygulamalar saf C#, Zenject'siz, yapıcı enjeksiyonu: `ProgressRepository` (T-003, `Current` her
okumada), `IClock`, `EconomyConfig`, `RaceEventCatalog` (T-004).

## Kabul kriterleri
- [ ] `EntryTicketService : IEntryTickets` (`SpeedStars.Meta.Tickets`): PDF §1.2 — üst sınır, gerçek zamanlı yenilenme (oyun kapalıyken de),
      doluyken sayaç durur, sınırın altına inince yeni tam aralık başlar; birden çok aralık geçmişse hepsi uygulanır ama sınırı aşmaz.
      `PlayerProgress.EntryRegenDueUtcTicks == 0` = sayaç çalışmıyor. Saat geri alınırsa (IClock zaten geri gitmez) bilet üretmez.
      Harcama (`TrySpend`) servisin public metodu olabilir; giriş kapısı onu kullanır.
- [ ] `WalletService : IWallet` (`SpeedStars.Meta.Wallet`): PlayerProgress'ten okur; Süreli erişim aktifliği `TimedAccessExpiryUtcTicks > now`.
- [ ] `RaceSelection : IRaceSelection` (`SpeedStars.Meta.Race`): başlangıçta katalogdaki ilk **oynanabilir** etkinlik.
- [ ] `RaceEntryGate : IRaceEntryGate`: bilinmeyen → UnknownEvent; oynanamaz → NotPlayable; `RequiresTicket` false → EnteredFree; Süreli erişim aktif →
      EnteredWithTimedAccess (harcamaz); bilet varsa 1 düşer → EnteredWithTicket ve **hemen kaydeder** (`ProgressRepository.Save`); yoksa NoTicket.
      Bekleyen giriş varken aynı etkinliğe tekrar `Enter` aynı sonucu ödemeden döner (çift tıklama); `TryTakePending` tek seferlik.
- [ ] Değişiklikte `Changed` olayları tetiklenir (bilet sayısı, sayaç).
- [ ] EditMode testleri (MD §6.5 tablosundaki ilişkiler, sayılar asset'ten/test kurulumundan, PDF sayısı sabit assert edilmez): 2→1 harcama;
      sınırdayken zaman geçse de artmaz; sınırdan bir aşağı inince yeni aralık; kapalıyken N aralık geçince sınıra kadar; çift Enter tek ödeme;
      ücretsiz etkinlikte harcama yok; süreli erişimde harcama yok; NoTicket durumu değiştirmez. Sahte IClock + bellek içi ISaveStore.
- [ ] Meta Zenject'e referans vermez; MonoBehaviour yok; kodda denge sayısı yok.

## Çalışma kuralları
- Çalışma klasörü MUTLAK: `D:\Pundun\speed-starts-faz2` (worktree, dal `agent/faz2-dilim`). Ana klasöre yazma.
- Önce oku: `docs/CALISMA-KURALLARI.md`, `docs/TEMEL-SOZLESME.md` §4-§6, `docs/decisions/ADR-2026-10-28-kayit-bicimi.md`, `docs/TUZAKLAR.md`,
  `Scripts/Meta/Persistence|Time|Config/**`, PDF §1.1-1.2 (`docs/gdd/Speed_Stars_V1.0_Turkce.pdf`), MD §6.
- Paralelde ui-agent'lar `Scripts/UI/**`, gameplay-agent `Scripts/Bridge/**` yazıyor; onlar senin arayüzlerini (Contracts) kullanıyor.
- Unity/batchmode/verify YASAK, `git commit` YASAK, `.meta` yazma. Derleme kontrolü scratchpad'de Unity'nin Roslyn'iyle yapılabilir.
- Bitince `status: review`, `tests:` dolu, `## Çalışma notu` 5 satır.

## Çalışma notu
- Yapıldı: EntryTicketService (+public TrySpend), WalletService (+NotifyChanged), RaceSelection, RaceEntryGate; hepsi saf C#, denge sayısı yok.
- Regen tembel: her okumada Refresh; geçen aralıklar tek aritmetikle uygulanır, sınırda sayaç 0; Refresh durum değişince Save eder.
- Gate somut EntryTicketService alır (TrySpend arayüzde yok); Contracts'a dokunulmadı.
- Testler yazıldı ama Unity/derleme koşulmadı (yasak); test yardımcıları TicketTestKit.cs (FakeClock, MemoryStore).
- Açık: WIRING (ProjectInstaller, T-014). Aynı EntryTicketService örneği hem IEntryTickets hem gate'e verilmeli.
