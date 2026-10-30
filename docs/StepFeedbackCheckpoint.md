# Step feedback checkpoint

Five presentation cues are attached automatically by RunnerBody: perfect contact, early wobble, late text, speed-driven back wind, and recovery contact/text. They do not modify running physics.

Visibility investigation in the Race scene found valid, supported materials and active renderers. The contact ring was mostly covered by the shoe and faded quadratically within 0.22 seconds. Back wind was disabled below 2 m/s, very narrow, and low-opacity against the pale sky.

Corrections:
- Contact ring radius increased to 0.29 x 0.43 m, width to 0.04 m, ground clearance to 0.055 m. Contact and recovery last 0.6 seconds with a readable initial hold before fading.
- Wind starts above 0.8 m/s with a minimum visible strength, grows to 1.6 m, and uses a dedicated wider-profile blue-white alpha shader. Reset and teleport clear it.
- PERFECT, RECOVER!, and LATE appear behind the runner, above the wind ribbon.
- LATE lasts 0.55 seconds and has a 1.5-second repeat cooldown.

Validation: all five targeted PlayMode tests passed, including actual Race camera captures in Temp/FeedbackReview. The final additional 0.28 m text-height adjustment was made afterward; the recheck could not be confirmed because Unity disconnected during that run. Re-run StepFeedbackVisualTests.CaptureActualRaceFeedback when Unity is available.
