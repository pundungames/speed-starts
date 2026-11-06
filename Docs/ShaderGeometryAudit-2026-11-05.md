# Camera-angle geometry audit

Source inspection only: Unity MCP reported zero connected Editor instances, so shader compilation and visual reproduction remain unverified.

## Findings and changes

`DesertPixelPaint.shader` divided clip-space xy by `max(0.0001, w)` before snapping. Negative w was replaced with a positive epsilon, then multiplied by the original negative w. Camera-crossing triangles could therefore acquire enormous screen-space offsets.

The shared shader now skips snapping behind the near plane, divides only by positive w, and fades snapping in between one and two near-plane distances. All profiles retain their authored pixel silhouette strength with bounded offsets, including distant scenery, ground and mountains. Material assets need no migration.

`RoadPainted.shader` widened yellow ground vertices around the fixed world x coordinate -5.63 by 1.8. This also moved any marking whose center differed from that coordinate. Removed the vertex modifier: authored mesh positions and widths are now preserved. This currently restores the original, potentially thinner line; preserving the previously widened appearance while correcting alignment remains unresolved. It does not recenter incorrectly authored meshes.

## Cross-map source audit

| Theme | Materials using shared pixel shader with snap enabled | Relevant exposure before fix |
| --- | ---: | --- |
| Egypt | 83 | Ground/paving profile 6, limestone profile 7, distant profile 5 |
| RomeByzantium | 75 | Ground/paving profile 6, near/far mountains profile 7, distant hills profile 5 |
| PirateSea | 53 | Distant profile 5; weathered cliff profile 1 |
| FloatingSky | 74 | Distant profile 5; weathered cliff profile 1 |

Counts describe material assets, not visible renderer counts or confirmed visual failures. The camera-side guard protects every user of the shared shader, including profile-1 cliffs. Ground/mountain/distant profiles retain their original snapping style with the same camera-side guard.

## Validation

- Scoped `git diff --check` passed.
- Numerical checks at negative, zero, near-plane and positive w values showed bounded offsets in the corrected perspective calculation; the original negative-w case produced extreme offsets.
- Pending in Unity: shader import/compilation, camera orbit through ground and cliffs in all four maps, road alignment and restored line width, plus a check in the normal gameplay camera.
- Backups and exported delivery copies were not modified; changes target the live Assets shaders.

