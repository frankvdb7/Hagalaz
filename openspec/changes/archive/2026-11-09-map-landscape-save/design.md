# Design

## Context

See proposal.md for motivation. Edits live in the page's `MapSceneDraft`: transforms keyed by source placement (`null` means removed) plus additions. Index 5 holds each region's landscape as a named `l{x}_{y}` archive. `MapRenderDecoder.DecodeLandscape` reads raw source planes, without the bridge adjustment `MapCodec` applies, and those are the planes the editor shows and edits. `MapCodec.Encode` is not usable: it writes terrain and landscape as one stream, loses terrain opcodes and does not sort locations. `ICacheAPI.Write` already takes the write lock, bumps versions, recalculates checksums and invalidates snapshots.

## Goals / Non-Goals

Goals and non-goals are in proposal.md. This design adds no terrain encoding and no new persistence or locking mechanism.

## Decisions

**The client sends each edited region's complete final placement list, one request per region.** The endpoint is `POST /types/maps/{id}/landscape` with `{ objects: [{ id, shape, rotation, x, y, plane }] }`. Replacing the whole list makes retries idempotent, so a partial failure needs no server transaction. Rejected: sending deltas, which needs placement identity (the format has none, and duplicates are legal) and makes retries unsafe. Rejected: one request for all regions, because cache writes are per-archive and could not be atomic anyway.

**A new `MapLandscapeEncoder` sits beside the decoder.** It orders placements by object ID, then location, keeping input order for ties, and writes the deltas the decoder reads. The endpoint decodes the encoded bytes and compares them with the sorted input before writing, matching the existing "replacement encoding is invalid" rule for definitions.

**Fix `WriteHugeSmart` rather than work around it.** For values above 32,767 the loop overshoots and writes a negative remainder. Real regions can have object ID gaps this large. The loop will write 32,767 only while the remainder is at least 32,767, then the remainder.

**The endpoint reads the existing container before writing.** Reading without keys proves the stored landscape is readable, which protects encrypted regions the editor could not display, and supplies the compression type to keep. The service is the single owner of this check. A decode failure uses the same data-error set as `MapRenderReader`.

**State owners.** The page owns the draft and turns it into per-region lists. `CacheMapSceneStore` owns the request sequence, the `saving` flag and the result. `ICacheAPI.Write` owns persistence. The administrator owns retry: there are no automatic retries.

**Requests run sequentially and stop at the first failure.** This makes "saved these, failed on this one" exact. A signal store method exposes it; HTTP stays in `CacheMapsService`.

**On full success the page reuses `retry()`.** It clears edits and history and reloads, so the screen shows what the cache now holds. Rejected: patching local state, which could diverge from what was stored.

**Save sits in the viewport's more actions menu** using the same `canUndo` / `undo` input and output pattern. The confirmation reuses `CacheWriteDialogComponent`, whose text already warns that the live cache changes immediately.

## Risks / Trade-offs

- [Two administrators edit one region and the later save wins] → Accepted, matching item definition saves. Detection is a non-goal.
- [Placements on tiles or planes the client cannot use] → Ranges are validated. Placement semantics stay the administrator's responsibility, as in the preview.
- [A running game world keeps old placements] → Stated in the proposal. Reloading worlds is out of scope.
- [A process failure mid-write leaves the archive and reference table inconsistent] → Same exposure as every existing cache write.
- [Live cache data is overwritten with no undo after saving] → The confirmation warns of this, and the store's versioning is unchanged. Backups remain an operational concern.

## Migration Plan

No data migration. The cache service must be restarted to expose the endpoint. Rollback is removing the endpoint and menu item; saved landscapes stay valid because they use the format the client already reads.
