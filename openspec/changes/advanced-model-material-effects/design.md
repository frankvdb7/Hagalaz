# Design

## Context

ModelScene already owns a Three.js animation loop, GPU textures/materials and disposal. CacheModelPreview owns authenticated asset requests and decoded bitmaps. The material endpoint retains raw IDs, parameters, scrolling and HDR presence, but the shared ModelTexture currently omits effect and scroll fields. See proposal.md for scope.

## Goals / Non-Goals

Extend these existing boundaries. Do not introduce a general effect plugin framework, extra renderer, clock, worker or dependency. Preview scene-dependent materials with explicit fidelity labels rather than building a scene simulation.

## Decisions

- Preserve numeric source IDs and parameters in renderer-neutral data. Use a small explicit effect switch rather than an interface or class per effect. Direct material construction remains the owner of shader resources.
- Evaluate effects with elapsed seconds in ModelScene's existing render loop. Tests pass explicit time into the same calculation. Model replacement resets the preview epoch; skeletal animation pause does not pause independent material animation. Avoid timers per texture.
- Apply UV scrolling using raw signed bytes / 64, verifying orientation against the bitmap flip and client matrix convention. Do not accumulate frame deltas, which introduces drift and makes deterministic testing harder.
- Use focused custom shader hooks only where ordinary Three.js material parameters cannot express proven behavior. Preserve vertex tint, alpha mode and face alpha. Keep supported IDs distinct even when capabilities require a common fallback.
- HDR decoding belongs in the cache layer alongside TextureImageDecoder. TextureLoader.method649 reads companion samples and multiplies RGB by 1 + sample * 31 / 4096. Add a protected renderer-neutral float projection through TextureEndpoints; keep existing image and metadata routes compatible. Preserve source sample extraction and dimensions with fixtures before wiring GPU uploads.
- The admin request subscription remains the sole CPU asset owner and retry owner. ModelScene remains the sole GPU resource owner. Keep unsupported/fallback presentation in admin using shared effect classification data so labels cannot disagree with renderer selection.

## Evidence and effect boundaries

References: docs/3d-rendering/revision-742-appearance-semantics.md, renderer-neutral-contracts.md, GameClient TextureLoader.java, MaterialInformation.java and renderer effect selectors. Recorded categories are 1 environment map, 2 ordinary water, 3 unresolved, 4 underwater, 5 lake water, 6 light absorb, 7 reflection, 8 HD water and 9 HD water wave. Ordinary water has documented five-second and four-second phases. Exact parameter masks and coordinate formulas require focused client-source fixtures during implementation. Do not infer them from effect names.

Each supported category must record its production selection path, parameter use and fixed-time samples. Missing scene inputs produce named approximations or base-material fallbacks; category 3 stays unsupported. No hard-coded texture-ID behavior.

## Risks / Trade-offs

- Isolated models lack the client environment and shoreline. Use a clearly labeled inspection environment or base-material fallback, never a parity claim.
- Custom material hooks can break alpha and color behavior. Retain existing surface grouping and add mixed-face regression cases.
- HDR output depends on renderer tone mapping. Test decoded linear values separately from visual appearance and disclose preview tone mapping.

## Migration Plan

Deploy compatible cache additions before admin consumption. Revert shared/admin effect wiring to return to existing base previews. No data migration or authorization change.
