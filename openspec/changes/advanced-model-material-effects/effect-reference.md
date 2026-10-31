# Effect implementation reference

## Reachable client flow

GraphicsToolkit_Sub1.method4842 reads MaterialInformation from a selected textured face, applies aByte531/aByte530 scrolling, then passes aByte525, aByte526 and decoded anInt527 to Class48.method599. Class48 selects Class49_Sub7/4/1/5/8/3/9/6 for IDs 1/2/4/5/6/7/8/9 respectively. There is no texture-ID lookup. Its capability fallback converts 4/8/9 to 2; unsupported HD classes also fall back 9 -> 8 -> 4.

TextureLoader reads the packed parameter at column byte offset 18 * presentCount plus ordinal * 4, before the alpha column at 22 * presentCount. Preserve this full signed 32-bit payload; shader masks use unsigned shifts where appropriate.

## Source formulas

- GraphicsToolkit_Sub1.method4842: ((milliseconds % 128000) / 1000 * signedScrollByte / 64) % 1 on both axes. Texture coordinates remain in client orientation; ImageBitmap flipY and HDR row inversion give the same sampling direction. At 1000ms bytes -32/16 give -0.5/0.25, at 2000ms give -0/0.5, at 129000ms repeat the first sample.
- Class49_Sub7.method606: parameter selects one of three precomputed environment lobes with index p - 1. Preview uses a labeled analytic inspection sky with lobe-dependent strength instead of client cubemaps.
- Class49_Sub4.method608: 5-second noise cycle and 4-second secondary phase. method606 uses packed & 1 for animation, byte & 128 disables secondary animation, byte & 64 controls lighting and byte & 3 selects distortion 0.025, 0.05 or 0.1. At 1000ms p=3 packed=1 gives phases 0.2/0.25 and distortion 0.1.
- Class49_Sub1.method606: p & 1 enables a 4-second generated-noise cycle. Without it phase is zero.
- Class49_Sub5.method606: vertical shift ms * -0.0005 * ((p & 3)+1), noise phase ms * 0.0005 * (((p >> 3) & 3)+1), both modulo 1. Coordinate scale is 1/2048 or 1/1024 according to bit64; bit128 chooses X rather than Z. Preview preserves these masks while replacing the generated noise volume.
- Class49_Sub8 -> GraphicsToolkit_Sub1.method4855/4856 disables GL_LIGHTING. Preview uses an unlit material with existing texture/color/alpha.
- Class49_Sub3 uses a scene environment cube and ignores both effect parameters. Preview uses the same labeled inspection sky as category1 with view-dependent reflection.
- Class49_Sub9.method606 and Class49_Sub6.method606: phase ((ms * (1 << (p & 3))) % 40000)/40000; scale (1 << ((p >> 3)&7))/32. Packed low16 = break depth, bits16..17 /8 = break offset. Wave additionally uses bits23..26 /16 and bits19..22 /16 for intensity and bits27..30 for exponent. At 2500ms p19 gives phase0.5 scale0.125. Shore parameters are preserved but not applied without terrain/depth. Preview uses procedural normals and wave intensity/exponent as an explicitly labeled approximation; no shoreline foam is fabricated.
- Category3 remains unresolved and uses a base-material fallback.

## HDR

IComponentDefinition creates textureStore 9 and texturesHdrStore 37; TextureLoader.method649 samples both images. method650 expands grayscale to RGB, then method649 uses packedRGB & 0xfff, scales by31/4096 and adds1. It multiplies normalized source RGB and retains alpha. The HTTP float RGBA projection keeps values above1, top row first. DataTexture rows are inverted to match the existing ImageBitmap orientation and are treated as linear inspection intensities. Three's default output transform remains in use; client gamma/tone mapping parity is not claimed. Missing companion data leaves the ordinary PNG preview and displays a retry action.

## Ownership and limitations

No additional clock or animation loop. ModelScene's render callback updates offsets and material uniforms from its replacement epoch. GPU resources use existing replacement/dispose paths. CacheModelPreview owns requests and decoded bitmaps, including late completion and cancellation. Static topology updates for skeletal frames do not reset material time. Water noise, environment, fog, depth, sun and shore inputs are unavailable in an isolated model viewer, so labels explicitly describe approximations. Browser checks remain waived.

## Verification completed 2026-10-30

- 45 targeted cache codec tests pass, including packed material parameter layout, HDR low-bit extraction, grayscale expansion, alpha, dimensions and existing geometry/animation fixtures.
- 58 cache-service tests pass, including float HDR output, compatible PNG output, missing/mismatched companions and invalid IDs. CacheEndpointsAuthorizationTests.MapCacheEndpoints_AppliesCacheAdminPolicyToGroup verifies that all rendering routes inherit the unchanged cache policy; existing CacheAuthorizationTests cover role/scope decisions.
- 30 shared viewer tests pass. New cases cover explicit-time source formulas, effect classification, shader hook/uniform binding for all seven approximate categories, unlit effect6, mixed transparency, HDR row orientation, replacement epoch, continuous material time during posing and GPU-resource disposal.
- 231 admin tests pass. New HDR retry and in-flight cancellation assertions run alongside existing autoplay, loop, switching, missing-model and bitmap-lifetime regressions.
- Both Angular production builds pass. Affected .NET projects build through the tests and Aspire cache rebuild. Aspire reports the cache resource healthy after rebuilding.
- git diff --check passes. Existing ImageSharp vulnerability/package warnings and test polyfill warning remain; no new compiler diagnostics were introduced by this work.

Browser checks were skipped at the user's explicit request. Shader tests exercise real Three material/shader source construction with a mocked GPU, not GPU shader compilation or visual parity. No new browser or screenshot evidence is claimed. Water/environment previews are explicitly labeled approximations, effect6 matches the lighting-disable state, HDR sample math and scroll/parameter formulas are source-verified, and category3/unknown effects remain visible fallbacks.
