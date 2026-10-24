# Tasks

## 1. Route workspace and metadata

- [x] 1.1 Replace the generic sprite detail composition with the responsive three-region Material workspace while preserving `/cache/types/sprites/:id`; verify the route renders using the existing metadata store and no generic duplicate metadata card.
- [x] 1.2 Add metadata, zero-frame, loading, failure, and sprite-ID-change behavior; verify with focused detail-page tests that API values are shown and old metadata is not attributed to a new ID.
- [x] 1.3 Add lazy decoded thumbnails for visible/near-visible frame rows plus keyboard-accessible selection controls; verify thumbnails, bounded requests for long lists, frame labels, empty/single-frame states, selection, and keyboard navigation in component tests.

## 2. Frame preview and request state

- [x] 2.1 Update only the existing sprite store request state/operators needed to isolate metadata, preview, and mutation activity and ensure latest-selected-frame behavior; verify focused store tests for independent pending state and rapid frame selection.
- [x] 2.2 Add the pixelated image preview, zoom/fit, checkerboard/solid background, dimensions, loading/error feedback, and selected-frame PNG download; verify preview controls and downloaded content in focused component tests.
- [x] 2.3 Add frame and route request race regression tests, including selected-frame reconciliation after frame-count changes; verify the latest route ID/frame remains visible when earlier requests complete later.

## 3. Whole-sprite replacement

- [x] 3.1 Add local PNG type and decodeability validation with a before/after preview and explicit whole-sprite wording; verify valid and invalid file cases without issuing a mutation for invalid input.
- [x] 3.2 Reuse the existing confirmation dialog and replacement service/store path; verify cancellation sends no request and a confirmed request uses the existing whole-sprite endpoint.
- [x] 3.3 Handle mutation success and failure, refresh authoritative metadata and the current preview once on success, preserve retryable candidate preview on failure, and reconcile the selected frame; verify success/failure/cancel scenarios with focused tests.
- [x] 3.4 Verify temporary preview and download object URLs are revoked when superseded or destroyed, with a focused lifecycle test.

## 4. Integration validation

- [x] 4.1 Verify the standalone sprite utility and current route/API behavior remain available using relevant existing and updated tests.
- [x] 4.2 Run `pnpm run admin:build`, `pnpm run admin:test` with coverage, `pnpm run lint:biome`, `pnpm run fallow:audit`, `openspec validate dedicated-sprite-studio --strict --no-interactive`, and `git diff --check`; report results and measured changed-code coverage.
- [x] 4.3 When browser tooling is available, compare the desktop and narrow-screen route against the attached reference and verify focus, keyboard use, and responsive controls in the running admin UI.

## Workflow follow-up

- Apply the proposal after review, then archive only after all acceptance criteria and validation evidence are complete.
