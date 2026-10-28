# Marine map composition

The active sea catalogue is rebuilt by `ThemeKitBuilder.RebuildMarineMap`.
`ThemedLoopWay.Marine.cs` owns its layout; the generic island row no longer runs for PirateSea.

- 12 waterfront templates, chosen by least usage with a recent-history exclusion.
- 8 coast/story templates: wide harbor, volcano, long coast, eroded cave, pirate galleon, admiral flagship, two fishing skiffs and beacon island.
- Coast event anchors use a global 75 m grid with seeded offsets, independently of the 60.43 m infrastructure sections. Landmarks can extend across a section boundary.
- Later event cycles shuffle their decks and avoid equal neighbors at deck boundaries.
- Large islands, nearby rock formations, boats and distant volcanoes occupy different depth ranges.
- Ground piles remain submerged; buildings connect to the boardwalk with sloping timber gangways.
- No octopus, human figures or animal models are in the active sea pools. Fishing skiffs use rods, lines, oars and tackle rather than modeled people.
- Marine materials use stable local paint cells without an outline, with modeled mortar courses, shutters and roof tiles.
- The sea runtime retains sections 90 m behind the runner to avoid retiring a visible cross-boundary island.

Validation during the rebuild: all 12 waterfront templates used over 80 planned sections, zero adjacent frontage repeats, zero frontage interval overlaps; all 8 event templates used and zero adjacent event repeats over 128 events. Per-model geometry/contact audit: `MarineRebuildAudit.md`.
