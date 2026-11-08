# Proposal

## Why
Item Forge only previews the inventory model, hiding the equipped and head model references already decoded from item definitions.

## What Changes
Add Inventory, Male equipped, Female equipped, Male head and Female head choices to the existing item preview. Expose read-only appearance metadata and reuse the current assembly endpoint with an optional appearance selector.

## Acceptance criteria
1. Metadata lists source IDs and availability for all five appearances; invalid selections return 400 and absent appearances return 404.
2. Equipped parts use client primary/secondary/tertiary selection, normalized units, decoded wear offsets and ordered appearance replacements. Head parts exclude wear offsets. Inventory behavior remains compatible.
3. Item Forge shows model IDs, switches appearances, resets when the item changes and reports missing models. Both applications build and focused API/admin regressions pass.

## Non-goals
No full character assembly, equipment slots/body hiding, player customization overrides, player animation association or cache writes. Browser checks remain waived. No new dependencies or state owners.

## Capabilities
### New Capabilities
- `item-model-appearances`: inspect inventory, equipped and head item models.
### Modified Capabilities
None.

## Impact
Existing model endpoints, admin preview and focused tests. Reuse item codecs, cache authorization, AssembleModel and shared renderer.
