# Runespan activity — revision 742

**Repository assessment:** No dedicated activity implementation found. Existing files are entry points, not evidence of feature completeness.

## Historical scope

2012-era runic siphoning realm with nodes/creatures, temporary rune economy, movement platforms, runesphere and rewards; distinct from regular altar crafting.

## Hagalaz entry points

- `Hagalaz.Game.Scripts/Skills/Runecrafting/Runecrafting.cs`
- `Hagalaz.Services.GameWorld/Services/RunecraftingService.cs`
- `Hagalaz.Services.GameWorld/Model/Maps/Regions/MapRegion.cs`

## Implementation path

1. Start with cache/region, NPC, node and widget identification for 742. Reuse interaction scripts, object/NPC spawns, item containers and character task scheduling; do not force temporary runes into the ordinary persistent bank.
2. Define a separate, session-scoped temporary rune balance with explicit exchange-on-exit rules only after fixture proof. Implement one node and one traversal platform end-to-end before additional node families.

## Regression acceptance

- Entry/exit cannot export temporary runes; depleted node transitions are shared; disconnect recovers cleanly; skill XP/points persist once; later-era nodes and interfaces are excluded.
- Verify recipe IDs, rates, object interactions and interface details against revision 742 fixtures; extend focused project tests instead of copying later behavior.

## Reference and uncertainty

- [Period reference](https://wiki.darkan.org/Runespan)
- Apply [implementation conventions](implementation-conventions.md) for ownership, testability and durable state.
