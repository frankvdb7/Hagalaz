# Quests and journal — revision 742

**Repository assessment:** Definitions and quest skeleton present; behavior coverage unknown. Code entry points do not imply full behavior coverage.

## Historical scope

Quest prerequisites, dialogue, stage progression, persistence, rewards, journal/Noticeboard display and world changes.

## Existing Hagalaz entry points

- `Hagalaz.Services.GameWorld/Model/Creatures/Characters/Quests.cs`
- `Hagalaz.Game.Scripts/Model/Quests/QuestScriptBase.cs`
- `Hagalaz.Services.GameWorld/Data/QuestRepository.cs`
- `Hagalaz.Game.Scripts/Widgets/Tabs/QuestTab.rsi.cs`
- `Hagalaz.Game.Scripts/Widgets/NoticeBoard/NoticeBoardQuest.rsi.cs`

## How to implement

1. `Quests` exposes status lookups for its in-memory dictionary, but that alone does not establish a working stage-transition pipeline. Trace initialization, registration, storage, and all quest script implementations before extending.
2. Implement one 742-era quest end-to-end: prerequisites, start, stage, dialogue, item consumption, reward, journal update and save/restore. A script should own quest-specific stages; shared quest storage should not hardcode dialogue.

## Verification criteria

- Replaying dialogue cannot repeat XP/items; stage survives relog; quests locked by prerequisites remain unavailable; Noticeboard does not claim unavailable quests are started.
- Resolve exact 742 interface IDs, timings, data and animations with client/cache fixtures and focused server tests.

## Evidence

- [Historical reference](https://wiki.darkan.org/Update%3ADaily_Challenges_%26_Noticeboard)
- Follow [implementation conventions](implementation-conventions.md) before adding state, handlers or persistence.
