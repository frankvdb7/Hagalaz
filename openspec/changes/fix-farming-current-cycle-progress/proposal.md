# Fix farming current-cycle progress hydration

## Goal

Keep farming `CurrentCycleTicks` as non-negative progress within the persisted current cycle, safely normalize oversized legacy values, and apply offline growth without signed arithmetic overflow.

## In scope

- Correct `FarmingPatchTickTask.Hydrate()` to combine saved cycle progress with elapsed offline ticks.
- Normalize oversized legacy progress without replaying cycles already represented by persisted `CurrentCycle` and condition state.
- Use wide arithmetic for offline elapsed ticks and stop catch-up when the farming state cannot advance further.
- Reject database `uint` values that cannot be represented by the signed runtime DTO instead of silently wrapping.
- Preserve existing growth, cycle-completion, product, condition, and identity behavior.
- Add focused GameWorld and Characters persistence regressions.

## Non-goals

- Database schema or contract type changes.
- World-entry, session/admission, Contacts, Authorization, Notes, or client changes.
- Changes to checked persistence conversions or unrelated farming behavior.

## Acceptance criteria

- Hydration leaves live `TickCount` nonnegative and, for active cycling patches, below the current cycle length.
- Saved progress is retained with no offline ticks; partial elapsed time advances progress accurately.
- Exact and multiple cycle boundaries use the existing growth transitions and preserve the correct remainder.
- Oversized legacy progress is normalized without replaying persisted growth stages; mature and dead patches retain their terminal state with zero tick progress.
- Offline arithmetic does not overflow for realistic multi-year intervals and catch-up stops at terminal or non-progressing states.
- Unsigned database progress above `Int32.MaxValue` is rejected rather than wrapped into a negative DTO value.
- Farming patch identity, seed identity, state, and checked persistence remain valid.

## Stop conditions

Stop if correct catch-up requires changing cycle/product semantics beyond the existing loop, or if solving this requires schema or cross-contract changes.
