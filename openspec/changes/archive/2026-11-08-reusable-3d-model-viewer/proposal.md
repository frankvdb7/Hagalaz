# Proposal

## Why

Administrators cannot inspect the cache's 3D assets. A shared viewer can also support future web application features.

## What Changes

- Decode model, texture, material and animation data on the server and expose authenticated renderer-neutral data.
- Provide a shared viewer and an admin browser for raw models and item, NPC and object definitions, with associated animations and verified effects.
- Replace the Item Forge preview placeholder with the item's inventory model.
- Report unavailable or unsupported assets and effects explicitly.

## Acceptance criteria

1. Administrators can select models and definitions, inspect textured geometry and play available associated animations.
2. Item Forge displays the corresponding inventory model using the shared viewer.
3. Both web applications can consume the viewer without admin or authentication dependencies.
4. Rendering data respects existing cache authorization and does not break existing APIs.
5. Fixture, API, UI, build and browser checks verify supported behavior and document fidelity gaps.

## Non-goals and stop conditions

No cache mutation, game client replacement, world renderer, or separately published package. Record unverified effects and formats as unsupported; stop if the requested behavior requires a materially different scope or a new state owner.

## Capabilities

### New Capabilities

- `cache-model-viewing`: inspect cache model and definition rendering data in the admin viewer.

### Modified Capabilities

None.

## Impact

Cache codecs and cache service, shared Angular code, admin routes, Item Forge, and a Three.js dependency. Reuse existing cache reads, authorization, definition codecs, and admin navigation.
