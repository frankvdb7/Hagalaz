# Tasks

## 1. Equipment Transfer Participation

- [x] 1.1 Update `IEquipmentContainer` documentation and `EquipmentContainer.TryMoveTo()` to own a transaction only when standalone, join the same current-thread transaction when both boundaries are enlisted, and reject partial/conflicting participation; verify with focused standalone, joined-commit, rollback, and rejection regression tests.
- [x] 1.2 Document `EquipItem()` and `UnEquipItem()` as standalone gameplay commands on the public Equipment contract; verify the XML remarks accurately describe their current ownership behavior.

## 2. Specification and Validation

- [x] 2.1 Update the canonical item-container storage specification from the change delta and run `openspec validate --all --strict`.
- [x] 2.2 Run the focused GameWorld Equipment tests and `git diff --check`.
