## 1. Overlay render properties

- [x] 1.1 Decode and expose overlay opcodes 11, 12, 13, 14, 16, 20, 21 and 22 with decoder and API tests.
- [x] 1.2 Apply priority and spreading to map floor vertices with spreading and geometry tests.

Validation: 9 map decoder tests, 211 cache service tests, 338 admin tests, `lint:biome`, `fallow:audit` and the admin production build passed. Visual comparison against the game client is pending.
