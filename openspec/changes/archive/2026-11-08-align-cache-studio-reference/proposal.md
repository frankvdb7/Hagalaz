## Why

Cache Studio should reflect the supplied reference while keeping its real cache workflows and available data.

## What Changes

Align the cache workspace header, category tabs, search panel and result list with the reference using existing Material controls and assets.

Provide a row actions menu that opens the existing definition detail page with the current browse context.

## Capabilities

### New Capabilities
- `cache-studio-reference-layout`: Responsive reference-aligned cache browsing presentation.

### Modified Capabilities
None.

## Impact

The Angular admin cache shell and types manager presentation. Reuse existing services, stores, thumbnail endpoints, search, pagination, detail routes and Material theme.

## Acceptance criteria

- A1: Header, tabs, search and results follow the reference and remain usable at narrow widths.
- A2: All existing categories, search, pagination, real thumbnails, detail links, loading and errors retain their behavior.
- A3: Build, lint, focused tests, strict validation and fixture browser checks pass, with visual and live-service limitations recorded.
- A4: Each result exposes an accessible actions menu that navigates to that row's existing detail route and preserves browse context.
- A5: While paging the same category and search query, the store keeps the last successful results and pagination context until the new page loads, so the paginator stays aligned with those rows and keeps its normal appearance; changing the search query does not display stale matches.

## Non-goals and stop conditions

No new backend contracts, fabricated descriptions, speculative filtering, sorting, view modes, data or routes. Existing API capabilities determine visible controls.
