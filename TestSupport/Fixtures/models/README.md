# Model projection regression fixture

## Object 45291, shape 10: software-client first-texel fallback

`model-48743.dat` is the real 111-vertex/171-face payload from the same revision-742 source. SHA256: `437f44670ef9de324c2cbaa8fc6a872646745ce366ef2e4473509502ca9b09ca`.

The original compiled client projection helpers produce nonfinite UVs on exactly 47 faces, recorded as the decoder test's expected face-ID list. A valid face-0 UV golden is also captured from the original helpers. Strict decoding must still reject the model; the rendering opt-in returns finite placeholders plus explicit first-texel face IDs. Those placeholders are not used as projected UVs: the shared renderer samples the original first texel centre independently of normal scrolling/transforms, retaining texture alpha. All positions, indices, texture IDs and valid UVs remain intact. API regressions cover all four rotations, single/batch equivalence, warnings, the raw model endpoint and face-ID offsets when two copies are assembled.

## Object 45152, shape 22

`model-48859.dat` is a 446-byte revision-742 model from the same cache source, captured via `ReadContainer(7, 48859)`. SHA256: `b2b4400faba82a2eae8d7699c82a24d864c399ccc23914c9262ca2e96e251165`.

Its zero-Y box mapping covers sloped faces 31..37. Select the Y projection when the oriented Y normal is nonzero even if it is not dominant, so the valid X/Z UV plane does not use the collapsed Y scale. The regression includes this small real fixture, all four single/batch appearance rotations and an analytical sloped-face UV check across model versions 12/14/15.

## Object 45092, shape 0

`model-48778.dat` comes from the same revision-742 cache source described below, captured read-only via `ReadContainer(7, 48778)`. It is 4607 bytes; SHA256 `a930adc149845e157cdaa773350eb481209d1773622869ab726a14aadc825205`.

Its zero-X box mappings include sloped faces. The client's scaled-normal selector falls through to negative-X side 5 when X scale is zero. Preserving that side explicitly keeps the Y/Z projection finite without relying on NaN comparisons. Using the unscaled normal's dominant axis unconditionally breaks this model, so both real fixtures must remain covered together. The API tests exercise single and batch appearance reads in all four rotations using these exact payloads and minimal decoded shape-0 definitions.

## Object 33858, shape 0

`model-36668.dat` is the decompressed index-7 model payload used by object 33858, shape 0, from the revision-742 GameClient `build/ui-test-cache`. Source repository: `frankvdb7/Hagalaz.GameClient`, commit `2d6d99555c87d23d5909ff0f9dfea1e58fa5bdda`, matching the map fixture provenance. Captured read-only through `ICacheAPI.ReadContainer(7, 36668)` on 2026-11-04.

Payload length: 1525 bytes. SHA256: `522caf7881d2dfbcfebf8dfde37bfebc5d275514e4e69c25b760f731ec5e0077`.

The uncorrected decoder throws `Degenerate texture projection cannot produce finite coordinates.` Box mapping 6 has scale `(5120, 0, 32768)` and is referenced by faces 10 and 11. The zero-scale Y axis is unused by the correct projection plane, but dividing the scaled normal by its scale produces NaN during side selection. The regression retains real geometry and validates finite, nontrivial UVs rather than replacing failed projections with placeholders. Ordinary client-derived projection goldens remain unchanged.
