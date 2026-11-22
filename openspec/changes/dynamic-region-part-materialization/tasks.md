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
- [x] 3.7 Re-flag surviving objects that overlap discarded collision; including survivors in resident adjacent regions; verify with cross-part and two-region overlapping-footprint regressions
- [x] 3.8 Keep raw object planes and terrain flags in the decoded map and template, rebuild whole columns with recorded content, and apply bridge planes at the destination; verify `DynamicChunks_FollowClientBridgePlanesForRegion12850` against a client-rule reference for all rotations, and the region 12850 standard and restore tests
- [x] 3.9 Remove runtime objects of a replaced part while keeping objects other columns placed there; verify `Delete_RemovesRuntimeObjectsButKeepsObjectsPlacedByOtherColumns`
- [x] 3.10 Keep plane-0 objects under a bridge without collision, yielding to objects with collision, in dynamic parts and standard loads; verify `PlaneZeroObjectUnderABridge_IsKeptWithoutCollisionUntilTheBridgeGoes`, `LoadAsync_AddsPlaneZeroObjectUnderABridgeWithoutCollision` and the 12850 client-rule test
- [x] 3.11 Rebuild columns that read an edited plane-1 chunk's bridge flags; verify `ChangingABridgeChunk_RebuildsColumnsThatReadIt`
- [x] 3.12 Unflag a replaced object's collision and re-flag overlaps; flag an added object before its spawn hook and drop collision of objects the hook removes; verify `ReplacingAnObject_ClearsTheReplacedFootprintUntilTheChunkIsDeleted` and `SpawnHookThatRemovesTheObject_LeavesNoCollisionAndKeepsOverlaps`
- [x] 3.13 Rebuild columns whose objects an edited column displaced from a shared slot, building columns in ascending order; verify `DeletingAChunkThatDisplacedAnObject_RestoresTheDisplacedObject`
- [x] 3.14 Track content per part and rebuild only affected parts, arbitrating shared slots by client load order; verify `RewritingTheLosingChunk_KeepsTheWinnersObjectWhateverTheEditOrder`, `RewritingTheLosingChunk_KeepsARuntimeRemovedWinnerRemoved` and `EditingPlaneZero_LeavesARuntimeRemovalOnPlaneOneAlone`
- [x] 3.15 Rebuild bridge readers only when the effective bridge mask changes, keep runtime removals of unchanged and beaten parts, index template source objects per part and coalesce slot-replacement re-flagging; verify `RewritingPlaneOneUnchanged_LeavesPlaneZeroRuntimeRemovalAlone`, `BeatenChunk_KeepsItsRuntimeRemovalWhenTheWinnerIsDeleted` and `SourceObjectIndex_ReturnsOnlyThePartsObjectsInSourceOrder`
- [x] 3.16 Reuse live objects and carry removals of all template-owned objects through dependency-only rebuilds; verify `BridgeDependencyRebuild_KeepsARemovedConfiguredSpawnRemoved` and `BridgeDependencyRebuild_KeepsAnObjectChangedInPlace`
- [x] 3.17 Replace a part's own stale object when its rebuild produces a different one in the slot, merge the owner/origin registries into one, keep rebuild scratch state local, and express edits as part descriptors validated before applying; verify `NewBridgeUnderAnObject_KeepsItDrawnWithoutCollision` and the existing rebuild regressions
- [x] 3.18 Reuse objects across collision transitions instead of replacing them, keeping runtime removals and in-place changes; verify `NewBridgeUnderARemovedObject_KeepsItRemoved` and `NewBridgeUnderAnObject_KeepsItDrawnWithoutCollision`
- [x] 3.19 Settle live objects to their final collision before slot claims during a rebuild; verify `BridgeUnderTheWinningStatic_LetsTheCollidableConfiguredSpawnTakeTheSlot`
- [x] 3.20 Keep script-unclipped objects unclipped through bridge changes and collision restoration; verify `BridgeRoundTrip_KeepsAScriptUnclippedObjectUnclippedUntilTheScriptRestoresIt`
- [x] 3.21 Clear collision overrides for an occupant that `TryAdd` supersedes, through the shared discard path; verify `ReplacingAScriptUnclippedObject_DoesNotKeepReferencingTheDestroyedOne`
- [x] 3.22 Free the old slot of a live object that the rebuild moves to another plane before lower-ranked claims; verify `StaticMovingOffAPlane_FreesItsOldSlotForALowerRankedSpawn`
- [x] 3.23 Use one placement rule for building and settling objects and read bridge bits from the per-column mask; verify the existing rebuild regressions still pass

## 4. Dynamic creation

- [x] 4.1 Update `MapRegionService.CreateDynamicRegion` to read source descriptors by local index, load each distinct source template, write parts by local index, and stop calling `MakeStandard()` on the source; verify a source with region X != region Y away from origin maps all 4x8x8 parts correctly, and existing `MapRegionServiceTests` readiness/cancellation tests pass
- [x] 4.4 Build identity mappings for a standard source without creating, loading or awaiting it, keeping the descriptor path for a dynamic source; verify `CreateDynamicRegion_DoesNotCreateOrLoadAStandardSourceAndNeverLoadsTheDestination` and `CreateDynamicRegion_WaitsForADynamicSourceAndCopiesItsMappings`
- [x] 4.5 Await a dynamic source through its populating task instead of the normal load scheduler and resume an idle dynamic source; verify `CreateDynamicRegion_WaitsForAnInitializingDynamicSourceWithoutTheNormalLoader` and `CreateDynamicRegion_ResumesAnIdleDynamicSource`
- [x] 4.3 Apply all mappings first and build each column once during creation; verify `CreateDynamicRegion_BuildsEachObjectOnlyForItsFinalPlacement`
- [x] 4.2 Verify determinism: creation with the source resident and not resident produces the same objects and collision, and runtime objects, ground items and NPCs in the source are not copied; replace `DynamicRegionDimensionTests` with template-based coverage in `DynamicRegionPartTests`

## 5. Integration

- [x] 5.1 Run `dotnet build Hagalaz.sln` and `dotnet test Hagalaz.Services.GameWorld.Tests`; report new warnings and confirm no other callers of removed APIs remain
- [x] 5.2 A static removed at runtime stays removed when a dependency rebuild moves it to another plane (bridge added/removed): its tombstone is carried to the final plane
