# Summoning and familiars — revision 742

**Repository assessment:** Partial; definitions and familiar persistence present. This is an implementation guide, not a claim of complete feature parity.

## Revision-specific behavior

Pouch creation, summoning points, familiar lifetime, followers, special scrolls, beast-of-burden inventory and obelisk recharge.

## Existing Hagalaz entry points

- `Hagalaz.Game.Scripts/Skills/Summoning/SummoningSkillService.cs`
- `Hagalaz.Services.GameWorld/Services/SummoningService.cs`
- `Hagalaz.Game.Scripts/Characters/FamiliarCharacterScript.cs`
- `Hagalaz.Game.Scripts/Skills/Summoning/FamiliarInventoryWidget.cs`
- `Hagalaz.Services.GameWorld/Logic/Dehydrators/FamiliarDehydrator.cs`

## Build approach

1. Use definition lookups for pouch/scroll/NPC relationships. Keep familiar lifetime and inventory under the existing familiar owner; use existing NPC following and item-container boundaries.
2. Handle summon, dismiss, logout/login, scroll casting, pet overlap, restriction zones and pouch consumption explicitly; honor tooltips and NPC IDs from 742 cache rather than guessing.

## Acceptance and tests

- One active familiar; lifetime and points cannot go negative; beast-of-burden transfers conserve items and reject full receivers; a saved familiar and carried items restore without duplication or loss.
- Add focused tests near the cited code; check wire UI behavior with a revision 742 client/cache fixture before treating IDs or timings as exact.

## Evidence and open questions

- [Historical feature reference](https://wiki.darkan.org/Summoning_obelisk)
- Read [implementation conventions](implementation-conventions.md) before modifying runtime state or persistence.
