# ADR 2026-10-30: Save gains `dayBests` (leaderboard source), no schema bump

**Context.** The local leaderboard (T-037) ranks the player by period (today, week, month, all time). A single best per event
(`bestTimes`) cannot answer "best inside this week".

**Decision.** `PlayerProgress` gets one additive list `dayBests` of `{eventId, dayUtcTicks, elapsedMilliseconds, achievedUtcTicks}`:
the player's best finished race per (event, UTC day), written by `PersonalBestService.Record` (same race-id idempotence, same save
call). Capped by `LeaderboardConfig.maxDayBestEntries`; the oldest go first and each event's fastest entry is never dropped.

**Migration.** No schema bump. An older save has no `dayBests` key; `JsonUtility` leaves the constructor's empty list and
`EnsureValid` repairs a null. Tested in `DayBestTests.OlderSave_WithoutTheList_OpensWithAnEmptyList`. Going back to an older
build ignores the unknown key. History before this change is not reconstructed (only `bestTimes` existed).

**Note.** Name and flag are not saved per entry: rows show the player's current settings.
