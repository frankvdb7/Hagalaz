# Design

## Context

See [proposal.md](proposal.md) and [the sprite studio spec](specs/cache-sprite-studio/spec.md) for the goal and observable contract. The route already has `CacheSpriteDefinitionsStore` for ID-based metadata, `CacheSpritesStore` and `CacheSpritesService` for image requests and replacement, a Material write-confirmation dialog, and a standalone sprite utility page. The replacement API recreates the complete frame collection.

## Goals / Non-Goals

**Goals:**

- Make the existing sprite detail route a dedicated Material workspace.
- Keep metadata, frame previews, replacement, and temporary object URLs under explicit existing owners.
- Isolate independent asynchronous operations so metadata activity cannot suppress a frame preview or mutation.

**Non-Goals:**

- Change backend routes or mutation semantics.
- Replace the standalone utility page or introduce a new store/service layer.
- Add a canvas renderer or local persistence for edits.

## Decisions

### Detail composition

Expand `SpriteDefinitionsDetailPageComponent` into the route-level workspace and replace its generic definition wrapper composition. Reuse `CacheSpriteDefinitionsStore` for route metadata and the existing sprite service/store for frame bytes and mutation. Keep standalone search in `SpritesManagerPageComponent` available on its existing route; share its existing API/service behavior without requiring the detail page to render its utility forms.

**Alternative considered:** Keep the generic definition card and place the new editor inside it. Rejected because the screenshot calls for a single three-region inspector and the current generic shell would duplicate metadata and compete for the page hierarchy.

### Request and state ownership

- `CacheSpriteDefinitionsStore` owns the route ID and authoritative metadata resource. Route ID changes drive the metadata resource, which is also the source used to reconcile a replacement result.
- `CacheSpritesStore` owns the currently requested frame blob and replacement operation/result. Its frame-preview request uses latest-request semantics so a slow earlier frame cannot win. Replacement remains a single non-overlapping mutation operation. Metadata, frame preview, and mutation receive independent pending/error state so one operation does not filter or hide another.
- The route component owns selected frame and presentation-only state (zoom, fit, background mode). The extracted sprite inspector owns the candidate file, validation, before/after preview, and confirmation dialog. It clamps the selected index against refreshed metadata and resets it when the sprite ID changes.
- The component that creates each object URL revokes it when its source blob changes and on destruction. Do not copy blobs or URLs into a second store.
- A confirmed mutation is reported only from the server response. The route component initiates one metadata refresh and one current-frame refresh after that success; failed or canceled mutations do not enter that path. On a successful refresh with zero frames, show the empty state; otherwise clamp the frame index before requesting the display frame.

**Alternative considered:** Put metadata, preview, file selection, zoom, and mutation into a new editor-specific store. Rejected because the existing route metadata resource and sprite store already own the backend data and operations; another store would duplicate ownership and make refresh behavior harder to trace.

### Frame browser and preview

Use CDK virtual scrolling to request only visible or near-visible thumbnails. Thumbnail components use local instances of the existing sprite store for their preview and error state. The detail route and standalone utility each provide a scoped `CacheSpritesService`, which shares frame-image responses by sprite ID and frame with `shareReplay`. This avoids duplicate requests when a thumbnail and the main preview need the same bytes. Failed requests are evicted for retry, successful replacement invalidates the responses, and the page scope releases the retained responses on navigation away. No new service or store is introduced. Load decoded PNG thumbnails for visible or near-visible rows in the frame browser, and load a selected frame for the central `<img>` preview; do not request every frame just to populate a long list. Use image elements with pixelated scaling and contain sizing; zoom and background mode are view state only. Use Material controls and accessible labels, tooltips, status/error text, and established portal theme overrides. Download the current selected frame using its existing preview URL; do not create an additional download URL.

**Alternative considered:** Draw frames into a canvas or prefetch all thumbnails. Rejected because the current API already returns decoded PNGs, canvas adds complexity without required editing behavior, and eager frame requests can be expensive for sprites with many frames.

### Replacement flow

Keep candidate-file validation and the before/after preview in the focused `SpriteInspectorComponent`. Its template and file lifecycle are separate from frame navigation and zoom, keeping both templates understandable without lint exclusions. Validate the PNG type and decodeability before enabling confirmation; use a temporary local object URL with the same cleanup owner. Reuse `CacheWriteDialogComponent` for the explicit confirmation, changing its message to state whole-sprite replacement. Submit through `CacheSpritesService.replaceSpriteImage()` via `CacheSpritesStore`. Preserve the candidate preview after request failure and clear it only when replaced, canceled/reset, or the route ID changes.

**Alternative considered:** Add an upload endpoint or change the endpoint to frame-level replacement. Rejected because the current server accepts a complete replacement image and reconstructs the frame collection; endpoint changes are outside this proposal.

## Risks / Trade-offs

- **Many-frame sprite lists cannot show all real thumbnails without requesting each frame** → Load only the visible or near-visible thumbnail window and the selected preview; reuse shared responses when the same frame appears in both.
- **Concurrent image requests may complete out of order** → Latest-request handling owns preview selection; tests cover rapid selection and route ID changes.
- **PNG metadata may differ after replacement** → Re-read metadata once from the authoritative resource and clamp/reset selection before requesting the new preview.
- **Browser image decoding can fail for corrupt PNG data** → Keep the existing sprite visible, report validation feedback, and do not submit a file that cannot be decoded.

## Migration Plan

No data or API migration is required. Ship the route presentation with the existing backend contract. Rollback consists of reverting the frontend change; no persisted state is written by the editor apart from the existing confirmed replacement operation.
