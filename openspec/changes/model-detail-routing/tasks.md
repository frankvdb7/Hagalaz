# Tasks

## 1. Route model selection

- [ ] 1.1 Add a lazy model detail route/page using existing cache ID validation and breadcrumbs, and make the lookup navigate to it; verify direct entry, model zero, malformed IDs and the Models cache tab through route tests (AC1, AC4).
- [ ] 1.2 Keep lookup/reload/retry available in model details and remove the old local selection owner; verify another-ID navigation, same-ID reload, failed-request recovery and back/forward with no stale model in focused routed page tests (AC2).

## 2. Route inspection tabs

- [ ] 2.1 Expose controlled inspection-tab selection across the detail/preview/workspace boundary and map stable query values at the page; verify every supported tab, absent/unknown query values, default-query removal, direct links and back/forward without refetching or resetting preview transforms (AC3).
- [ ] 2.2 Verify breadcrumbs, existing cache guards and compact embedded preview compatibility, and document the supported URL shape in the relevant admin documentation (AC4).

- [ ] 2.3 Reuse the shared types search/table/paginator for Models, add bounded metadata-only range/capacity endpoints and preserve list page context; verify sparse IDs, range validation, pagination and inspect links in API and routed browser tests (AC4).

## 3. Integration

- [ ] 3.1 Run the affected admin routing/component tests and production build, inspect the diff and record outcomes and warnings; validate this OpenSpec change strictly (AC1-AC4).
- [ ] 3.2 Verify `/cache/models/666?tab=textures` in the browser through refresh, another model selection and history navigation, recording evidence and ensuring the running admin server serves the change (AC1-AC4).
