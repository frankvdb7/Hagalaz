# Proposal

## Why

`MapRegion.FlagStandardObject` and `UnFlagStandardObject` repeat the same rotation-based width and height calculation for a standard object's collision footprint. Keeping the geometry in both operations risks flagging and unflagging different tiles after a future change.

## What Changes

- Extract the shared rotation-based size calculation into a private instance method on the existing `MapRegion`.
- Keep each public method's collision mutation and its distinct guard behavior unchanged.

## Scope and Acceptance Criteria

- Only the standard-object size calculation in `CollisionMethods.cs` changes.
- Both public methods use the same rotation-to-dimensions rule: rotations 1 and 3 swap `SizeX` and `SizeY`; other rotations keep them.
- Flagging retains its early return for non-clipped, non-gateway objects; unflagging retains its existing behavior.
- Existing public-method tests verify the rotated footprint for all four rotations and baseline restoration.
- If consolidating the size calculation requires changing iteration order, collision flags, or flag/unflag guards, stop and propose a separate change.

## Non-Goals

- Do not combine flagging and unflagging behind a mode flag, callback, or shared mutation loop.
- Do not refactor wall-object collision logic or other exact clones.
- Do not add a static helper, new class, interface, or dependency.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

None. This is an implementation-only refactor; standard-object collision behavior remains the contract.

## Impact

- `Hagalaz.Services.GameWorld/Model/Maps/Regions/CollisionMethods.cs`
- Existing direct public-method tests in `Hagalaz.Services.GameWorld.Tests/CollisionMethodsTests.cs`

No API signature, dependency, registration, or runtime topology changes are required.
