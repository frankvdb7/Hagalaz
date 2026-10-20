## 1. Shared lifecycle boundary

- [x] 1.1 Inject the existing `IEntityStore` into map-region ownership code and
  register runtime and loader-added ground items/game objects through
  `MapRegionPart.Add`.
- [x] 1.2 Unregister exact resources on permanent part removal, region
  destruction, and loader rollback; include disabled static objects in region
  destruction.

## 2. Regression coverage

- [x] 2.1 Cover runtime registration, permanent removal, slot generation
  invalidation, and static disable/re-enable identity preservation.
- [x] 2.2 Cover loader registration and region-destruction invalidation for
  loaded resources.
- [x] 2.3 Keep existing region, loader, destruction, and script tests passing
  with concrete world-entity fixtures where identity assignment is required.

## 3. Validation

- [x] 3.1 Run focused identity/loader/destruction and script tests.
- [x] 3.2 Run the full GameWorld test project, solution build, and diff checks.
