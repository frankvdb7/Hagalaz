# Tasks

## 1. Shared map template

- [x] 1.1 Add `MapRegionTemplate` and `IMapRegionTemplateLoader`, moving cache decode, database object spawns (region-local) and definition lookup out of `MapRegionLoader.PrepareAsync`; register it in DI; verify with template loader tests for cache objects, database spawns and blocked tiles
- [x] 1.2 Make `MapRegionLoader` build objects and floor blocks from the template; verify existing `MapRegionLoaderTests` pass unchanged

## 2. Coordinates and presence

- [x] 2.1 Rename `GetRegionPartData` to `GetRegionPartByAbsolutePart`, expose `GetRegionPartByLocalPart` on `IMapRegion`, remove the absolute `WriteBlock`/`RotateBlock`/`DeleteBlock` wrappers and public `LoadPartObjects`/`UnloadPartGameObjects`/`AddPartObjects`, and use the Y conversion in rotate/delete; verify with tests that rotate/delete by local index on a region with region X != region Y change only the expected part
- [x] 2.2 Remove `DrawRegionDimension` and the `drawPartDimension` overload; verify the solution builds and that a deleted part is absent while a (0, 0, 0, rotation 0) mapping is present

## 3. Part materialization

- [x] 3.1 Add `MapRegionPart.DiscardContent()` and use it with collision unflagging and `FloorBlock` clearing when a part is written, rotated or deleted; verify a runtime-removed static from the old content sends no removal on full update and is unregistered, and runtime `Remove` still records disabled statics
- [x] 3.2 Materialize a part from a `MapRegionTemplate` using the source part X/Y and plane, rotated floor blocks, rotated object positions and `(rotation + r) & 3`; verify copy to a different local part, copy from another plane, and the four rotations of a fixture with wall, wall decoration, floor decoration, 1x1 and 2x3 objects (position, rotation, size, shape, layer)
- [x] 3.3 Verify collision for the four rotations equals a standard region containing the rotated floor block and objects (floor, wall, solid object, range flags, multi-tile footprint)
- [x] 3.4 Implement `MakeStandard(own)` as a full restore; verify load, make dynamic, write/rotate/delete, make standard equals a fresh standard load (mappings, presence, objects, collision)

- [x] 3.5 Address zone updates to the destination part instead of the draw source; verify full and incremental updates for a cross-part, cross-plane mapping use the destination base and plane
- [x] 3.6 Drop pending and prepared updates of discarded objects while keeping other updates; verify both queues with `Rewrite_DropsQueuedUpdatesOfDiscardedObjectsButKeepsOtherUpdates`
- [x] 3.7 Re-flag surviving objects that overlap discarded collision; verify with a cross-part overlapping-footprint regression

## 4. Dynamic creation

- [x] 4.1 Update `MapRegionService.CreateDynamicRegion` to read source descriptors by local index, load each distinct source template, write parts by local index, and stop calling `MakeStandard()` on the source; verify a source with region X != region Y away from origin maps all 4x8x8 parts correctly, and existing `MapRegionServiceTests` readiness/cancellation tests pass
- [x] 4.2 Verify determinism: creation with the source resident and not resident produces the same objects and collision, and runtime objects, ground items and NPCs in the source are not copied; replace `DynamicRegionDimensionTests` with template-based coverage in `DynamicRegionPartTests`

## 5. Integration

- [x] 5.1 Run `dotnet build Hagalaz.sln` and `dotnet test Hagalaz.Services.GameWorld.Tests`; report new warnings and confirm no other callers of removed APIs remain
