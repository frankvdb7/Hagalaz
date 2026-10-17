# Death, drops and gravestones — revision 742

**Repository assessment:** Combat death and ground items present; gravestone coverage unverified. Code entry points do not imply full behavior coverage.

## Historical scope

Safe/minigame vs dangerous deaths, protected items, dropped items, grave lifetime, reclaim, equipment restrictions and respawn.

## Existing Hagalaz entry points

- `Hagalaz.Services.GameWorld/Model/Creatures/Characters/CharacterCombat.cs`
- `Hagalaz.Services.GameWorld/Services/GroundItemService.cs`
- `Hagalaz.Game.Scripts/Widgets/EquipmentTab/ItemsKeptOnDeathWindow.rsi.cs`
- `Hagalaz.Services.GameWorld/Logic/Dehydrators/ItemCollectionDehydrator.cs`

## How to implement

1. Decide safe/unsafe outcome from combat area and active minigame, then calculate kept/lost slots before any removal. Use ground-item owner and current respawn logic; do not add a parallel loot store to replace map items.
2. Map reclaim duration, visibility, grave exceptions and risk-preview icons to 742 evidence. Explicitly distinguish trade/duel escrow and safe coin pouch from losable carried items.

## Verification criteria

- The same death event cannot drop twice; respawn and reconnect never duplicate or erase protected items; grave expiry is deterministic; non-lootable deaths do not leak items to other players.
- Resolve exact 742 interface IDs, timings, data and animations with client/cache fixtures and focused server tests.

## Evidence

- [Historical reference](https://wiki.darkan.org/Items_Kept_on_Death)
- Follow [implementation conventions](implementation-conventions.md) before adding state, handlers or persistence.
