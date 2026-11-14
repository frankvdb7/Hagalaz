# Proposal

## Why

The reusable viewer currently renders advanced materials as ordinary textured surfaces and reports their effects as unverified. Administrators need animated material previews with explicit fidelity information.

## What Changes

- Add source-driven UV scrolling and advanced material previews to the existing shared viewer.
- Cover effect IDs 1, 2, 4, 5, 6, 7, 8 and 9, preserving distinct source identities and parameters. Label any approximation and capability fallback; keep unresolved category 3 unsupported.
- Supply the HDR companion data required for supported HDR materials through the existing protected cache-service boundary.
- Reuse ModelScene's animation loop, texture ownership, cache material decoder and admin request lifecycle. No new dependency, worker or second clock.

## Acceptance criteria

1. Signed scroll bytes produce rawByte / 64 periods per second on the correct axes, verified against client samples at explicit times.
2. Each supported effect has source evidence for its parameters and coordinate/time behavior, a deterministic preview, and an accurate fidelity label. Unknown effects retain an explicit unsupported state.
3. HDR decoding preserves the client companion-image multiplier; missing data is visible and has a documented fallback.
4. Replacement, cancellation and destruction release effect resources through existing owners; ordinary geometry, alpha, animation and texture behavior remain compatible.
5. Focused codec/API/viewer/admin tests and both application builds pass. Browser checks remain waived by the user.

## Non-goals and stop conditions

No world rendering, terrain/shoreline generation, scene-dependent underwater simulation, new asset pipeline, animation tweening or general lighting overhaul. Effects requiring absent scene inputs must expose a labeled fallback rather than invented parity. Stop for a revised design if implementing these previews requires a new subsystem or changes cache authorization.

## Capabilities

### New Capabilities

- `model-material-effects`: source-selected animated material inspection and fidelity reporting in the shared model viewer.

### Modified Capabilities

None. This extends the unarchived reusable-3d-model-viewer change without editing its completed task record.

## Impact

Shared model viewer and admin material presentation, cache material/HDR projections and focused tests. Existing authenticated metadata and PNG APIs remain compatible.
