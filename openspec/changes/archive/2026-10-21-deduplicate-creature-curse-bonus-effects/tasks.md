# Tasks

## 1. Characterize the shared state mapping

- [x] 1.1 Add interface tests for SapWarrior, SapRanger, SapMager, each leech state, and no active curse states.

## 2. Consolidate the mapping

- [x] 2.1 Implement `GetCurseInstantBonusEffects` in `CreatureCombatRules` and expose it through `ICreatureCombatRules`.
- [x] 2.2 Aggregate recent attacker effects in the existing `CreatureCombat` base and use that result in both tick implementations; use the combat rule for character self-state checks, preserving side effects and timing.

## 3. Verify the scoped refactor

- [x] 3.1 Run the focused GameWorld combat-rules tests.
- [x] 3.2 Re-run the strict exact-clone scan and confirm the duplicated state-to-bonus mapping is gone.
- [x] 3.3 Review the scoped diff and cumulative PR context, validate the OpenSpec change, and leave unrelated clone candidates for separate follow-ups.
