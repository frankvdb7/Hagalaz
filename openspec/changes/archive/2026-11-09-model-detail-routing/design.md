# Design

## Context

ModelsManagerPageComponent owns draftId and selectedId locally, and reloading the same ID calls CacheModelPreviewComponent.model.reload. Other cache resources register lazy detail pages in cacheDetailRoutes and validate route IDs with injectCacheDefinitionId. CacheShellPageComponent already recognizes the second URL segment `models`, so it supports both lookup and nested detail paths without a new navigation mechanism. ModelWorkspaceComponent currently owns its inspection tab as a local signal.

## Goals / Non-Goals

See proposal.md. Keep Angular Router as the only owner of addressable selection, with no global model-selection store. Keep preview controls local unless explicitly covered by the tab query contract.

## Decisions

### Separate lookup entry and detail route

Register `models/:id` in cacheDetailRoutes with a model detail page under pages/details. Reuse the existing lookup form through a small shared component or template only if needed to avoid duplicating validation. The entry page embeds TypesManagerPageComponent in models mode, reusing its search, table, paginator, route-query state and error/retry UI. It no longer owns a selected model. The detail page derives its ID from injectCacheDefinitionId and supplies it to the existing workspace preview. Its editable draft ID is form state, not a second selection owner.

Use PortalBreadcrumbDirective to publish lookup/model breadcrumbs as the other detail pages do. Keep the lookup and reload actions available when a request fails. Same-ID submission calls the existing model resource reload; another ID navigates to its route, resets the inspection tab to Model and creates a history entry.

Rejected: adding only `?id=666` to the existing manager. That preserves selection but does not make models a detail view following the other cache resources. Also reject storing selection in localStorage, which cannot represent a shared link or browser history.

### Route state stops at the page boundary

The detail page owns mapping stable tab names to workspace tab indexes. Make preview/workspace tab selection controllable through inputs/outputs or Angular model bindings. Compact previews keep their existing defaults and do not inject routing. All route reads react to paramMap/queryParamMap changes so Angular's component reuse supports back/forward without reconstructing the viewer.

Tab actions navigate with merged query parameters, removing `tab` for Model. Ignore equal state to avoid navigation feedback loops. Tab-only navigation creates normal history entries and does not reload geometry or reset local transform state. Unsupported query values display Model without requiring an extra redirect.

Rejected: injecting ActivatedRoute inside the shared renderer or workspace. The rendering components also serve embedded previews whose route parameters have different meanings.

### Keep data loading and retry ownership unchanged

CacheModelPreviewComponent remains the sole model/texture/animation resource owner. The detail page passes selection; it does not fetch a second copy or implement another retry. Existing error and retry UI remains authoritative. Invalid route IDs prevent preview creation and keep lookup available.

## Risks / Trade-offs

- Query synchronization can reset local state through unnecessary component recreation. Test the same detail instance across tab and ID changes.
- Existing manager tests assume local selectedId. Replace those assertions with routed behavior rather than retaining a compatibility signal solely for tests.
- New detail routes must remain inside existing auth/cache guards. Extend app.routes tests for detail links and active Models navigation.

## Migration Plan

Existing `/cache/models` links remain valid. Add `/models/capacity` and bounded `/models/range` metadata endpoints backed by reference table 7. Sparse IDs are filtered from reference metadata, not by decoding model containers. Capacity is the ID upper bound used by the existing range paginator, not the number of populated entries. No data migration or redirects are needed. Reload the existing admin development server after implementation as needed; rebuild the cache service through Aspire to expose the new list endpoints.
