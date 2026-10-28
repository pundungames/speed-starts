# Runner Toon

Built-in Render Pipeline shader: `Speed Stars/Runner Toon`.

The lit band displays the texture multiplied by Base Color directly, without specular, reflections, ambient lighting or light-color tint. Main-light direction and received shadow attenuation select a hard darker band. Shadow Darkness multiplies the base color; Shadow Grain Strength and Density add UV-anchored grain only inside that band. Outline Width is measured in reference pixels at a render height of 1284, matching the approved landscape iPhone 13 Pro Max Simulator (2778x1284). Actual pixel width is `_OutlineWidth * renderHeight / 1284`, so thickness relative to screen height stays constant across render resolutions without changing material values. The front-culled expanded hull can leave small seams at split mesh normals; this is not a full-screen edge detector.

Materials: `Assets/Speed-Staes/Materials/RunnerToon`. Adjust Base Color, Shadow Darkness, Hard Shadow Threshold, Shadow Grain Strength/Density and Outline Width in the Inspector. Zero grain or outline width disables that effect.

`Speed Stars > Toon > Apply to Runner` converts the open scene's `HumanCharacterDummy_F` skinned renderers. Existing base texture, UV tiling/offset and tint are copied into independent material assets. Original materials stay intact. Applying again skips materials already using the shader. Renderer assignments support Undo; generated assets remain available. Save the scene to persist the assignments.

`Speed Stars > Toon > Refine Runner Look` applies the refined four-material preset, narrows and depth-biases the outline, changes the directional light to a steeper angle with a weaker cast shadow, and adds a soft procedural contact oval. Grain uses UV coordinates with derivative filtering to suppress subpixel noise. Outline intersections are reduced, not completely removed; separate clothing shells can still produce internal lines.

The contact oval follows the runner in X/Z at a configured fixed ground height, matching this flat track. It does not project onto slopes or stairs. Adjust its Size and Ground Height on `Runner Contact Shadow`. The original meshes, gameplay and animation are unchanged. Outline thickness is proportional to render height; aspect ratio can still change framing, and antialiasing/downsampling can affect subpixel edge coverage. Compare captures at matching aspect ratios and display sizes.
