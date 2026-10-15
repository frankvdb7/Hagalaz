# Agility and shortcuts — revision 742

**Repository assessment:** Course obstacles and Gnome route scripts present. Existing files are entry points, not evidence of feature completeness.

## Historical scope

Obstacle sequences, movement/animation, failure damage, lap rewards, run-energy benefits and individual shortcuts.

## Hagalaz entry points

- `Hagalaz.Game.Scripts/Skills/Agility/Agility.cs`
- `Hagalaz.Game.Scripts/Skills/Agility/Courses/Gnome/LogBalance.rsobj.cs`
- `Hagalaz.Game.Scripts/Skills/Agility/Courses/Gnome/Signpost.rsobj.cs`
- `Hagalaz.Game.Scripts.Tests/Skills/Agility/Courses/Gnome/SignpostTests.cs`

## Implementation path

1. Have each obstacle own its exact route/animation/XP rule via object scripts and map movement tasks. Track current lap stage in character state only if needed for reward correctness.
2. Reuse pathfinding and movement permissions; guard shortcuts by level, area and prior interaction. Do not build a generic obstacle graph for a single course.

## Regression acceptance

- Skipped/repeated obstacles cannot farm lap XP; blocked arrival or movement interruption restores control; shortcut does not bypass collisions outside allowed transition.
- Verify recipe IDs, rates, object interactions and interface details against revision 742 fixtures; extend focused project tests instead of copying later behavior.

## Reference and uncertainty

- Exact level tables, success chances and asset/config IDs remain unverified until reference fixtures are captured.
- Apply [implementation conventions](implementation-conventions.md) for ownership, testability and durable state.
