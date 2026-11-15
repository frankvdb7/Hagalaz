# Sprite Studio verification

Verified on 2026-10-23.

## Result

The existing sprite detail route now renders a Material frame browser, pixel preview, and properties/replacement inspector. The existing sprite metadata store, binary image store, service endpoints, authentication, and standalone sprite utility remain in use. No backend or package changes were required.

The inspector owns file validation, candidate preview, and confirmation. Frame images are shared by the page-scoped HTTP service so thumbnails and the selected preview do not download the same frame twice. Successful replacement invalidates shared frame responses before metadata and previews reload.

## Automated evidence

`pnpm run admin:test:coverage`: 144 tests passed across 12 files.

| Requirement | Evidence |
| --- | --- |
| Existing route and actual metadata | `renders real metadata in the dedicated route workspace` |
| Empty and single-frame sprites | `handles a sprite with %s frames without requesting nonexistent frames` |
| Frame selection and keyboard navigation | `navigates frames by keyboard and ignores out-of-range selection` |
| Lazy thumbnails | `requests a bounded visible window of real thumbnails for long lists` |
| Zoom, background, and PNG export | `changes zoom and background and downloads the selected PNG` |
| Invalid PNG validation | `rejects invalid file types and undecodable images before confirmation`; `discards an obsolete image decoder and validates usable PNG dimensions` |
| Before/after preview and canceled confirmation | `shows a before/after preview and sends no mutation when confirmation is canceled` |
| Whole-sprite replacement and reconciliation | `replaces the whole sprite, refreshes metadata once, and clamps the selected frame`; `clears the preview when refreshed metadata has zero frames` |
| Replacement failures and retry | `keeps the candidate and original preview after a failed replacement and allows retry`; `does not show a late mutation failure as an error for a different sprite` |
| Route changes and preview identity | `cancels old frame requests and hides old images when navigating to another sprite` |
| Metadata and preview failures | `rejects invalid route IDs and supports retrying failed metadata`; `preserves a correctly labeled previous frame on preview failure and retries the selected frame` |
| Independent request state | `loads previews independently while metadata is pending`; `cancels earlier previews and displays the latest selected frame`; `reports a failed search independently and clears it on retry` |
| Preview lifetime | `revokes all preview and candidate URLs when superseded or destroyed` |
| Shared frame responses | `shares decoded frame requests and invalidates them after a successful replacement`; `allows a failed frame request to be retried` |
| Existing standalone utility | All existing `SpritesManagerPage preview lifecycle` tests passed |

## Coverage

Coverage comes from the V8 source-mapped report, including Angular templates. Changed-code metrics intersect the added production source lines in the working-tree diff with executable coverage locations; newly added production files are counted in full.

| Scope | Statements | Branches | Functions | Lines |
| --- | ---: | ---: | ---: | ---: |
| Changed production code | 94.64% (565/597) | 92.35% (181/196) | 85.58% (89/104) | 100% (296/296) |
| Admin suite | 93.49% | 92.29% | 88.51% | 95.09% |
| Sprite Store | 100% | 100% | 100% | 100% |
| Sprite HTTP service | 100% | 100% | 100% | 100% |

The new detail, inspector, and thumbnail TypeScript files each have 100% line coverage. Remaining partial coverage includes template event callbacks and fallback branches. Browser interaction checks exercise the primary rendered controls separately.

## Build and quality checks

- `pnpm run admin:build`: passed; initial production bundle 427.56 kB.
- `pnpm run lint:biome`: passed, no findings.
- `pnpm run fallow:audit`: changed-code gate passed. Fallow continues to report seven inherited sprite-service member findings and two inherited clone groups. No exclusions or quality thresholds were changed.
- `openspec validate dedicated-sprite-studio --strict --no-interactive`: passed.
- `git diff --check`: passed.

The test compiler still reports the existing `src/polyfills.ts` TypeScript-program warning. The admin production build reports no warnings.

## Browser evidence and limits

Playwright with Edge verified the running admin app at 1440 × 1100 and 390 × 844. Temporary HTTP fixtures supplied authentication and sprite responses using the repository's existing Hagalaz image asset. No fixtures or screenshots were added to production source.

- Frame keyboard navigation selected the requested frame.
- Zoom and solid-background controls worked.
- Export downloaded `sprite-128-frame-1.png`.
- Choosing a valid local PNG showed original/replacement images before submission.
- Canceling confirmation sent no mutation; confirming sent exactly one existing POST operation.
- Successful replacement refreshed the frame count and preview.
- Each initial frame was fetched once; frame zero was fetched once more after replacement invalidated the response cache.
- The narrow layout stacked the inspector below the preview without document overflow.
- The browser reported no runtime errors.

Desktop, replacement, and narrow-layout screenshots were inspected and retained outside the repository. Live authentication and cache persistence were not exercised. The existing backend already supports all implemented operations; no additional backend capability is required by this change.
