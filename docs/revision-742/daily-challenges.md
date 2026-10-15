# Daily challenges and Noticeboard — revision 742

**Repository assessment:** Noticeboard widgets present; no challenge progression system identified. Code entry points do not imply full behavior coverage.

## Historical scope

Daily level-appropriate tasks, completion requirements, reward claim and Noticeboard aggregation existed by September 2012.

## Existing Hagalaz entry points

- `Hagalaz.Game.Scripts/Widgets/Tabs/NoticeBoardTab.rsi.cs`
- `Hagalaz.Game.Scripts/Widgets/NoticeBoard/NoticeBoardQuest.rsi.cs`
- `Hagalaz.Game.Common/Events/Character/SkillLevelUpEvent.cs`

## How to implement

1. First identify UI layouts and daily reset behavior for 742. Add challenge definitions and one per-character persisted assignment/progress owner, preferably within current character persistence model.
2. Hook actual gameplay completion events at the existing skill/kill/crafting owners; count final production steps rather than every acquired input. Avoid a separate global event bus when existing game events suffice.

## Verification criteria

- New day assigns eligible challenge once; claimed reward cannot be reclaimed; server restart cannot multiply rewards; progress increments only on qualifying actions, not purchases.
- Resolve exact 742 interface IDs, timings, data and animations with client/cache fixtures and focused server tests.

## Evidence

- [Historical reference](https://wiki.darkan.org/Update%3ADaily_Challenges_%26_Noticeboard)
- Follow [implementation conventions](implementation-conventions.md) before adding state, handlers or persistence.
