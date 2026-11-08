# Design

## Evidence and decisions
Read-only cache probe: item20769 inventory65270, male65297, female65316. Modern model flag2 carries emitter count then ushort emitter ID/face pairs, followed by effectors. ModelDefinition.method1201 and Class93 retain the face's three indices. Seven emitter403 attachments occur on male faces785..791 and female782..788. Inventory has none. Preserve face references and remap them on assembly and frontend corner expansion, so pose updates keep attachments aligned.

Class93.method1085 -> CutsceneCreature.method974 reads index27 archive0 emitter ID. Class210.method2214 decodes emitter opcodes. Class406 samples lifetime/rate/speed/color, emits barycentrically across a triangle, and accumulates emission in 1/64 units per millisecond. Class325_Sub3_Sub1_Sub1 moves fixed-point positions. Emitter403 refers to texture744 and force6; source color range is translucent white/gray, so do not hard-code the reference screenshot's gold color.

Backend owns decoded definitions/attachment metadata; model endpoints return emitter settings or visible warnings when required data cannot be decoded. Existing admin texture resource additionally owns emitter images. A small shared particle surface owns its Points geometry/material and bounded CPU slots, updates from ModelScene's single render callback and receives current model geometry for spawning. Existing model replacement/disposal clears all particle resources. Deterministic seeded sampling and explicit delta seconds make tests reproducible. No second render loop or timer.

## Fidelity boundary
Source attachment, texture and basic lifecycle parameters are used. Billboard rendering and ballistic movement are an inspection approximation. Force fields, collisions, client prewarm, advanced color/speed/size curves and render sorting remain explicitly unverified/unsupported. Unknown opcodes fail visibly rather than guessing their length. Cache authentication is inherited unchanged.

## Validation
Retain real model and emitter fixtures with focused geometry/API tests; verify source lifecycle math, attachment remapping, posed spawn origins, animation and cleanup with real Three resources and mocked GPU. Admin tests verify emitter texture loading and warnings. Browser checks remain waived.

## Completed verification

Real fixtures are retained under TestSupport/Fixtures/particles. ModelParticleTests checks both worn face ranges, inventory absence, emitter403 source fields and unsupported/truncated definitions. API tests cover source-ID to member-index mapping, missing/unsupported emitter warnings and multipart face offsets; existing policy tests cover inherited authorization. Shared tests cover expanded-face and posed origins, bounded slots, sprite fallback, borrowed texture ownership and ModelScene replacement cleanup using the existing loop. Admin tests cover emitter-only texture loading and visible fidelity limitations.

Passing evidence: 50 targeted codec tests, 68 cache-service tests, 34 shared viewer tests and 234 admin tests. Both Angular production builds pass. Existing package vulnerability/pruning and test polyfill warnings remain; no new compiler diagnostics were introduced. Browser checks remain explicitly waived, so no GPU compilation, screenshot or visual parity evidence is claimed.

Source-defined emitter403 uses translucent white/gray particle colors with sprite744. The screenshot's gold coloration is not hard-coded into item20769. Geometry attachments and lifecycle settings come from the cache; forces, advanced curves, prewarm, collision and exact client billboard/sorting behavior remain disclosed limitations of this ballistic inspection preview. Selecting inventory creates no fake emitters. No new authentication boundary, dependency, timer or second resource owner was introduced.

## Particle color correction

Production trace: item20769 worn models65297/65316 -> emitter403 faces785..791 /782..788. Every attachment face has packed HSL20287 and alpha255. prepareCacheModel treated255 as fully opaque, rendering the green helper triangles. Model_Sub3 maps signed alpha -1 to type2 and then its hidden-face sentinel -2. Hide alpha255 through zero opacity while keeping face/vertex order, preserving emission and animation indices. Alpha254 retains the separate client constant-shading behavior and stays visible.

The particle sprite744 contains only grayscale visible pixels. Its HDR companion is multiplied into an unclamped float texture. The current inspection renderer has no HDR bloom/output pipeline, so float sprite values clip to white and obscure tint. Normalize HDR sprite RGB by its peak only when above1, before multiplying particle tint, preserving RGB ratios and alpha. This is a disclosed bounded-intensity preview fallback, not client HDR/bloom parity. Ordinary sprites and surface HDR remain unchanged.

Source ARGB tint is interpolated in source color space, then converted to linear RGB before the shader applies the output color-space transform. This prevents the earlier double-brightening of nonwhite tints. Tests round-trip the rendered tint back to source RGB and retain source alpha.

Color correction validation: 35 shared-viewer tests, 234 admin tests and five real particle codec fixture tests pass. Both Angular production builds pass; git diff --check passes. Regression cases retain hidden attachment geometry and particle emission, distinguish alpha254/255, round-trip tinted particle RGB through the linear output pipeline, retain source alpha and enable brightness normalization only for float HDR sprites. The tint check compares restored 8-bit channels, allowing Three's documented transfer-function approximation and float-buffer precision. No backend runtime change was needed; browser checks remain waived.
