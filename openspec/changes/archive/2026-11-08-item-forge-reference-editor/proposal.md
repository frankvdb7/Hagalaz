# Proposal

## Why
The selected Item Forge reference has a stronger editing hierarchy, preview inspector and technical details than the current read-only page. The user explicitly requested real editing and duplication for this design pass.

## What Changes
Match the supplied desktop reference within the existing Angular admin shell: item image/header, state chips, actions/navigation, icon tabs, two-column overview form and stacked preview/technical/actions panels. Load and save the full ItemType definition and support exact definition duplication through existing protected cache mutation boundaries.

## Acceptance criteria
1. Administrators can load and save the complete ItemType through the codec, including model and template fields; legacy unmodeled payloads survive round trips. The form edits common fields and keeps noted status derived.
2. Duplicate creates an exact new definition at a free ID without overwriting existing definitions, using the existing cache write lock and checksum/reference-table owner. It navigates to the created item.
3. The page follows the attached 1651x953 layout with real cache imagery, existing typography/icons, responsive stacking, preview appearance controls and functional tabs/navigation/copy/actions. Dirty navigation, save/duplicate confirmation, loading/errors/retry are handled using existing components.
4. Focused codec/API/frontend regressions and both production builds pass; no real cache mutations are performed for verification. Visual QA remains unverified under the user's standing browser-check waiver.

## Non-goals
No global shell redesign, fake stored descriptions, independent noted toggle, gameplay database edits, new frontend framework/dependencies, publishing or full model viewer redesign. Description is absent from this cache format and stays explicitly unavailable. Browser checks remain waived.

## Capabilities
### New Capabilities
- `item-forge-editing`: faithful item editing and exact duplication with retained cache semantics.
### Modified Capabilities
None.

## Impact
Existing item codec, cache write boundary, item API, admin Item Forge and tests. Reuse confirmation/dirty guards, mutation state, images, renderer, Material icons and Semplicita typography.
