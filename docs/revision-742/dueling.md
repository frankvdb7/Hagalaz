# Dueling and stakes — revision 742

**Repository assessment:** Partial; arena, rules and stake transfer exist. This is an implementation guide, not a claim of complete feature parity.

## Revision-specific behavior

Challenge, friendly/staked matches, equipment and combat restrictions, two-stage confirmations, countdown, winner/forfeit settlement and loser recovery.

## Existing Hagalaz entry points

- `Hagalaz.Game.Scripts/Minigames/DuelArena/DuelArenaScript.cs`
- `Hagalaz.Game.Scripts/Minigames/DuelArena/DuelRules.cs`
- `Hagalaz.Game.Scripts/Minigames/DuelArena/DuelStakeExchange.cs`
- `Hagalaz.Game.Scripts/Minigames/DuelArena/DuelArenaCombatScript.cs`
- `Hagalaz.Game.Scripts.Tests/Minigames/DuelArena/DuelStakeExchangeTests.cs`

## Build approach

1. Keep duel rules and one active duel owner in the existing minigame. Review stakes with the same item/coin invariants as direct trading; apply rules before countdown and on combat actions, not just on UI input.
2. Use 742 rule options, animations, arena movement and interface controls from fixtures. Keep stake ownership separate from normal drops and from ordinary PvP death.

## Acceptance and tests

- Rule changes invalidate confirmations; restricted styles and consumables cannot be used; simultaneous disconnect/forfeit settles at most once; stakes are conserved after a full inventory or cancelled match.
- Add focused tests near the cited code; check wire UI behavior with a revision 742 client/cache fixture before treating IDs or timings as exact.

## Evidence and open questions

- [Historical feature reference](https://wiki.darkan.org/Duel_Arena)
- Read [implementation conventions](implementation-conventions.md) before modifying runtime state or persistence.
