## 1. Underwater terrain

- [x] 1.1 Decode and expose `um` and `ul` data with decoder, API and real-cache checks.
- [x] 1.2 Render the layer and depth fog in the map preview with model, fog and geometry tests.

Validation: 10 map decoder tests, 212 cache service tests, 345 admin tests, 82 shared model-viewer tests, `lint:biome`, `fallow:audit`, the admin production build and a real-cache probe of all 1,577 `um` archives passed. The user confirmed the manual visual comparison against the game client passed on 2026-11-09; no capture was attached in this message.
