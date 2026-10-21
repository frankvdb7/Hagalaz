# Design

## Context

`CreatureCombat.Tick` invokes each concrete combat implementation's `CursesTick`. Both character and NPC implementations inspect `RecentAttackers` and translate their curse states into the five instant prayer bonuses that must remain active. Character combat uses the same state-to-bonus mapping for its own curses before applying a drain to the target.

## Goals and Non-Goals

**Goals:**

- Keep the curse-state mapping in one combat-rules owner.
- Keep aggregation over the base class's `RecentAttackers` collection in the existing `CreatureCombat` owner.
- Preserve each combat implementation's bonus effects.
- Preserve the character-only self-curse path and its timing.

**Non-Goals:**

- Do not combine the two `CursesTick` implementations.
- Do not add a separate rules service or a new value type.
- Do not change curse state checks or instant bonus behavior.

## Decisions

- Add `GetCurseInstantBonusEffects(ICreature)` to the existing `ICreatureCombatRules`. It returns a named tuple with the attack, strength, defence, ranged, and magic effects.
- Implement the existing `HasState<T>()` mapping once in `CreatureCombatRules`; cache repeated Sap state checks within the call.
- Add a protected `CreatureCombat` method that ORs the rule result across `RecentAttackers`; this collection is already owned by the base class.
- Keep stale-bonus clearing and target bonus application in their current callers.
- In character combat, use the same rules operation for the owner's self effects instead of repeating the state checks.
- Test every state category and the empty-state result through `ICreatureCombatRules`.

## Risks and Trade-offs

- A mistaken mapping would affect curse bonus cleanup or drains. Tests assert the complete five-flag tuple for each distinct state category.
- The rules method centralizes state mapping, while the base combat class centralizes only attacker aggregation; timing and side effects remain in the current concrete tick implementations.

## Migration Plan

No persisted state or protocol changes. Rollback consists of restoring the duplicated checks and removing the interface operation and its tests.
