## 0. Cache census

- [x] Count textures and floor definitions and record test regions.

## 1. Water noise

- [x] Port and verify the three volumes and turbulence tables against client-generated samples. Measure generation and verify shared GPU resource disposal.

## 2. Shoreline depth

- [x] Verify client depth qualification and add a map-only vertex attribute. Test with and without underwater terrain and verify prepared geometry reuse.

## 3. HD water

- [x] Port effects 8 and 9, including atmosphere, blend state and base fallback. Verify shader source and real WebGL linking.

## 4. Fixed-function water

- [x] Resolve and port the effect 2, 4 and 5 combine stages. Update effect descriptions and verify shader source and WebGL linking.

## 5. Floor edges

- [x] Verify client corner weighting and add water blending if the existing path can preserve the effect. Record the reason if this phase must be deferred.

## Final verification

- [x] Run admin and public tests, changed-code coverage, Biome, admin build, Fallow and strict OpenSpec validation.
- [ ] Check the authenticated editor console and verify HD water animation and the water-surface toggle in region 15177.
- [ ] Compare the editor and client side by side.
