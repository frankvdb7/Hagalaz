## Why

Cache Studio needs discoverable model navigation without a second tab hierarchy. The user explicitly replaced the prior Definitions/Models/Sprites row with one row of Items, NPCs, Objects and other cache types, including Models.

## What changes

The cache shell owns the existing icon-bearing Material type navigation. Add Models at the end, reuse the existing model route, remove the former upper row and remove the duplicate navbar from the definition browser. Model lookup uses the same outlined search card/field/action structure. The legacy sprite manager remains removed.

## Acceptance criteria

- A1: Exactly one cache-type tab row contains the existing definition types and Models, with no Definitions resource tab.
- A2: Each tab opens its existing route, reflects query/detail selection and uses existing unsaved-change guards.
- A3: Models uses the shared Material search/result structure; existing preview/reload behavior remains intact.
- A4: Tests, coverage, build, lint, Fallow, strict validation and desktop/phone browser checks pass.

## Non-goals

No backend, new routes, new model listing API or model rendering changes. No second tab state owner or navigation mechanism. No legacy sprite page.

## Capabilities

### New capabilities
- `cache-studio-resource-navigation`: Single cache-type navigation including Models.

### Modified capabilities
None.

## Impact

Cache shell, definition browser presentation, model lookup presentation and regression tests.
