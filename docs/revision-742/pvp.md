# Wilderness and player combat — revision 742

**Repository assessment:** Character combat and area scripts present; rules require audit. Code entry points do not imply full behavior coverage.

## Historical scope

Risk levels, skulls, wilderness eligibility, safe zones, single/multicombat, teleport limits, dangerous death and level-dependent opponent matching.

## Existing Hagalaz entry points

- `Hagalaz.Services.GameWorld/Model/Creatures/Characters/CharacterCombat.cs`
- `Hagalaz.Game.Scripts/Areas/Wilderness/`
- `Hagalaz.Game.Scripts/Widgets/EquipmentTab/ItemsKeptOnDeathWindow.rsi.cs`

## How to implement

1. Keep target validity and skull updates in the existing character combat and area owners. Read the target's live position, world/height and area state at execution, not a stale click snapshot.
2. Use exact revision-742 single/multi and attack-level calculations; keep Wilderness death/drop rules distinct from Duel Arena or other safe instances.

## Verification criteria

- Safe zone checks apply at attack and hit time; restricted teleport requests fail; kill attribution/drop ownership is stable; crossing zone boundaries does not allow repeated or impossible hits.
- Resolve exact 742 interface IDs, timings, data and animations with client/cache fixtures and focused server tests.

## Evidence

- Historical behavior and resource IDs require direct 742 fixture verification.
- Follow [implementation conventions](implementation-conventions.md) before adding state, handlers or persistence.

## P1 PvP and character-death verification

[Combat and character mechanics](reference/deep-combat-character-mechanics.md#7-death-skull-item-conservation-and-pvp) follows Wilderness combat-level limits, teleport gates, familiars, skulled status and DuelArena rules. [Tests C27–C34](reference/combat-verification-matrix.md) describe boundary conditions and item-conservation cases. Runtime tests and any current DB-specific death overrides remain unverified.
