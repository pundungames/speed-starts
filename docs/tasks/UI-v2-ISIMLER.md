# UI v2 — isim sözleşmesi (T-015 view'lar ↔ T-016 builder ↔ T-018 Play testi)

Kaynak sanat: `Assets/Speed-Staes/UI/Art/Home/` (47 PNG + `layout.json` + `preview.jpg` + `README.txt`) ve `UI/Art/Race/` (12 PNG).
Tasarım görüntüleri: `docs/gdd/referans-ekranlar/tasarim-ana-ekran-v2.png`, `tasarim-yaris-hud-v2.png`. Metin/sayı PNG'lere gömülü DEĞİL
→ Text ile üstüne yazılır. Aynı bileşenin katmanları aynı rect'e %100 ölçekle üst üste (README). Konum `layout.json`'dan (referans 1678×937)
okunur; canvas 2400×1080 yükseklik eşleşmesi → ölçek 1080/937; sol kenara yakın bileşen sol köşeye, sağdakiler sağ köşeye, ortadaki
(race_select) alt-ortaya anchor'lanır. Aşağıdaki adlar hiyerarşi yolu (kökten) ve view alanı. **Değiştirmek gerekirse önce süpervizöre.**

## HomeScreen (kök: `HomeScreenView`)
| Yol | Katmanlar | View alanı (HomeScreenView) | Davranış |
|---|---|---|---|
| `DailyReward` (Button) / `Notification` | daily_reward_* | `sevenDayButton`, `sevenDayRedDot` (GameObject) | 7 gün popup'ı |
| `Settings` (Button) | settings_* | `settingsButton` | sistem yok → `interactable=false` |
| `Sound` (Button) | sound_* | `soundButton` | sistem yok → pasif |
| `Environment` (Button) | environment_* | `environmentButton` | sistem yok → pasif |
| `Upgrade` (Button) | upgrade_* | `upgradeButton` | sistem yok → pasif |
| `Players` (Button) | players_* | `playersButton` | sistem yok → pasif |
| `HighScores` (Button) | high_scores_* | `highScoresButton` | High Scores ekranı |
| `RaceSelect` (Button) / `Icon`, `Label` (Text), `Chevron` | race_select_* | `eventSelectButton`, `eventNameText` | Event Select ekranı |
| `Play` (Button) / `Icon` | play_* | `playButton` | giriş kapısı akışı (Faz 2) |
| `CharacterCard` / `Background` | character_card_background | — | — |
| `CharacterCard/Select` / `Portrait`, `Name` (Text) | character_select_* | `racerNameText` | ön ayar adı ("Custom 1") |
| `CharacterCard/Previous`, `CharacterCard/Next` (Button) | character_previous_*, character_next_* | `previousRacerButton`, `nextRacerButton` | ön ayar sistemi Faz 5 → pasif |
| `CharacterCard/PowerBar` / `Empty`, `Fill` (Image Filled, yatay) | stats_power_* | `powerFill` | stat sistemi yok → builder fill=0.75 |
| `CharacterCard/StaminaBar` / `Empty`, `Fill` | stats_stamina_* | `staminaFill` | builder fill=0.7 |
| `CharacterCard/HeightField` / `Label` (Text) | stats_height_field | `heightText` | "Height: 5'9" sabit metin |
| `CharacterCard/EditRacer` (Button) | edit_racer_* | `editRacerButton` | Cosmetics ekranı |
| `CharacterCard/Emote` (Button) | emote_* | `emoteButton` | sistem yok → pasif |

## ResourceBar (widget prefab, sol dikey sütun; kök: `ResourceBarView`)
| Yol | Katmanlar | View alanı | |
|---|---|---|---|
| `PurpleGem` / `Value` (Text) | purple_gem_* | `purpleGemText` | |
| `EntryTicket` / `Value` (Text) | entry_ticket_* | `entryTicketText` | "2/3" |
| `TicketTimer` / `Value` (Text) | ticket_timer_background | `entryTimerRoot` (GameObject), `entryTimerText` | doluyken gizli |
| `TimedTicket` / `Value` (Text) | timed_ticket_* | `timedTicketText` | sayı ya da kalan süre |
| `Cash` / `Value` (Text) | diamond_* | `cashText` | Nakit |
Mevcut alan adları ResourceBarView'da farklıysa view'daki ad geçerli; T-015 bu tabloyu kodla eşitler ve çalışma notuna yazar.

## RaceHud (kök: `RaceHudView`) — `UI/Art/Race/`
| Yol | Sprite | View alanı (RaceHudView) | |
|---|---|---|---|
| `Telemetry` / `Stopwatch`, `Time` (Text), `Speedometer`, `Speed` (Text) | telemetry_panel, stopwatch_icon, speedometer_icon | `timeText`, `speedText` | süre `ss.cc`, hız "x.x m/s" |
| `Cash` / `Icon`, `Value` (Text), `Plus` (Button) | counter_panel, cash_diamond_icon | `cashText`, `cashPlusButton` | Nakit; + pasif |
| `Energy` / `Icon`, `Track`, `Fill` (Image Filled) | energy_icon | `energyFill` | `IRaceTelemetry.Energy01` |
| `Cadence` / `Label` (Text "Cadence"), `Track` (gradient yeşil→kırmızı), `Marker` | cadence_marker | `cadenceTrack` (RectTransform), `cadenceMarker` (RectTransform) | `PressesPerSecond / cadenceMaxPressesPerSecond` (view alanı, Tooltip) |
| `Exit` (Button) | square_button_background, exit_icon | `exitButton` | Main menu (bilet iadesi yok, PDF §1.2) |
| `Restart` (Button) | square_button_background, restart_icon | `restartButton` | Race again ile aynı yol (`gate.Enter`) |
Eski `Progress/...` (mesafe çubuğu) kaldırılır; view'da progress alanları opsiyonel (null-safe) kalır.

## Bütün ekran ve popup'lar — kapat
`UiPanel` (taban) yeni opsiyonel alan **`closeButton`** (Button): tıklanınca popup ise `navigator.ClosePopup(this)`, ekran ise `navigator.Back()`.
Builder her ekran/popup'taki `Back` / `Close` / `ItemBack` butonunu bu alana bağlar — **kendi kapatma kodu olan view'larda** (EntryGateModalView.closeButton,
EventSelectScreenView.backButton) taban alan BOŞ kalır (çift tetik olmasın).
