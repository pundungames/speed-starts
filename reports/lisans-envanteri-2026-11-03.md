# Lisans envanteri - 2026-11-03 (T-095)

Hukuki hüküm değildir. Her kalem "kanıt var / kanıt yok / incelenmeli" diye işaretlidir. Risk: yok / düşük / incelenmeli / yüksek.

## Yöntem
- Kökler: `ProjectSettings/EditorBuildSettings.asset` -> `Home.unity` (b20fa4eb) + `Race.unity` (eb89111c); `Assets/**/Resources/**` (ProjectContext, DOTweenSettings, 7 materyal, Control Rig.prefab, Zenject MPM Editor asset'i).
- Sahne/prefab/asset YAML'ındaki `guid:` -> `.meta` -> yol, özyinelemeli (betik: scratchpad `scan.py`; yalnız dosya taraması, AssetDatabase yok). 2899 dosya erişildi (130 .cs, 1592 .asset, 543 .png, 314 .mat, 289 .prefab, 17 .shader, 8 .wav, 2 .fbx, 1 .ttf, 1 .dll).
- Sınır: yalnız GUID bağıyla erişim. Koddan string yolla yüklenen şey (Resources.Load, vb.) yakalanmaz; projede `Resources.Load`/Addressables yok (TEMEL-SOZLESME:300), bu yüzden risk düşük. PNG'lerin içeriğine (gömülü logo vb.) bakılmadı - bkz. madde 2.

## Özet: incelenmeli / yüksek

1. **Ürün kimliği yer tutucu**: `productName: speed-starts`, `companyName: DefaultCompany`, Android id `com.DefaultCompany.speed-starts`. Oyuncuya görünen metinde, sahne/prefab/Data asset'lerinde "Speed Stars" geçmiyor (tarandı). Ama oyun adı, repo, klasör (`Speed-Staes`), namespace (`SpeedStars`) ve editör menüleri referans oyunun adı. Mağazaya çıkış adı ve marka kararı yok -> **incelenmeli**.
2. **UI sanatı (543 PNG, 11 ekran klasörü) kaynak kanıtı yok**: `UI/Art/*` altında yalnız teslim README'leri/`layout.json` var ("PNG'ler RGBA, metin gömülü değil"); kim/ne üretti, lisansı ne, projede yazmıyor. Ekranlar referans oyunun (docs `orijinal-*.jpg`, `eksik-ui`) düzenini izliyor -> görünüm/trade dress benzerliği -> **incelenmeli**. Referans oyundan kırpılmış sanat kanıtı bulunmadı (ama PNG'ler tek tek görsel olarak incelenmedi).
3. **Mixamo emote animasyonları**: 9 FBX (`Animations/*.fbx`, içinde `mixamorig`/Mixamo/Adobe izi) repo'da duruyor; build'e giren yalnız pişmiş `Animations/Baked/*.asset` (türetilmiş veri). Ham FBX build'e girmiyor. Mixamo koşulları ham animasyonun yeniden dağıtımını sınırlayabilir -> repo herkese açıksa **incelenmeli**. Emote adlarından "Gangnam Style" / "Shopping Cart Dance" / "Guitar Playing" klip adı olarak asset'lerde (`m_Name`, `clipName`) duruyor; oyuncuya görünen adlar "Horse Trot", "Cart Dance", "Air Guitar" (EmoteCatalog.displayName). Dans/şarkı adı (Gangnam Style) görünen yerde yok -> düşük, ama `clipName` görünür yere sızmasın.
4. **Dünya modelleri (Themes, ~1866 prefab/asset)**: Serhat'ın üretimi; `Themes/README.md` "Models and theme definitions are inside each concept folder" der, kaynak/lisans yok, doku yok (mesh+materyal). Dış paket kanıtı yok ama üretim yöntemi belgelenmemiş (elle mi, araçla mı, yapay zekâ mı) -> **kanıt yok, Serhat'a sorulmalı**. `Kevin Iglesias/.../Models/Yol.fbx` ve `LoopWay.prefab` Serhat'ın (T-072), Kevin Iglesias paketinin parçası değil.
5. **Ash Assets "Control Rig"** (`Resources/Control Rig.prefab` build'de): paketin LICENSE'ı yok, yalnız `Documentation.txt`; Asset Store paketi mi kendi işi mi belli değil -> **kanıt yok, Malik'e sorulmalı**.
6. **Kevin Iglesias "Human Character Dummy"** (karakter FBX + palet doku build'de): projede lisans dosyası yok; Asset Store ücretsiz paketi olduğu klasör yapısından anlaşılıyor (kanıt: yalnız yapı) -> **kanıt yok**. Klasörde ham kaynak `HumanDummyModels_Source.zip` (831 KB) repo'da; yeniden dağıtım sorusu (repo herkese açıksa) **incelenmeli**.
7. **GUI PRO Kit / CasualFantasy** (YOL-HARITASI:100): `Assets` içinde hiçbir iz yok (GUID/yol/ad taraması: 0 sonuç; Nunito ve `Flags/` klasörü de yok). Referanslar yalnız Sticker Patch'teki (`shikakuflow`) kaynakta ve `reports/leaderboard-tasima-2026-10-28.md`'de. Leaderboard sanatı bunun yerine `UI/Art/Leaderboard` teslimi. Yani bu madde bugünkü build'i etkilemiyor; taşıma yapılırsa o 25 yer lisans kontrolü ister -> **şimdilik risk yok, taşıma öncesi incelenmeli**.
8. **Ülke bayrakları** (`Leaderboard/country_row_flag_*`, 102 dosya adı eşleşti): kaynak belirsiz (teslimle gelen çizim; sembolik, genelde serbest ama kanıt yok) -> düşük.
9. **Ses (8 WAV)**: `scripts/sfx-uret.py` ile sentezlenmiş (sinüs/ton) -> kaynak kanıtı var, kendi üretimi, risk yok. Müzik klibi boş.

## Envanter: build'e giren

| Kalem | Yol | Kullanım | Kaynak | Kanıt | Risk |
|---|---|---|---|---|---|
| Kod (Speed-Staes) | `Assets/Speed-Staes/Scripts` (126 .cs erişildi) | tüm oyun | kendi (Malik/Serhat/biz) | repo | yok |
| Zenject (Extenject 9.2.0) | `Assets/Plugins/Zenject` | DI; yalnız Source + Signals; MPM `Editor` asset'i Resources'ta erişiliyor ama Editor klasörü oyuncuya girmez | açık kaynak | `LICENSE.txt` (MIT, Modest Tree Media) | yok |
| DOTween (ücretsiz) | `Assets/Plugins/Demigiant/DOTween/DOTween.dll`, `Resources/DOTweenSettings.asset` | UI animasyonu | Demigiant | `readme.txt` telif satırı; ayrı LICENSE dosyası yok (Asset Store / ücretsiz sürüm koşulları) | düşük |
| Kanit-BlackItalic | `UI/Fonts/Kanit/Kanit-BlackItalic.ttf` | UI fontu (tek ttf) | Google Fonts / Kanit Project | `OFL.txt` (SIL OFL 1.1, "Copyright 2020 The Kanit Project Authors") yanında | yok (OFL: ad "Kanit" ile bağlı kalınır, dosya yeniden satılmaz) |
| LegacyRuntime.ttf | Unity yerleşik | Text varsayılan | Unity | yerleşik | yok |
| HumanCharacterDummy_F + palet | `Assets/Kevin Iglesias/Human Character Dummy/Models/HumanCharacterDummy_F.fbx`, `Materials/..._ColorPalette.mat`, `Textures/..._ColorPalette.png` | koşucu karakteri | Kevin Iglesias (Asset Store ücretsiz paket olduğu yapıdan) | lisans dosyası yok | incelenmeli (madde 6) |
| Yol / LoopWay | `Kevin Iglesias/.../Models/Yol.fbx`, `LoopWay.prefab` | standart yol | Serhat (T-072) | task notu | düşük |
| Control Rig | `Assets/Ash Assets/Control Rig/Resources/Control Rig.prefab` (+1 .cs) | karakter rig'i, Resources | "Ash Assets" (paket mi bilinmiyor) | yalnız `Documentation.txt` | incelenmeli (madde 5) |
| Poz asset'leri | `Speed-Staes/Physical/Poses/{Ready,Run,Stumble}.asset` | koşu pozları | kendi (Malik) | - | yok |
| Emote verisi | `Animations/Baked/*.asset` (9) | emote oynatma | Mixamo'dan pişirilmiş | FBX'te Mixamo izi | incelenmeli (madde 3) |
| UI sanatı | `UI/Art/{Analysis,Costume,EventSelect,Home,Leaderboard,RaceHud,RaceStart,Results,Rewards,Settings,ThemeSelect}` - 543 PNG erişildi | tüm ekranlar | kullanıcının teslim aldığı paketler (üretici belirsiz) | README/layout.json, lisans yok | incelenmeli (madde 2) |
| Palet doku | `Speed-Staes/Art/ColorPalette.png` | tema/kostüm | belirsiz (kendi) | - | düşük |
| Mesh/aksesuar | `Speed-Staes/Art/Character/RunnerAccessoryMeshes.asset`, `UI/Art/Costume/Items/*` (kostüm küçük resimleri) | kostüm | kendi/üretim | README: "önizleme asset'idir" | düşük |
| Tema modelleri | `Speed-Staes/Prefabs/Themes/{Egypt,PirateSea,RomeByzantium,FloatingSky}`, `Buildings`, `StreetProps`, `LoopWayLibrary`, `Clouds`, `Hurdles` | Race arka planı | Serhat | `Themes/README.md`, kaynak yok | kanıt yok (madde 4) |
| Shader'lar (17) | `Speed-Staes/Shaders/*.shader` | tema/efekt | kendi | repo | yok |
| Materyaller | `Speed-Staes/Materials` (101), `Resources/*.mat` (7) | genel | kendi | - | yok |
| SFX (8 WAV) | `Speed-Staes/Audio/Sfx/*.wav` | UI/yarış sesleri | `scripts/sfx-uret.py` ile sentez | betik repo'da | yok |
| Veri asset'leri | `Speed-Staes/Data` (103) | config | kendi | - | yok |
| Sahneler | `Scenes/Menu/Home.unity`, `Scenes/Game/Race.unity` | build sahneleri | kendi | - | yok |
| Unity paketleri | uGUI 1.0.0, Input System 1.19, Animation Rigging 1.2.1, Postprocessing 3.4.0, Shadergraph 14.0.12, Test Framework 1.1.33, Recorder 4.0.3, Device Simulator Devices | runtime/editör | Unity | manifest.json | yok (Unity koşulları) |
| MCP for Unity | `com.coplaydev.unity-mcp` (git, `#main`) | yalnız editör aracı; Editor asmdef'i editör-only, Runtime asmdef'i `autoReferenced` -> küçük runtime parçası build'e girebilir | CoplayDev (açık kaynak) | PackageCache'te README var, lisans dosyası kökte bulunmadı | düşük (MIT olduğu varsayımı doğrulanmadı; ana dala sabitlenmemiş `#main` bağımlılığı ayrı konu) |
| Unity logosu | ProjectSettings: `m_ShowUnitySplashScreen: 1` | açılış | Unity | - | Unity sürüm/plan koşuluna bağlı (plan bilgisi yok) -> incelenmeli (düşük) |
| Uygulama ikonu | `m_BuildTargetIcons: []`, özel ikon yok | - | - | - | yok (varsayılan) |

## Build'e girmiyor (erişilemedi; toplam 2882 görsel/asset dosyası)

| Yol | Ne | Not |
|---|---|---|
| `Assets/Free Stylized Hand-Painted Skybox/` | BD Studios ücretsiz skybox (27 dosya, Demo + Cloudy PNG) | hiçbir sahne/prefab bağı yok; Home/Race gökyüzü `ThemePreviewSky.mat` (kendi). Lisans dosyası yok; silinmediği sürece repo'da dolaşır |
| `Assets/Samples/Animation Rigging/1.2.0` | Unity sample sahneleri (58) | Unity paket örneği, build dışı |
| `Assets/PlayerPrefsEditor/` | bgTools PlayerPrefs Editor (README, editör) | yalnız editör; asmdef'ler Editor; `README.md` var, ayrı LICENSE dosyası yok |
| `Assets/Kevin Iglesias/.../Prefabs`, `Human Character Dummy.unity`, `HumanDummyModels_Source.zip`, diğer materyaller | paket demosu/kaynak | build dışı |
| `Assets/Ash Assets/Control Rig/{Models,Scenes,Materials,Editor}` | demo sahnesi, "Simple Human.fbx" | build dışı |
| `Assets/Speed-Staes/Animations/*.fbx` (9) | ham Mixamo | build dışı, madde 3 |
| `Assets/Speed-Staes/UI/WardrobeIcons_20261028_*` (222 PNG) | kostüm ikon ham çıktıları | build dışı; kaynak üretim çıktısı |
| `UI/Art/*/*.jpg` (18) | `approved_design*.jpg`, `preview*.jpg`, `01_main_screen.jpg`, `storyboard.jpg` vb. önizlemeler | build dışı; referans oyun ekran görüntüsü içerebilir ama Assets'e girmiyor |
| `UI/Art` kullanılmayan PNG'ler (~115, çoğu hover/pressed/hover varyantı) | - | build dışı |
| `Prefabs/Themes` kullanılmayan ~2300 dosya (alternatif mesh/kalite varyantları) | - | build dışı |
| `Assets/Screenshots/` | tek ekran görüntüsü | build dışı |
| `Scenes/Test/*`, `Run_100m.unity`, `Anamenu Themaları.unity`, kök dışı demo sahneleri | test | Build Settings'te yok |
| `docs/eksik-ui/`, `docs/gdd/referans-ekranlar/` (orijinal-*.jpg), `docs/gdd/*.pdf` | referans oyunun ekran görüntüleri/PDF | `Assets` dışında; build'e girmez. Repo herkese açıksa referans oyun ekran görüntüleri dağıtılmış olur -> incelenmeli (repo görünürlüğü bilinmiyor) |

## Açık sorular (kullanıcıya)
1. Repo (`pundungames/speed-starts`) herkese açık mı? Mixamo FBX, Kevin Iglesias zip, referans oyun ekran görüntüleri buna bağlı.
2. UI sanatının üretici/lisans bilgisi (11 teslim paketi) nerede? Sözleşme/not varsa `Assets/Speed-Staes/UI/Art/LICENSE` benzeri bir yere yazılmalı.
3. Mağaza adı ve paket adı (productName / applicationIdentifier / ikon) ne olacak?
4. Serhat: tema modelleri nasıl üretildi (dış paket / kendi / araç)? Malik: Ash Assets Control Rig kaynağı?
