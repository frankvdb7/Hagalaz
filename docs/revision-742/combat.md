# Combat and combat levels — revision 742

**Repository assessment:** Partial; mechanics need fixture comparison. This is an implementation guide, not a claim of complete feature parity.

## Revision-specific behavior

Pre-20-November attack-style combat: melee, ranged and magic attacks, attack speed, special-attack energy, hitpoints, combat levels and aggression. Do not add an ability bar, dual-wield or later damage formulas.

## Existing Hagalaz entry points

- `Hagalaz.Services.GameWorld/Model/Creatures/Characters/CharacterCombat.cs`
- `Hagalaz.Game.Scripts/Widgets/Tabs/CombatTab.rsi.cs`
- `Hagalaz.Game.Abstractions/Model/Combat/AttackStyle.cs`
- `Hagalaz.Game.Configuration/CombatOptions.cs`

## Build approach

1. Trace attack initiation through reachability, line of sight, target validation, tick scheduling, hit accuracy/damage, experience and death. Establish dated reference fixtures for attack speed, stance bonuses, special attacks, equipment requirements and combat-level formula before changing constants.
2. Keep combat rules in the character/creature combat owner; put weapon-specific behavior in existing scripts/config. Do not create a second combat engine or port post-boundary ability rotations.

## Acceptance and tests

- Melee/ranged/magic attack tests cover hit/miss, cooldown, autocast, weapon swapping and blocked routes; no damage is dealt through collision walls; opposing attacks cannot exceed per-style attack cadence.
- Add focused tests near the cited code; check wire UI behavior with a revision 742 client/cache fixture before treating IDs or timings as exact.

## Evidence and open questions

- [Historical feature reference](https://wiki.darkan.org/Evolution_of_Combat)
- Read [implementation conventions](implementation-conventions.md) before modifying runtime state or persistence.
