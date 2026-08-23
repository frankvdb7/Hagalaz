## 1. Removal contract

- [x] 1.1 Change the ground-item removal overloads in `IMapRegionPart` and `IMapRegion` to return `bool`, then verify all implementations compile.
- [x] 1.2 Make `MapRegionPart` remove only the exact reference, return `false` with no side effects when absent, and preserve successful update, destroy, and respawn behavior; verify with focused region tests.
- [x] 1.3 Propagate the result through `MapRegion.Remove` and `GroundItem.Despawn`; verify stale despawn returns `false` and active despawn returns `true`.

## 2. Regression coverage

- [x] 2.1 Add deterministic tests for the real `ItemScript.TakeItem` to `GroundItem.Despawn` to region-removal chain, plus second despawn, same-location replacement protection, and no side effects after a failed removal.
- [x] 2.2 Add or retain coverage for inventory-full pickup and successful respawning/static ground-item consumption; verify existing behavior remains unchanged.

## 3. Validation

- [x] 3.1 Run the focused `Hagalaz.Services.GameWorld.Tests` and `Hagalaz.Game.Scripts.Tests` projects, then build the affected projects and review the cumulative diff for scope compliance.
- [x] 3.2 Validate the OpenSpec change with `openspec validate reject-stale-ground-item-pickups --type change --strict`.
