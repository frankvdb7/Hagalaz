# Tasks

## 1. Characterize the shared formula

- [x] 1.1 Add interface tests for equal/current-below levels, multiple boosted levels, magic-damage bonus, and truncation.

## 2. Consolidate the formula

- [x] 2.1 Implement `CalculateMagicMaxHit` in `CreatureCombatRules` and add it to `ICreatureCombatRules`.
- [x] 2.2 Replace the copied formulas in `CharacterCombat.GetMagicMaxHit` and `NpcCombat.GetMagicMaxHit`, preserving each statistics lookup.

## 3. Verify the scoped refactor

- [x] 3.1 Run the focused GameWorld test class and confirm all tests pass.
- [x] 3.2 Re-run the same strict exact-clone scan and confirm the magic max-hit formula is no longer duplicated.
- [x] 3.3 Review the scoped diff and validate the OpenSpec change.
