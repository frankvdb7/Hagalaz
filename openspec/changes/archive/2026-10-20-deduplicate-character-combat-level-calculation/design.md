# Design

## Context

See `proposal.md` for the motivation and acceptance criteria. The duplicated getters calculate a fractional combat level, then cast at different points because only the full level adds summoning.

## Goals / Non-Goals

**Goals:**

- Share the common melee, ranged, and magic calculation without changing its floating-point or integer-conversion behavior.
- Keep both public property contracts directly testable.

**Non-Goals:**

- Change combat inputs, experience thresholds, skill state, or appearance refresh behavior.
- Move the calculation outside `CharacterStatistics` or add another owner.

## Decisions

- Extract the common calculation into a private instance method returning `double`, before final truncation. `BaseCombatLevel` casts its result to `int`; `FullCombatLevel` adds `LevelForExperience(Summoning) / 8.0` and then casts, matching the existing order of operations.
  - Returning an `int` from the helper would truncate before summoning is added and could change full combat levels. Keeping both formulas duplicated retains the identified divergence risk.
  - A private method is sufficient; an interface, utility class, or callback would add no useful boundary or variant.
- Use the existing `CharacterStatisticsTests` class to construct the public implementation, assert the base value, and set summoning experience to a level that demonstrates the full-level adjustment.
  - This tests the externally visible properties without introducing a test-only production seam.

## Risks / Trade-offs

- An integer helper could alter rounding near combat-level boundaries → return the fractional calculation and keep each property's cast and summoning addition in its current order.

## Migration Plan

No persisted data or API signatures change. Rollback consists of restoring the two getter bodies from the proposal's starting point.
