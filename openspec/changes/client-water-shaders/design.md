## Context

The cache already exposes water material parameters, underwater heights and the region atmosphere. The client is authoritative where its decompiled programs differ from the brief.

## Decisions

- Use the production cache decoders for the census. Prefer HD test regions that also have underwater terrain and six environment faces.
- Keep deterministic CPU data in `water-noise.ts`. Measure the first generation cost and keep long work off the render path. `ModelScene` owns one GPU texture per active volume per scene and releases it with its other resources. Generating per material would multiply CPU and GPU cost.
- Compute water depth in prepared map geometry using the client qualification rules and the existing height lookup. Upload the attribute for maps and retain the existing terrain cache across animation frames.
- Keep parameter decoding and shader source in `material-effects.ts`. Bind volume, atmosphere and cube resources through `ModelScene` and update time through `effectUpdates`. Follow the client cube axes, fog and blend state. Use the base material when the client would skip the HD program.
- Resolve the fixed-function texture stages against the client GL constants. Reuse the existing terrain material and corner weights for soft edges only if they preserve water output without a second animation path or substantial shader growth.
- Validate shader compilation and linking with real WebGL. Verify real-cache animation and fallback in the editor. Keep a separate client comparison task open.

## Cache census

The cache service reads `Source/Cache`. The production decoders read the material table in index 26, archive 0, and the terrain-addressable floor catalog in index 2. The scan decoded 2,622 named surface terrain archives with zero read errors.

| Effect | Textures | Underlays | Overlays | Editor region |
| --- | ---: | ---: | ---: | ---: |
| 2 | 30 | 0 | 7 | 10138 |
| 4 | 12 | 0 | 13 | 6960 |
| 5 | 5 | 0 | 0 | |
| 8 | 8 | 0 | 1 | 15177 |
| 9 | 5 | 0 | 0 | |

Regions 10138 and 6960 have complete terrain boundaries and animated plane-zero materials. Region 15177 has an underwater archive and six environment face IDs. The first effect 2 candidate, 7236, did not render in the live editor. Effects 5 and 9 have no floor definitions in this cache.

## Noise verification

The fixed-point volumes and HD height bytes matched a Java harness running the client generators byte for byte. Both turbulence calls produce the same table because `LongNode.method3378` ignores the offset. `Class49_Sub4` interleaves their values by cell and slides a 128-float window through that array. The window reads at most 255 values, so the port generates and uploads only the first two rows. The two-channel volumes use linear filtering without mipmaps. The HD RGBA volume uses trilinear mipmaps when supported. The normal pass uses reciprocal relief 0.5 and texel steps of 1/128. Its linear texture fetch at each slice boundary averages the current and preceding heights, including the repeat boundary. CPU rounding was checked against independent samples computed from the Java height field.

Cold Node wall times including yields were about 246 ms for ridged noise, 245 ms for lake noise, 554 ms for normals and 9 ms for the turbulence table. The volume generators yield between 128 by 128 slices. The editor still needs a live responsiveness check.

## Depth stream

`RegionManager` passes the underwater height field as the first extra terrain to `Class274.method2692` for surface plane 0. `Class274` writes underwater minus surface height into the fourth array for vertices allowed by the shape mask. `Class_xa_Sub1` stores this array as `anIntArrayArrayArray8771` and writes its value minus one to texture-coordinate Z. A masked vertex therefore receives -1. Without the depth array the texture coordinate defaults to zero.

## HD water

World-space view vectors use client axes, with web Y negated and X/Z preserved. The shader reflects that incident vector and samples the cube directly. The texture uses client V, which is the negative of the web floor image coordinate, plus the material scroll from the client texture matrix. The sun direction is the negated normalized client light direction and RGB uses the client byte fractions. Effect 8 uses a base sun exponent of 96. Effect 9 uses 64 and scales the RGB by 1 + (1 - abs(sun Y)) * 2. Both add a specular term with alpha 1 before scaling by shore factor, and effect 9 mixes the wave into RGBA. These alpha and effect 9 lighting details differ from the brief.

The terrain pass uses source-alpha blending, alpha test greater than zero and depth writes. Underwater terrain draws first. The HD program needs a volume and a region cube. The renderer retains prepared terrain while changing the material when the cube arrives or leaves. A Chromium WebGL2 run linked both HD programs with fog enabled and disabled.

## Fixed-function water

The client material table stores the texture combiner separately from floor color blend. The scan found combiner 0 (GL_MODULATE) on all 60 water textures, so this port requires no material API change. Effect 2 modulates the base with vertex color, then adds the noise luminance and replaces alpha with 1 when unit 1 is enabled. Effect 4 adds the noise luminance to vertex color with RGB scale 2 and multiplies by the noise alpha. The second unit keeps RGB and adds an alpha ramp derived from eye depth. Its two clamp-to-edge alpha texels are evaluated in GLSL. Effect 5 modulates the base with vertex color and then multiplies RGB by the lake noise luminance and alpha by its second channel. All three linked in Chromium WebGL2 with fog enabled and disabled.

Effect 2 applies the client ambient term to terrain only. Map objects already carry the region tint in their prepared vertices, so applying the term again would tint them twice. Point lights follow the water output on every water program, because the light term is inserted before the water shader replaces the colorspace chunk.

## Floor edges

The client batches floor contributions by material in `Class_xa_Sub1` and `Node_Sub16`. The existing web terrain blend is opaque and mixes base texture RGB, with a shared effect 7 reflection term. Water would need per-layer animation, different texture coordinates and alpha compositing for up to eight layers. This phase leaves water on the dedicated material path under the requested limit on shader growth.

## Editor smoke check

The user saw effect 2 deform in region 10138. Its texture 1 uses parameter 1 and packed parameter 0, so turbulence moves the base UVs while the second volume stays at time zero. The user reported that effect 4 worked in region 6960 and saw reflections in region 15177. All five effect shaders linked in a separate Chromium WebGL2 fixture. In the authenticated editor, regions 15177 and 10138 logged no console errors and every program linked. Both regions keep rendering while their water is visible. The look of HD water, the water-surface toggle in 15177 and the client comparison still need evidence.

## Verification

Admin tests passed 523, public tests passed 31, and shared viewer tests passed 143. The admin production build, Biome and strict OpenSpec validation passed. Biome reported two existing non-null assertion warnings. Angular tests reported the existing missing-polyfills TypeScript warning.

V8 measured 100% lines and 95.89% branches for material effects and 98.13% lines and 91.95% branches for ModelScene. Before the review fixes it measured 100% line and branch coverage for the noise generator, 99.05% lines and 90.7% branches for map geometry, and 83.54% lines and 65.8% branches for the viewport component.

Fallow exits 1. Its remaining findings are five unused-member or unused-input findings and eight complexity findings. Each complexity finding also exists at the commit before this change, and this change adds none. No duplication groups were reported.
