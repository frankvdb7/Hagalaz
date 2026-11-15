## Context
Items have a page-owned breadcrumb picker. Generic detail pages and Sprite Studio already own their own definition stores. TypeEndpoints provides name search only for Items and NPCs; sprite search filters numeric IDs.

## Decisions
Rename and generalize the existing picker. Its small store contract includes existing selected-definition signals and optional name-search operations. A kind/name map supplies singular labels. Only Items and NPCs enable name autocomplete; all types accept exact IDs.

A directive on each page-owned breadcrumb template registers it with the existing shell-scoped template slot and removes it on destruction. This replaces the item-specific registration effect and avoids duplicating it in three page variants. The toolbar renders templates without owning definition requests.

No new HTTP services, stores or retry ownership are added. Existing HTTP resources handle search failure and cancellation. Picker close cancels the debounce and restores focus. The existing generic and sprite editors remain unchanged.

## Alternatives
Separate pickers would duplicate interaction rules. New search endpoints or archive-wide client searches would exceed this UI change. Both are rejected.

## Verification
Test all kind labels/routes, name-search capability boundaries, ID zero and template cleanup. Keep existing item mapping and sprite editor tests. Run the admin validation commands and inspect an NPC and an ID-only type at desktop/mobile widths.
