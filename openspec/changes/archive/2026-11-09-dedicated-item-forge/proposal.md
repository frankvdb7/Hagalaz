## Why

Items currently use the generic cache definition form. Administrators need a dedicated read-only workspace that explains item fields, ordered actions, and explicit cache relationships without suggesting unsupported edits.

## What Changes

- Replace the existing item detail view with Item Forge, reusing its route, service and signal store.
- Add a breadcrumb autocomplete picker, Material tabs and an identity inspector with a reserved preview.
- Expose six decoded read-only fields for note/lending/bought relationships and the inventory model.
- Document the prerequisites for a future item mutation endpoint.

## Capabilities

### New Capabilities
- `cache-item-forge`: Read-only item inspection and navigation.

### Modified Capabilities
- None.

## Impact

Admin item detail page, shared toolbar breadcrumb, item DTO mapping and focused tests. Existing authentication, other cache types and sprite editing remain unchanged.

## Scope boundary

No item writes, model rendering, new price sources, gameplay bonuses or runtime instance editing. No inferred relationships or new state stores.

## Acceptance criteria

1. The existing item route offers a breadcrumb autocomplete picker and five Material tabs on desktop and mobile.
2. Returned values preserve null, zero, false and ordered action slots; explicit relationship links accept ID zero.
3. Loading, errors, retry and route changes work through the existing item store and service.
4. Six additive DTO fields come directly from the decoder and have regression tests. No save operation is offered.
5. Mutation prerequisites are documented; relevant automated and visual checks pass or concrete limitations are reported.

## Stop conditions

Record any requested write API, rendering pipeline or server gameplay data integration as separate work.
