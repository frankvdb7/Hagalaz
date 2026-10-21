# Design

## Context

Both character and NPC magic attacks calculate maximum spell damage by applying a 3% multiplier for each current magic level above the unboosted level, then applying magic-damage bonus and truncating. The callers obtain those levels through different statistics implementations.

## Goals and Non-Goals

**Goals:**

- Have one owner for the shared magic max-hit formula.
- Preserve the character and NPC level lookup behavior.
- Preserve observable max-hit results.

**Non-Goals:**

- Add a new rules service or static helper.
- Merge character and NPC combat APIs.
- Change magic attack accuracy or spell-specific damage inputs.

## Decisions

- Add `CalculateMagicMaxHit` to the existing `ICreatureCombatRules` and implement it in the existing instance class.
- Pass base damage, current magic level, base magic level, and magic-damage bonus to the calculation. Keep statistics lookups in each caller.
- Test equal levels, multiple boosted levels, bonus ordering, and integer truncation through the interface.

## Risks and Trade-offs

- Changing the order of level boosts, bonus application, or truncation would change gameplay damage. The shared method preserves the existing order exactly.
- Character and NPC base-level sources differ; keeping those lookups in their callers avoids changing that behavior.

## Migration Plan

No persisted data or public combat signatures change. Rollback consists of restoring both original formula blocks and removing the rules-interface method and its tests.
