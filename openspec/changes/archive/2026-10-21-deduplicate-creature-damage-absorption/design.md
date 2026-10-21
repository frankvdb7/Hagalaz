# Design

## Context

The character and NPC attack paths both calculate absorption after their distinct pre-processing hooks: `CharacterCombat` calls `PerformDefence`, while `NpcCombat` calls the NPC script's `OnAttack`. Their following absorption blocks are exact copies.

## Goals and Non-Goals

**Goals:**

- Have one owner for damage-type absorption selection and soak arithmetic.
- Keep all character- and NPC-specific attack flow at its current owner.
- Preserve observable damage and soak results.

**Non-Goals:**

- Add another combat service or an attack coordinator.
- Change public `Attack` signatures or the order of attack hooks.

## Decisions

- Add `CalculateDamageAbsorption` to `ICreatureCombatRules` and implement it in the existing instance class. It accepts the damage type, current damage, and the three absorption bonuses, then returns the adjusted damage and soaked amount as a named tuple.
- Keep bonus lookup in each caller and pass the current bonus values to the rule. Bonus lookup is a direct value read; evaluating all three values does not change state.
- Call the rule only after the current character defence or NPC script hook. Keep life-point mutation, attacker tracking, and retaliation in their existing classes.
- Test damage-type selection, the strict `> 200` threshold, integer truncation, positive-only soak, and the `-1` no-soak result through the interface.

## Risks and Trade-offs

- Moving the threshold or bonus selection before the existing character/NPC hooks could change behavior. The extraction occurs after those hooks and leaves their order untouched.

## Migration Plan

No persisted data or public attack signatures change. Rollback consists of restoring each original absorption block and removing the interface method and its tests.
