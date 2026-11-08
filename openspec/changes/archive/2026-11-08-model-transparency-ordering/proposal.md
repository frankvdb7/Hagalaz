# Proposal

## Why

The admin model viewer blends transparency but submits faces in source order and drops cache face priorities. Overlapping transparent surfaces therefore render incorrectly, and priority-sensitive models cannot match the GameClient.

## What Changes

Implement GameClient-aware transparency ordering for admin model previews.

- Preserve uniform and per-face priorities in both supported model formats, assembled models, model API responses and viewer geometry.
- Draw opaque and cutout surfaces before blended surfaces. Respect client face-priority semantics for order-sensitive faces, with camera-dependent depth ordering where compatible with the characterized client path.
- Refresh ordering after camera movement, model animation and animated face-alpha changes while retaining face/material/particle associations.
- Replace the blanket unsupported-ordering warning with accurate supported behavior and any remaining limits.

Acceptance criteria:

1. AC1: Legacy and newer model fixtures preserve default and per-face priorities; assembly preserves priorities from each part.
2. AC2: A priority-sensitive client fixture and overlapping transparent fixture demonstrate the specified submission order, including texture alpha mode 2 and face alpha. Priorities take precedence wherever the client path requires them.
3. AC3: Camera orbit, animation and alpha transitions recompute the correct order without corrupting UVs, materials, topology or particle face references.
4. AC4: Focused tests, affected builds and a browser comparison establish the supported behavior; warnings accurately describe remaining differences.

Non-goals: client lighting, new shader effects, exact per-pixel sorting of intersecting triangles, billboard parity, changing the GameClient, and general renderer redesign.

Stop conditions: if source and fixtures cannot establish a priority-sensitive ordering rule, resolve that uncertainty before implementation. Do not silently replace client priority semantics with a generic depth sort.

## Capabilities

### New Capabilities

- `model-transparency-ordering`: Priority-aware ordering of transparent cache model faces in admin previews.

### Modified Capabilities

None. The existing reusable viewer work is an active change, not a published capability spec.

## Impact

Reuse `ModelGeometryDecoder`, `ModelEndpoints` assembly, `GetModelDto`, `prepareCacheModel`, and the shared `ModelScene` render loop. The shared viewer also serves the other web application, so its behavior must remain compatible. API fields are additive. No new dependencies, services, workers or persistence are needed.
