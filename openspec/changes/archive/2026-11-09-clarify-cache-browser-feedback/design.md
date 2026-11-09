# Design

The current NgRx stores retain loading, errors and results. The list template selects one content state, keeping the existing header and paginator. Loading takes precedence so retained resource values cannot appear to belong to the new route. Retry dispatches through the existing loadPage operation, or reloads archive sizes when that request failed; no additional retry owner or status store.

Search capabilities derive directly from supported type semantics: item/NPC names, exact IDs elsewhere, optional blank-query ranges except ID-only types. Legacy sprite search remains an ID-fragment API and receives accurate wording.

Overview adds query parameters to the sprite quick action. Global search adds a sprite destination to the existing destination list. No sidebar reorganization or duplicated navigation component.

Rejected: clearing every store on navigation (unnecessary data ownership changes), a second request state machine, generic capability framework, redirecting/removing legacy tools.
