# Tasks

## 1. Characterize shared absorption behavior

- [x] 1.1 Add interface tests for melee, ranged, magic, unsupported damage types, the 200/201 boundary, truncation, and the no-soak sentinel.

## 2. Consolidate the duplicate calculation

- [x] 2.1 Implement `CalculateDamageAbsorption` in `CreatureCombatRules` and add it to `ICreatureCombatRules`.
- [x] 2.2 Replace the copied absorption blocks in `CharacterCombat.Attack` and `NpcCombat.Attack`, preserving surrounding call order.

## 3. Verify the scoped refactor

- [x] 3.1 Run the focused GameWorld test class and confirm all tests pass.
- [x] 3.2 Re-run the same strict exact-clone scan on GameWorld and confirm the absorption calculation is no longer duplicated. Review the remaining repeated call-site glue and leave it inline because a forwarding helper would add indirection without owning a rule.
- [x] 3.3 Review the scoped diff and cumulative PR context, validate the OpenSpec change, and leave unrelated clone candidates for separate follow-ups.
