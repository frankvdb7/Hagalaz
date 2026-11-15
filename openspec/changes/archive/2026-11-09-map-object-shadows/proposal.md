# Proposal

## Why

The client darkens floors around walls and ground objects. The admin map editor draws floors without this, so buildings and objects look like they float on evenly lit ground.

## What Changes

Goal: draw the client's object-cast floor shadows in the admin map editor.

In scope:

- A per-placement shadow strength from the Cache API, because it depends on the object definition and the assembled model.
- The client's shadow grid, filter and floor darkening for walls, wall corners and ground objects.
- Shadows that follow draft edits and hidden scene groups.
- Delaying shadow updates until a load settles, so progressive loading does not rebuild the terrain repeatedly.

Non-goals:

- Terrain slope lighting, point lights or any change to object lighting.
- Shadows cast from objects in neighbouring regions that are not part of the selection.
- Bridge-adjusted effective planes; floors and objects stay on their source planes.

Existing mechanisms reused: `GetObjectRenderAppearanceDto`, `MapScenePreparation` terrain caching, and the floor shading path used for water fog.

Acceptance criteria:

- Shadow cells, maximum-write behaviour and the asymmetric integer filter match the client research.
- Darkening uses the client's brightness of 74 minus the interpolated shadow, relative to unshadowed ground.
- Walls cast at strength 50. Static ground objects cast at their radius over 4 capped at 30, and animated or state-driven ones at 15.
- Objects whose definition disables shadows cast none.
- Moving, removing or hiding an object updates the shadow after the edit.
- Loading more assets does not rebuild the terrain until the load settles.

Stop conditions: if the shadow field cannot be derived without a second object-placement owner, stop and report.

## Capabilities

### New Capabilities

- `map-object-shadows`: How the admin map editor darkens floors under objects.

### Modified Capabilities

None.

## Impact

Cache API object appearance DTO, admin map scene geometry and page scene composition. The Cache service must be restarted to provide the new field.
