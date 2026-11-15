# Proposal

## Why

The admin map editor can move, rotate, add and remove placements, but those edits exist only in the browser and vanish on reload. Administrators need to store them in the cache so the map actually changes.

## What Changes

Goal: an administrator can save local placement edits from the map editor into each edited region's cache landscape archive.

In scope:

- A cache service endpoint that replaces one region's landscape placements, validating them first and refusing a region whose stored landscape cannot be read.
- A landscape encoder that writes the format the existing landscape decoder reads, including object ID gaps of 32,767 or more.
- A fix to `WriteHugeSmart`, which currently writes a negative remainder for values above 32,767 and would corrupt such a gap.
- A **Save changes** item in the editor's more actions menu, behind the existing "Confirm cache update" dialog, saving each edited region and reloading the scene from the cache afterwards.
- Failure handling that keeps unsaved edits so the save can be retried.

Non-goals:

- Terrain, overlay, underlay, height or atmosphere edits, and underwater (`ul`) placements.
- Creating a landscape archive for a region that has none.
- Saving regions whose landscape needs XTEA keys. The editor loads without keys, so it never sees those placements and must not overwrite them.
- Persisting scale. Landscape placements have no scale field, so scale stays preview-only and the editor says so when saving.
- Conflict detection between administrators, redo, Ctrl+S, and pushing changes to a running game world.

Acceptance criteria:

- Saving an edited region and reloading shows the same placements, with terrain and other archives unchanged.
- Re-reading a saved landscape returns exactly the submitted placements, including object ID gaps of 32,767 or more.
- Invalid placements and absent or unreadable landscapes are rejected without any write.
- The editor writes nothing until the administrator confirms, keeps edits on failure, and reports which regions were saved.

Stop conditions: pause and raise a follow-up change if saving requires new archive creation, XTEA re-encryption or changes to reference-table handling.

Reused mechanisms: `ICacheAPI.Write` for locking, versioning, checksums and invalidation; `MapRenderDecoder.DecodeLandscape` to verify encoded output; the admin cache authorization policy; `CacheWriteDialogComponent`; `CacheMapSceneStore` for request state. No new worker, queue, lock or dependency.

## Capabilities

### New Capabilities

- `cache-map-landscape-mutations`: how administrative edits replace a region's stored landscape placements, and how the map editor saves them.

### Modified Capabilities

None.

## Impact

- `Hagalaz.Cache`: new landscape encoder.
- `Hagalaz.Cache.Extensions`: `WriteHugeSmart` fix.
- `Hagalaz.Services.Cache`: new `POST /types/maps/{id}/landscape` endpoint under the existing authorization.
- `Hagalaz.Web.App/src-admin`: maps service, map scene store, map detail page and viewport menu.
- Writes change the live cache immediately. Running game worlds keep their loaded regions until they reload.
