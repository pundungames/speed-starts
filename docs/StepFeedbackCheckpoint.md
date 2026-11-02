# Step feedback checkpoint

## Latest revision (2026-11-02)

Feedback is now explicitly installed on the RaceSessionAdapter player (or the ThemeRunPlayground player), rather than every RunnerBody. The Race visual test asserts that NPC bodies have none of the four player-feedback components. All feedback text is smaller; LATE uses muted red, 0.4-second duration and a 2-second repeat cooldown.

The continuous speed-only back ribbon was removed. Momentum now displays three separated short air strokes for 0.32 seconds after three alternating Good presses with at least 80% efficiency, within 0.8 seconds of each other, at 2.5 m/s or faster. Its cooldown is 1.8 seconds. Bad presses break the sequence and hide the effect. Five targeted tests passed, followed by a passing Race visual recheck.

The sections below describe the earlier checkpoint and visibility investigation.

Five presentation cues are attached automatically by RunnerBody: perfect contact, early wobble, late text, speed-driven back wind, and recovery contact/text. They do not modify running physics.

Visibility investigation in the Race scene found valid, supported materials and active renderers. The contact ring was mostly covered by the shoe and faded quadratically within 0.22 seconds. Back wind was disabled below 2 m/s, very narrow, and low-opacity against the pale sky.

Corrections:
- Contact ring radius increased to 0.29 x 0.43 m, width to 0.04 m, ground clearance to 0.055 m. Contact and recovery last 0.6 seconds with a readable initial hold before fading.
- Wind starts above 0.8 m/s with a minimum visible strength, grows to 1.6 m, and uses a dedicated wider-profile blue-white alpha shader. Reset and teleport clear it.
- PERFECT, RECOVER!, and LATE appear behind the runner, above the wind ribbon.
- LATE lasts 0.55 seconds and has a 1.5-second repeat cooldown.

Validation: the final 0.28 m text-height adjustment was verified on 2026-11-02 in the Race scene. PERFECT, LATE, and RECOVER! are behind the player and clear of the chest-height wind ribbon. A visual overlap between the fading LATE warning and RECOVER! was corrected by hiding LATE on an efficient good step while retaining its repeat cooldown. The three targeted PlayMode tests passed with no console errors. Actual camera captures are in Temp/FeedbackReview (contact.png, late.png, recovery.png, wind.png).

The visual capture test now selects the player through RaceSessionAdapter rather than the first RunnerBody found, since background runners also have that component. It uses a fixed capture frame rate and restores the previous setting afterward.
