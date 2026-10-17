# Travel, home teleport and lodestones — revision 742

**Repository assessment:** Lodestone service/widget and teleport tasks present. Code entry points do not imply full behavior coverage.

## Historical scope

Activate a destination once, store unlock state, use the home-teleport selection screen, and enforce teleport animation, cooldown, spellbook and restriction semantics as of 742.

## Existing Hagalaz entry points

- `Hagalaz.Services.GameWorld/Services/LodestoneService.cs`
- `Hagalaz.Game.Scripts/Widgets/Lodestone/LodestoneInterface.rsi.cs`
- `Hagalaz.Game.Scripts/Skills/Magic/TeleportSpells/HomeTeleportTask.cs`
- `Hagalaz.Game.Abstractions/Features/States/Effects/LodestoneActivatedState.cs`

## How to implement

1. Resolve destinations from `GameobjectLodestone` definitions and existing activation states; persist unlocks via the existing state hydrator/dehydrator. The current widget synchronously waits for `FindAll()` in `OnOpen` and needs careful runtime review, not a new data access layer.
2. Verify 742 destination set, unlock interactions, arrival coordinates and UI components from cache/reference captures. Reuse teleport tasks and map movement; do not create another destination registry.

## Verification criteria

- Unactivated destination cannot be used; teleport prevented by combat/region restrictions; unlock survives relog; callbacks cannot teleport twice; animation is interruptible and arrival is valid.
- Resolve exact 742 interface IDs, timings, data and animations with client/cache fixtures and focused server tests.

## Evidence

- [Historical reference](https://wiki.darkan.org/Lodestone_Network)
- Follow [implementation conventions](implementation-conventions.md) before adding state, handlers or persistence.
