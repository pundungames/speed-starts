---
id: T-005
title: UI cercevesi (UiMotionConfig, ekran/popup, buton geri bildirimi, sahne akisi)
status: merged
owner: ui-agent
depends_on: [T-002]
owns_paths:
  - "Assets/Speed-Staes/Scripts/UI/Framework/**"
  - "Assets/Speed-Staes/Scripts/UI/Flow/**"
  - "Assets/Speed-Staes/Tests/EditMode/UI/**"
touches_scene: false
branch: agent/faz1-iskele
commits: [c3dbe46]
tests:
  - "Assets/Speed-Staes/Tests/EditMode/UI/ScreenNavigatorTests.cs"
  - "Assets/Speed-Staes/Tests/EditMode/UI/SceneFlowTests.cs"
  - "Assets/Speed-Staes/Tests/EditMode/UI/UiStaggerTests.cs"
analytics:
  - "yok: projede analytics yok"
needs:
  - tip: WIRING
    durum: closed
    not: "ScreenNavigator, SceneFlow, UiMotionConfig -> ProjectInstaller / sahne installer'lari (T-008). ProjectInstaller: UiMotionConfig asset'i BindInstance; [SerializeField] SceneFlowScenes alani -> BindInstance; ISceneLoader -> UnitySceneLoader AsSingle; SceneFlow AsSingle; DOTween.Init + SetCapacity (kit 05 s9). Home/RaceSceneInstaller: ScreenNavigator AsSingle (sahne basina) + acilista kok ekrani navigator.Push(<kok UiScreen>) ile yigina koyan cagri (IInitializable; Faz 2'de HomeScreen view'u da yapabilir). IButtonFeedbackHook bind'i yok (opsiyonel, bos kalabilir) -> T-008 Installers (ProjectInstaller, SceneUiInstaller)"
  - tip: WIRING
    durum: closed
    not: "Cihaz geri tusu -> ScreenNavigator.Back(). SpeedStars.UI asmdef'inde Unity.InputSystem referansi yok, okuyucu yazilmadi: Installers'ta (Assembly-CSharp) ITickable ile Keyboard.current.escapeKey.wasPressedThisFrame -> sahnenin ScreenNavigator.Back(); false donerse (kokte, popup yok) bir sey yapma. Alternatif: T-002 asmdef'ine Unity.InputSystem eklenir, okuyucu UI'da yazilir -> T-008 Installers (ProjectInstaller, SceneUiInstaller)"
  - tip: ASSET
    durum: closed
    not: "Data/Config/UiMotionConfig.asset ornegi (T-006): Create > Speed Stars > UI > Ui Motion Config; alan varsayilanlari kit 05 s1 degerleri, oldugu gibi kalabilir -> Faz1Builder batchmode (kopya) + GeriAl, agent/faz1-ui"
  - tip: SCENE
    durum: closed
    not: "T-007/T-006 prefab ve sahnelerinde: ekran prefab kokune UiScreen, popup kokune UiPopup (CanvasGroup kendiliginden eklenir); sahnede yalniz kok ekran aktif, digerleri ve popup'lar kapali baslar. Her Button nesnesine ButtonFeedback (Button'in kendi nesnesine; Punch Target istege bagli cocuk). Her sahnede UI Root'a SceneTransitionBlocker; en ustteki canvas'ta tam ekran, raycast target'li, alpha 0 Image'li kapali bir cocuk (TransitionBlocker) -> SceneTransitionBlocker.blocker alanina bagla. Home/Race sahne adlari Build Settings'te SceneFlowScenes ile ayni olmali (varsayilan Home / Race) -> Faz1Builder batchmode (kopya) + GeriAl, agent/faz1-ui"
started: 2026-10-28
finished: 2026-10-28
blocked_reason:
---

## Hedef
Bütün ekranların kullanacağı UI çerçevesi (namespace `SpeedStars.UI.Framework`, `SpeedStars.UI.Flow`). Ekranlar sahnede prefab
olarak durur (`docs/TEMEL-SOZLESME.md` §5); bu task onları açıp kapatan, geçişleri canlandıran ve sahneler arasında gezdiren
kodu yazar. **UI kodla kurulmaz**: view'lar hazır nesneleri `[SerializeField]` ile alır.

## Kabul kriterleri
- [x] `UiMotionConfig` (ScriptableObject): panel aç/kapa, popup, stagger, buton punch, sayaç süreleri ve eğrileri
      (`agent-kit/05-DOTWEEN-UI-ANIMASYON.md` reçeteleri). Kodda süre sabiti yok.
- [x] `UiScreen` / `UiPopup` taban bileşenleri: `CanvasGroup` + `RectTransform`; `Show()` / `Hide()` reçeteyle; açılırken etkileşim
      kapalı, bitince açık; yarıda kesilen animasyon `Kill` edilir; aynı anda iki kez açma güvenli.
- [x] `ScreenNavigator` (saf C#, Zenject ile bağlanacak): ekran yığını (Push / Pop / Replace / Back), popup katmanı ayrı;
      geri tuşu önce en üst popup'ı kapatır. Ekranlar bir arayüzle soyutlanır, test sahnesiz koşar.
- [x] `ButtonFeedback`: tıklamada punch scale (UiMotionConfig'ten); ses/titreşim için boş bırakılabilir bir kanca (arayüz).
- [x] `SceneFlow`: Home ↔ Race geçişi (sahne adları/indeksleri dışarıdan), çift istek engellenir, geçiş sırasında girdi kilitli.
      Sahne yükleyici arayüzle soyutlanır (test için).
- [x] DOTween yalnız **çekirdek API** ile (`DOTween.To`, `Sequence`, `Transform` kısayolları). `CanvasGroup.DOFade` gibi **modül**
      uzantıları KULLANILMAZ: modüller asmdef'siz (firstpass) ve `SpeedStars.UI` asmdef'i onları göremez (sözleşme §3).
- [x] EditMode testleri (`Tests/EditMode/UI/...`): ScreenNavigator yığın davranışı, popup önceliği, SceneFlow çift istek engeli.
- [x] Geri bildirim bölümü dolu (aşağıda).

## Geri bildirim
GORSEL  : panel/popup aç-kapa reçetesi, buton punch
SES     : kanca var, ses asset'i yok (sonraki faz)
DOKUNSAL: kanca var, uygulama yok
ZAMAN   : yok
KAMERA  : yok

## Kapsam dışı
- Ekranların kendisi (Home, Event Select …), prefab'lar (T-007), installer (T-008), ses sistemi.

## Çalışma kuralları
- Çalışma klasörü MUTLAK: `D:\Pundun\speed-starts-faz1` (worktree, dal `agent/faz1-iskele`). Ana klasöre yazma.
- Önce oku: `docs/CALISMA-KURALLARI.md`, `docs/TEMEL-SOZLESME.md` §3-§5, `docs/TUZAKLAR.md` (özellikle DOTween/asmdef maddeleri),
  `agent-kit/02-MIMARI-STANDARTLARI.md`, `agent-kit/03-HIS-VE-GERI-BILDIRIM.md`, `agent-kit/05-DOTWEEN-UI-ANIMASYON.md`,
  `Assets/Speed-Staes/Scripts/Core/Runtime/**` (DEĞİŞTİRME). Referans ekranlar: `docs/gdd/referans-ekranlar/`.
- Unity, batchmode, `unity-verify` YASAK. `git commit` YAPMA. `.meta` yazma.
- Bitince `status: review`, `tests:` dolu, `## Çalışma notu` 5 satır.

## Çalışma notu
1. Framework: `UiMotionConfig` (+`UiTransition`, `UiStagger`), `UiPanel` → `UiScreen`/`UiPopup`, `ScreenNavigator` (+`IUiView`), `ButtonFeedback` (+`IButtonFeedbackHook`); Flow: `SceneFlow`, `SceneFlowScenes`, `GameScene`, `ISceneLoader`/`UnitySceneLoader`, `SceneTransitionBlocker`. DOTween yalnız çekirdek (`DOTween.To`, `Sequence`, `DOScale`, `DOPunchScale`), süreler config'te.
2. Test: 31 EditMode testi (navigator 16, SceneFlow 9, stagger 6), Unity'de koşulmadı. Scratchpad'de asmdef referanslarıyla Roslyn derlemesi temiz (test assembly DOTween/Zenject'siz derlendi); saf testler NUnit taklidiyle 30/30 geçti, LogAssert'li 1 test Unity'siz koşamadığı için atlandı.
3. Karar: girdi kilidi = `SceneFlow.IsTransitioning` + sahnedeki tam ekran blocker nesnesi (kök CanvasGroup değil: overrideSorting'li iç canvas'ları kapsamaz, `interactable=false` butonları gri boyar). Ekran yığınında bir ekran tek kez durur; yığında olana Push geri sarar. Popup'lar ekran hareketinden etkilenmez.
4. Bırakılan: geri tuşu okuyucusu (UI asmdef'inde Input System yok, WIRING); stagger ve sayaç oynatıcısı (config alanları + `UiStagger` hazır, okuyucuları liste/sayaç ekranlarıyla); devre dışı butonda shake yok (kitte değer yok), yalnız `hook.Denied`; damga (stamp) config'e alınmadı (task listesinde yok).
5. Gözle bakılacak: `UiPanel`/`ButtonFeedback` (çift açma, yarıda kesme, açılırken girdi kapalı) EditMode'da test edilmedi (DOTween testten görünmez); Play'de panel aç/kapa spam ve buton punch ile bakılmalı. TUZAKLAR §12'ye 2 satır eklendi.
