## Why

The `/cache/sprites` page is a legacy duplicate of the current sprite browser and Sprite Studio. The user explicitly requested complete removal.

## What changes

Delete the legacy component/template/styles/tests and route. Point shared Sprites navigation to `/cache/types?kind=sprites`, with sprite detail routes in the same resource section. Remove metadata/search service and image-store APIs whose only production caller was the legacy manager.

## Acceptance criteria

- A1: No legacy manager component, route or production reference remains.
- A2: Sprites navigation opens the current sprite browser, stays active on sprite details, and does not simultaneously select Definitions.
- A3: Current sprite previews, replacements, validation and dirty-draft guards retain their behavior and tests.
- A4: Admin build/tests, coverage, lint, Fallow, strict validation and browser navigation checks pass.

## Non-goals

No new sprite tool, API endpoint or compatibility wrapper. Do not remove the current Sprite Studio or its shared image store/service. Keep existing definition browsing APIs and sprite-type support.

## Capabilities

### New capabilities
- `sprite-tool-retirement`: Removal of the legacy sprite manager.

### Modified capabilities
None.

## Impact

Admin sprite routing, shared resource navigation, obsolete legacy page, shared sprite service/store cleanup and related tests.
