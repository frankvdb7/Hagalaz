# Verification

## Automated checks

- `pnpm run admin:build`: passed.
- `pnpm run admin:test:coverage`: 162 tests passed across 14 files after the breadcrumb revision. Overall lines 95.70%, statements 94.20%, branches 92.62%, functions 88.81%.
- The new breadcrumb picker has 49/50 statements covered; the shell template slot has 4/4. Tests cover name debounce, real suggestions, item-zero selection, errors/retry, Escape/outside dismissal and focus restoration. Detail mapping and route tests remain in place.
- `dotnet test Hagalaz.Services.Cache.Tests/Hagalaz.Services.Cache.Tests.csproj --no-restore --verbosity quiet`: 14 tests passed. Two new endpoint tests use real ItemTypeCodec opcodes to verify exact references, default sentinels and ordered actions.
- `pnpm run lint:biome`: passed, no findings.
- `pnpm exec fallow audit --format json`: warn. No introduced dead-code, complexity or styling findings. Ten inherited service member findings and two service-wiring clone groups remain. The item service resource methods have concrete callers in CacheItemsStore; removing them would break production requests. No suppressions or relaxed gates were added.
- `openspec validate --changes --strict`: 5/5 passed.
- `openspec validate --specs --strict`: 64/64 passed.
- `git diff --check`: passed.

## Browser checks

Microsoft Edge through bundled Playwright, against the real admin dev server with temporary auth/API fixtures outside the repository:

- Desktop 1600 x 1100: detail and inspector columns, no permanent item navigator.
- Phone 390 x 844: one detail column; breadcrumb popup stays inside the viewport.
- All ten sparse action slots appear in their original order.
- Explicit item-zero relationship navigates to the existing route and updates the single toolbar breadcrumb.
- Archive ranges are [0,10), [10,20), [20,21); the next button disables at the end.
- One search result disables next-page navigation.
- Keyboard selection, Escape dismissal, outside-click dismissal, focus restoration and breadcrumb template cleanup after navigation all pass.
- No browser runtime errors observed.

Screenshots and the fixture runner are in the local temporary `hagalaz-item-forge-qa` folder, not production source.

## Limits and existing warnings

The cache API was offline. No live cache data, authentication server or persistence was validated. The supplied shared design thread loaded, but its interactive render was disabled, so comparison used its described layout and the repository's Runic Material theme rather than a pixel comparison with a rendered reference.

Existing admin-test polyfills compilation and backend NuGet vulnerability warnings remain. No dependency versions were changed. Hosted CI was not run for these uncommitted changes.

## Self-review

The original item route/class/service/store remain authoritative. No item mutation, second detail store, fabricated price, rendered image endpoint or adjacency-based relationship was introduced. New DTO fields are direct decoder values shared by detail/range/search mapping. A repeated route-ID validation rule now has one implementation used by generic, sprite and item detail pages. The permanent navigator and its archive/paging state were removed in the approved revision. The routed page supplies its breadcrumb template to a shell-scoped slot, so the picker shares the existing detail store. Search failures remain independent of the selected definition. Browser review caught a phone popup overflow; the CDK overlay now uses bounded width and viewport positioning.

Item mutation and item rendering still require backend work. The smallest typed mutation follow-up is documented in `Hagalaz.Web.App/docs/item-forge.md`.
