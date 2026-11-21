# Tasks

## 1. Collision follows the source world

- [x] 1.1 Add the world-aware `GetClippingFlag` read and use it in `IsAccessible`; verified by `MapRegionServiceTests` instance, missing-region and out-of-range cases
- [x] 1.2 Bind the world from the source location in `PathFinderBase` entry points and make `Movement.Tick` and melee reach step-check in the creature's world; verified by `InstanceCollisionIsolationTests` and `MovementTests`
- [x] 1.3 Fail entity-to-entity and entity-to-object paths across worlds; verified by `InstanceCollisionIsolationTests`
