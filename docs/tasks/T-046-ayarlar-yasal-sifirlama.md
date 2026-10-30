---
id: T-046
title: Ayarlar - Privacy Policy / Terms of Use / Support baglantilari + Reset Progress / Reset Personal Bests (onay penceresiyle)
status: review
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
  - "Assets/Speed-Staes/Tests/EditMode/Meta/Settings/LinkRulesTests.cs"
  - "Assets/Speed-Staes/Tests/EditMode/UI/Settings/ResetConfirmFlowTests.cs"
  - "Assets/Speed-Staes/Tests/PlayMode/SettingsResetSmokeTests.cs"
analytics:
  - "yok: projede analytics yok"
needs:
  - tip: WIRING
    durum: open
    not: "ProjectInstaller -> Container.Bind<IProgressReset>().To<SpeedStars.Meta.Settings.ProgressResetService>().AsSingle(); (ProjectInstaller.cs, next to the IPlayerSettings bind; needs ProgressRepository, already bound). Without it the two reset rows are greyed out."
  - tip: SCENE
    durum: open
    not: "Rebuild SettingsPopup prefab with the builder (UiSettingsPrefabs): 5 new rows (PrivacyRow, TermsRow, SupportRow, ResetBestsRow, ResetAllRow, each with a Hit child = SettingsActionRowView) + PopupLayer/ConfirmDialog (ConfirmDialogView); panel_single_9slice gets its 9-slice border. Check no field is fileID 0 in the prefab. Then run SettingsResetSmokeTests and take Home_settings."
  - tip: DECISION
    durum: open
    not: "Privacy Policy / Terms of Use / Support URL'leri verilmedi: SettingsConfig'te bos; bos satir pasif. Kullanici URL'leri verince config'e yazilir"
started: 2026-10-30
finished: 2026-10-30
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
- Yapildi: ProgressRepository.ResetPersonalBests/ResetAll (yeni oyuncu, ayarlar + LastSeenUtcTicks korunur, korunan kayit degistirilir), IProgressReset + ProgressResetService, SettingsConfig'e 3 URL (GetLinkUrl; bos/gecersiz = ""), LinkRules (yalniz http/https).
- UI: SettingsActionRowView (satir uzerinde seffaf buton, pasifse row_disabled), ConfirmDialogView (SettingsPopup PopupLayer'inda kendi dim'i), ResetConfirmFlow (saf: PB 1 adim, All 2 adim; son adim kirmizi RESET). Tam sifirlamadan sonra SceneFlow.GoTo(Home); SceneFlow yoksa popup kapanir.
- Builder: 5 yeni satir + ConfirmDialog; satir ikonlari mevcut sanattan (link: row_language_icon, reset: row_player_icon), ok = dropdown_arrow_down 90 derece. Kirmizi dugme: field_normal sprite + kirmizi tint (sanatta kirmizi dugme yok).
- Zorlandigim yer: servis/view bagimliliklari [InjectOptional] (baglama gelmeden eski ekran bozulmasin); URL'ler bos oldugu icin link satirlari su an gri (DECISION acik).
- Calistirilmadi: Unity/test yok; derleme ve PlayMode dogrulamasi sasiye ait. Risk: PlayMode testi IProgressReset baglanmadan kosarsa satir pasif kalir.
