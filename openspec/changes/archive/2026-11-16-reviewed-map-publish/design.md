# Design

## Ownership

The map detail page owns the draft, undo history and the confirmation snapshot. The scene store owns the active region loads, the serial save order and the result for each request. The Cache service owns version checks, XTEA verification and read-back. The page reconciles the draft from the store's confirmed outcomes and is the only owner of retry preparation.

## Review

Generate the review from the same baseline and draft that produce the write requests. Group by region ID, with terrain before landscape within a region. List changed tile fields by source plane and local coordinates. List placements with object ID, local coordinates, source plane, shape and rotation. Use draft transforms to pair before and after placements where possible, and preserve counts for identical placements. Keep the current explanations for edits the cache format cannot store.

Use the existing confirmation dialog. Freeze the reviewed snapshot or invalidate it when the baseline, selection, keys or draft changes. Confirmation authorizes that snapshot only.

## Region keys

The scene loader accepts a per-region key map, but the detail page currently always supplies an empty map. Provide a region-scoped way to supply keys in the existing editor and use the same in-memory map for render, save and reload. Keep keys out of URLs and messages. Accept four signed 32-bit integers with at least one nonzero value. Pass keys for the relevant landscape only. If a region cannot be read, tell the administrator to provide or correct that region's keys and verify the load before publishing. Never retry a failed keyed read or write without keys.

## Recovery

Capture successful, failed and unattempted region/kind requests. Stop at the first failure. Drop the confirmed edits from the pending publish, and reload their authoritative cache state with the original region keys. Incorporate only the confirmed kinds and required rendering dependencies. Keep the baseline, version and placement identity for each unsaved kind, including an unsaved landscape alongside saved terrain. Reconcile undo history so it cannot restore a confirmed write as a pending edit. Keep the existing full-success reload and world-map refresh behavior.

If a confirmed write's reload fails, retain its confirmed status, report the reload failure and retry the read before resuming edits against that baseline. A conflict requires the administrator to review the latest state and resolve the stale edit before publishing it again. A write whose response reports failed verification or whose result is unknown stays unconfirmed. Preserve its draft and report the uncertainty. A read can inform recovery, but must not silently advance the draft's expected version or authorize a retry over a different baseline.

A full scene reset would discard pending edits. Reloading all versions into the old draft would allow retry to overwrite concurrent changes. Replaying the whole batch would hit conflicts on confirmed writes. Per-kind reconciliation avoids those outcomes with the current API.
