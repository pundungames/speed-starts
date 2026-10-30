---
id: T-046
title: Ayarlar - Privacy Policy / Terms of Use / Support baglantilari + Reset Progress / Reset Personal Bests (onay penceresiyle)
status: todo
owner: ui-agent
depends_on: [T-031]
owns_paths:
  - "Assets/Speed-Staes/Scripts/UI/Settings/**"
  - "Assets/Speed-Staes/Scripts/Meta/Settings/**"
  - "Assets/Speed-Staes/Scripts/Meta/Contracts/SettingsContracts.cs"
  - "Assets/Speed-Staes/Scripts/Meta/Persistence/ProgressRepository.cs"
  - "Assets/Speed-Staes/Tools/Editor/Builders/Ui/UiSettingsPrefabs.cs"
  - "Assets/Speed-Staes/Tools/Editor/Builders/Scene/SettingsConfigSeed.cs"
  - "Assets/Speed-Staes/Tests/EditMode/Meta/Settings/**"
  - "Assets/Speed-Staes/Tests/EditMode/Meta/Persistence/**"
  - "Assets/Speed-Staes/Tests/EditMode/UI/Settings/**"
  - "Assets/Speed-Staes/Tests/PlayMode/**"
touches_scene: true
branch: agent/T-046-ayarlar-yasal
commits: []
tests:
  - "Assets/Speed-Staes/Tests/EditMode/Meta/Persistence/ProgressResetTests.cs"
analytics:
  - "yok: projede analytics yok"
needs:
  - tip: DECISION
    durum: open
    not: "Privacy Policy / Terms of Use / Support URL'leri verilmedi: SettingsConfig'te bos; bos satir pasif. Kullanici URL'leri verince config'e yazilir"
started:
finished:
blocked_reason:
---

## Hedef
`docs/eksik-ui/README.md` B-9 ve öncelik 1 (mağaza için şart). Ayarlar penceresine (T-031, aynı satır prefab'ı, aynı sanat) yeni satırlar:
- **Privacy Policy**, **Terms of Use**, **Support**: dokununca `Application.OpenURL(config URL)`. URL'ler `SettingsConfig`'te; boşsa satır
  pasif (gri). Satır sağında var olan bir ok/ikon (settings sanatındaki dropdown oku).
- **Reset Personal Bests**: onay penceresi → bütün etkinlik rekorları ve gün en iyileri silinir (liderlikte YOU "NOT RANKED" olur).
- **Reset All Progress**: iki adımlı onay ("Are you sure?" → "This cannot be undone" + kırmızı RESET) → kayıt yeni oyuncu hâline döner
  (başlangıç bakiyeleri config'ten; ayarlar — müzik/ses/dil/bayrak/ad — korunur), sonra Home yeniden yüklenir.
- Onay penceresi: settings sanatıyla (panel_single_9slice / header / done butonları) kurulan küçük popup prefab'ı (builder); metinler legacy
  Text, İngilizce.
Sıfırlama mantığı Meta'da tek yerde (`IProgressReset` ya da `ProgressRepository.ResetToNew(keepSettings)`), EditMode testli.
Kod kalıcı UI kurmaz; dokunma alanları görünür bölge; DOTween çekirdek (TUZAKLAR).

## Kabul kriterleri
- [ ] EditMode: tam sıfırlama = yeni oyuncu hâli + ayarlar korunur; rekor sıfırlama yalnız rekor/gün en iyilerini siler
- [ ] PlayMode: Ayarlar → Reset Personal Bests → onay → kapanır (gerçek ışınlı Tap); ekran görüntüsü `Home_settings` yeni satırlarla
- [ ] needs: DECISION (URL'ler) açık kalır

## Calisma notu
(Agent doldurur)
