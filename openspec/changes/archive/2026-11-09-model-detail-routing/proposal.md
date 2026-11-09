# Proposal

## Why

Model selection originally lived only in ModelsManagerPageComponent. Refreshing or sharing `/cache/models` loses the selected model, unlike the other cache detail tabs.

## What Changes

Make the selected model an addressable admin detail view.

- Use the existing shared search/table/paginator at `/cache/models` as the model browser and add `/cache/models/:id` as the canonical model detail URL.
- Navigate the lookup form to the detail URL and derive the displayed model from the route.
- Persist the active inspection tab with stable query values such as `?tab=textures`; omit the default Model tab.
- Keep the Models cache tab selected and provide the same breadcrumb/detail navigation conventions as other cache resources.

Acceptance criteria:

1. AC1: Loading model 666 opens `/cache/models/666`; direct navigation and refresh restore the same model. Zero is valid; malformed IDs produce an invalid-ID state without a model request.
2. AC2: Changing models, browser back/forward, retry after failure and same-model reload work without stale model state.
3. AC3: Textures, recolors, retextures, animations and metadata tabs round-trip through stable query values. Missing/unknown values show Model. Changing only the tab does not reload the model or reset its preview transforms.
4. AC4: The Models tab uses the existing shared search/table/paginator, lists only present model IDs, supports exact-ID lookup and preserves page context when returning from details.
5. AC5: Detail breadcrumbs and selected Models cache tab remain correct, existing compact previews continue working, and focused routing tests plus admin build pass.

Non-goals: model-name search, model thumbnails, persisting camera/transforms, selected texture, playback time or animation selection, backend API changes, and returning Preview notes to the UI.

Stop conditions: do not introduce a second selected-model owner or expand URL state into a general workspace serialization system.

## Capabilities

### New Capabilities

- `model-detail-navigation`: Addressable model inspection and browser-history navigation in Cache Studio.

### Modified Capabilities

None. Existing published capabilities do not specify model detail navigation.

## Impact

Reuse cacheDetailRoutes, Angular Router, injectCacheDefinitionId, PortalBreadcrumbDirective and the existing model workspace and lookup form. Update the preview/workspace boundary only to expose controlled tab selection. Reuse the shared range-store feature. Add bounded metadata-only model range and capacity endpoints from cache index 7; no model geometry is decoded for list rows. No new dependencies.
