# Reviewed map publishing

## Why

Cache Studio asks for confirmation without listing the exact edits. When a later save fails, the retained draft can resend requests that already committed and hit a version conflict.

## What changes

- Let administrators review exact static cache edits and publish the reviewed batch. Show that requests commit independently and do not update live spawns or hot-reload the running world.
- Use supplied region-specific XTEA keys for protected landscapes. Reject unreadable landscapes with a recovery message and keep their drafts.
- Report saved, failed and unattempted region/kind requests. Reload confirmed saves, keep unsaved edits and versions, and make retry skip confirmed saves.

## Capabilities

### Modified capabilities

- `cache-map-landscape-mutations` covers review, protected landscape publishing and recovery.
- `cache-map-terrain-mutations` covers review and per-kind recovery for terrain.

## Impact

Reuse the editor's existing draft, dialog, scene store, render and save endpoints. Keep the Cache service's version checks and write verification authoritative. Issue [#557](https://github.com/frankvdb7/Hagalaz/issues/557) is complete when its focused regressions and editor build pass and the review behavior is verified. Disk-backed revision-742 verification belongs to #556. Draft history redesign, live spawns, world hot reload and cross-region transactions are out of scope.
