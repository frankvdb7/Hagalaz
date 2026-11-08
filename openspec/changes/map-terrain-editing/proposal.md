# Proposal

## Why

The map editor can change and save objects, but the ground under them is read-only. Administrators need to reshape terrain and repaint floors from the same editor instead of editing cache data by hand.

## What Changes

Goal: an administrator can edit terrain heights and paint floors in the map editor, preview the result, and save it to each edited region's cache terrain archive.

In scope:

- Brush tools for a chosen surface plane: raise, lower, flatten, paint underlay, paint overlay with shape and rotation, and clear overlay, with a brush size.
- A live preview that also updates objects resting on the changed ground, and one undo step per brush stroke.
- A floor catalog endpoint and a floor picker, because the render response only describes floors already used in the region.
- A cache service endpoint that changes individual terrain tiles and leaves every other byte of the archive as it was.
- Saving through the existing **Save changes** item and "Confirm cache update" dialog, alongside object edits.

Non-goals:

- Tile flags (bridges, blocking and other gameplay values), underwater terrain (`um`), atmosphere and light records.
- Creating a terrain archive for a region that has none, or saving a terrain archive that cannot be read.
- Editing corners owned by a region that is not loaded.
- Fill, smoothing or noise tools, conflict detection between administrators, and pushing changes to a running game world.

Acceptance criteria:

- Saving and reloading shows the edited heights and floors, and unedited tiles decode identically, including automatically generated heights.
- Tile flags and the data stored after the terrain in the same archive are byte-identical after a save.
- A save with no effective tile changes reproduces the archive byte for byte.
- Heights the format cannot represent cannot be saved, and the editor never previews a height that would be stored differently.
- Undo restores the previous terrain one stroke at a time, and a failed save keeps all edits for retry.

Stop conditions: pause and raise a follow-up change if terrain edits need flag or collision changes to be usable, or if a brush stroke cannot be previewed at an acceptable speed without restructuring terrain rendering.

Reused mechanisms: `ICacheAPI.Write`, `MapRenderDecoder`, `MapScenePreparation`, `ModelScene.pickSurface`, `MapSceneDraft` and undo, `CacheMapSceneStore` saving, `CacheWriteDialogComponent`, and the admin cache authorization policy. No new worker, queue, lock or dependency.

This builds on `map-landscape-save`, which must be applied first.

## Capabilities

### New Capabilities

- `cache-map-terrain-mutations`: how administrative edits change stored terrain tiles and how the map editor edits, previews and saves them.

### Modified Capabilities

None.

## Impact

- `Hagalaz.Cache`: new terrain tile patcher and floor catalog read.
- `Hagalaz.Services.Cache`: new `POST /types/maps/{id}/terrain` and `GET /types/maps/floors` endpoints under the existing authorization.
- `Hagalaz.Web.App/src-admin`: maps service, map scene store, map detail page, viewport, scene geometry and a new terrain tools panel.
- Saves change the live cache immediately. Height changes can make existing walkable areas look different without changing collision, because flags stay as they are. Running game worlds keep their loaded regions until they reload.
