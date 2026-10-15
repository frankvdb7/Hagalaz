# Prayer and curses — revision 742

**Repository assessment:** Partial; active and quick-prayer state exists. This is an implementation guide, not a claim of complete feature parity.

## Revision-specific behavior

Earlier prayer and curse books, activation restrictions, mutually exclusive effects, drain, restoration, quick-prayer selection and combat modifiers.

## Existing Hagalaz entry points

- `Hagalaz.Services.GameWorld/Model/Creatures/Characters/Prayers.cs`
- `Hagalaz.Game.Abstractions/Model/Creatures/Characters/IQuickPrayer.cs`
- `Hagalaz.Services.GameWorld/Services/PrayerService.cs`
- `Hagalaz.Game.Scripts/Widgets/Tabs/PrayerTab.rsi.cs`

## Build approach

1. Use the existing prayer state as single owner of active prayers and drain ticks. Derive eligibility and exclusivity from definition data; update prayer book interfaces/configs through existing widget scripts.
2. Wire prayer effects to existing damage/accuracy/defence paths only once; do not introduce a parallel buff registry. Check quest-gated book switching and 742-config IDs from the cache.

## Acceptance and tests

- Exclusive prayers cannot coexist; draining to zero deactivates effects; restore cannot overfill; quick-prayer preserves selected choices through login; PVP protection modifiers are verified with 742 fixtures.
- Add focused tests near the cited code; check wire UI behavior with a revision 742 client/cache fixture before treating IDs or timings as exact.

## Evidence and open questions

- Exact historical values and component mappings remain fixture-gated; do not replace unknowns with later-client defaults.
- Read [implementation conventions](implementation-conventions.md) before modifying runtime state or persistence.
