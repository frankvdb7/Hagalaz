# Skill levels, XP and advancement — revision 742

**Repository assessment:** Character skill state and level-up flow present. Code entry points do not imply full behavior coverage.

## Historical scope

Combat and noncombat skill XP, level caps, experience curves, boosted/drained levels, milestone unlocks and combat-level display.

## Existing Hagalaz entry points

- `Hagalaz.Game.Scripts/Characters/SkillsCharacterScript.cs`
- `Hagalaz.Game.Scripts/Widgets/Tabs/SkillsTab.rsi.cs`
- `Hagalaz.Game.Common/Events/Character/SkillLevelUpEvent.cs`
- `Hagalaz.Services.GameWorld/Logic/Dehydrators/StatisticsDehydrator.cs`

## How to implement

1. Use the existing skill/stat owner as authoritative XP source. Call its established XP method from skill tasks, never write level fields directly from feature scripts.
2. Confirm earlier combat-level formula and Constitution scale against 742 captures; special skills, quest XP rewards and boosts must all pass through the same level-up/refresh mechanism.

## Verification criteria

- XP never decreases unintentionally; repeated events do not grant extra XP; boosted stats expire independently of base levels; correct widgets and saved stats refresh after relog.
- Resolve exact 742 interface IDs, timings, data and animations with client/cache fixtures and focused server tests.

## Evidence

- [Historical reference](https://wiki.darkan.org/Summoning_boost)
- Follow [implementation conventions](implementation-conventions.md) before adding state, handlers or persistence.
