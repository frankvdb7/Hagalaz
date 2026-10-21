# Proposal

## Why

`CharacterStatistics.FullCombatLevel` and `BaseCombatLevel` repeat the same melee, ranged, and magic combat calculation. Keeping the formula in both getters risks the two public values drifting as combat-level rules change.

## What Changes

- Extract the shared base combat-level calculation into one private instance method.
- Keep summoning contribution and integer conversion at their current points so public results remain unchanged.
- Add focused tests for both public combat-level properties.

## Scope and Acceptance Criteria

- Only the shared combat-level calculation and its focused `CharacterStatisticsTests` change.
- `BaseCombatLevel` continues to truncate the calculation without summoning.
- `FullCombatLevel` continues to add `LevelForExperience(Summoning) / 8.0` before truncation.
- Tests assert both public properties and distinguish the summoning contribution.
- If preserving the current intermediate rounding requires changing the formula or external behavior, stop and propose a separate change.

## Non-Goals

- Do not change experience-to-level thresholds, combat constants, skill state, or appearance refresh behavior.
- Do not add an interface, static utility, generic calculation framework, or new dependency.
- Do not refactor combat-level clones in NPC code or other jscpd findings in this change.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

None. This is an implementation-only refactor; public combat-level behavior remains the contract.

## Impact

- `Hagalaz.Services.GameWorld/Model/Creatures/Characters/CharacterStatistics.cs`
- `Hagalaz.Services.GameWorld.Tests/CharacterStatisticsTests.cs`

No API signature, dependency, or runtime registration changes are required.
