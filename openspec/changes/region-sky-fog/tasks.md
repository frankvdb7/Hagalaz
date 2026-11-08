## 1. Region sky and fog

- [x] 1.1 Decode and expose sky colour and raw fog value with decoder and API regression tests.
- [x] 1.2 Apply sky background and optional linear fog in the map viewport with scene, viewport and page tests.

Validation: 8 map decoder tests (region 12850 decodes sky `0x8DA4C2`, fog value 600), 211 cache service tests, 329 admin tests, 80 shared model-viewer tests, `lint:biome`, `fallow:audit` and the admin production build passed. Live visual comparison against the game client is pending.
