## Why
The item breadcrumb picker also suits other cache definitions. One shared picker should use each page's existing store and preserve the actual API capabilities.

## What Changes
- Generalize the item picker and render it in every cache detail breadcrumb.
- Reuse the page store and template slot; centralize template registration and cleanup.
- Keep name autocomplete for Items and NPCs. Other types offer exact ID navigation without unsupported name requests.

## Capabilities
### New Capabilities
- `cache-definition-navigation`: Consistent breadcrumb navigation across cache definition types.
### Modified Capabilities
- None.

## Impact
Admin shared picker, generic/item/sprite detail templates and toolbar. No backend or store ownership changes.

## Scope boundary
No new endpoints, archive browser, global definition state or changes to detail editors.

## Acceptance criteria
1. Every routed cache definition exposes its correctly labelled picker in the main breadcrumb.
2. Name searches use only supported APIs; ID zero works for all types.
3. Route selection, keyboard dismissal, focus restoration and template cleanup remain correct.
4. Relevant tests, admin build, Biome, Fallow and OpenSpec checks run; desktop/mobile behavior is inspected.
