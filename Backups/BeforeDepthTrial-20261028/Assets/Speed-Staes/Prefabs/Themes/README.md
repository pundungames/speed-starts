# LoopWay Theme Prefabs

- LoopWay2.prefab: Egypt — pyramids, pylon temple, sphinx courtyard, oasis, market, obelisk, palms and papyrus.
- LoopWay3.prefab: Rome / Byzantium — classical temple, domed basilica, aqueduct, triumphal arch, marble statues and mosaics.
- LoopWay4.prefab: Pirate sea — timber boardwalk above the sea, harbor, island, lighthouse, pirate fleet, giant octopus and nautical props.
- LoopWay5.prefab: Floating sky — floating castle and gardens, windmill island, rainbow, balloon, treasure and birds.

Use one theme prefab in your game scene in place of the environment being previewed. The original LoopWay prefab and its scene were not modified.

Themed Loop Way inspector:
- Loop Count is the total preview section count, starting at section 0.
- Seed deterministically changes scenery, silhouettes, spacing, vegetation and props.
- Auto Preview updates editor geometry when settings change; Undo/Redo restores settings and regenerates the preview.
- Stream At Runtime appends a new seeded section after the runner passes an old section. Assign Runner, or allow automatic RunnerMovement discovery.
- The original road meshes, dimensions, markings and guardrail geometry are shared by all four themes. Only road material colors differ.

Models and theme definitions are inside each concept folder. Materials are separate assets. Each theme has distinct foreground, background, prop, vegetation and lantern pools. Background clouds have independent CloudVariation values.

Preview images: D:/Github/speed-starts/Captures/Themes/

The existing active editor scene was left open and unchanged. A separate hidden preview scene was used for the screenshots.
