# Minigames and boss encounters — revision 742

**Repository assessment:** Several named minigame and boss script sets exist. Code entry points do not imply full behavior coverage.

## Historical scope

Period-appropriate Barrows, God Wars, Fight Caves, Soul Wars, Crucible, Duel Arena and related encounters; each has distinct entry, success and exit rules.

## Existing Hagalaz entry points

- `Hagalaz.Game.Scripts/Minigames/Barrows/BarrowsScript.rschar.cs`
- `Hagalaz.Game.Scripts/Minigames/Godwars/GodwarsScript.cs`
- `Hagalaz.Game.Scripts/Minigames/Crucible/Crucible.cs`
- `Hagalaz.Game.Scripts/Minigames/DuelArena/DuelArenaScript.cs`

## How to implement

1. Avoid one generic minigame framework. For each existing minigame, document prerequisites, per-player/party state, allowed equipment, safe/dangerous death, exit and reward once.
2. Use existing area/character/NPC/object scripts; add focused integration tests for stage transitions and interrupts. Ensure revision-742 boss phases and drop tables are fixture-backed.

## Verification criteria

- Players cannot carry instanced objects across areas; finishing or abandoning does not duplicate loot; deaths use encounter-specific policy; reconnect restores safe location and state.
- Resolve exact 742 interface IDs, timings, data and animations with client/cache fixtures and focused server tests.

## Evidence

- Historical behavior and resource IDs require direct 742 fixture verification.
- Follow [implementation conventions](implementation-conventions.md) before adding state, handlers or persistence.
