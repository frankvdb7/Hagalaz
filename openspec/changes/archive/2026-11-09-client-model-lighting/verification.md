# Client lighting verification

Implemented model-local software-client lighting under a deterministic inspection preset. The light direction is (-50,-60,-50), base brightness 1.1523438, directional responses 0.69921875 and 1.2, palette gamma 0.7 without client random jitter. This does not reproduce map atmosphere, scene seam-normal merging, point lights or all GPU backends.

## Evidence

- GameClient source: Model_Sub3.method4531 integer normals; method4549 smooth/flat and textured/untextured lighting; method4533 and Node_Sub26_Sub1.method3395 clamps; Class356.method4187 packed HSL-to-HSV; Class393.method4615 and VarClanSettingsDefinition palette generation; GraphicsToolkit_Sub3.m fixed-point light inputs.
- Extracted Java helpers ran in LightingReference.java under C:/Users/fmavd/.codex/visualizations/2026/11/01/01a19942-51c9-7643-9b30-500cff69a4ee/. With colour 0x1234 and ambient 64, +/-Z produced shades 71/221, untextured RGB 48321f/a27248 and textured RGB 4a321e/a57143. The >256 directional branch fixture produced 286; the negative fixture produced -93. Fixed expected values are asserted in model-lighting.spec.ts.
- Decoder suite: 41 passed. API appearance suite: 13 passed, including signed non-default item/NPC/object ambient and contrast, and retained shading types/textures. Shared model viewer suite: 47 passed. Admin suite: 241 passed.
- Both frontend production builds and the affected backend service build passed. No new compiler warnings identified. Existing ImageSharp/package-pruning warnings and the admin test polyfills warning remain.
- Aspire rebuilt hagalaz-services-cache and restarted hagalaz-web-admin; both reported healthy.
- The authenticated admin model #666 was loaded before lighting reload and captured in model-666-before-lighting.png in the artifact directory. Browser action dispatch became unreliable; the post-lighting visual comparison and smooth/flat animated browser fixtures are not yet verified. Task 3.2 remains pending.

## Latest UI request

The user requested removal of Preview notes on 2026-11-02. Removed the workspace details section, its input binding and unused styles. Lighting limitations remain documented here and in API warnings rather than in that workspace section. Admin production build passed after the template change.
