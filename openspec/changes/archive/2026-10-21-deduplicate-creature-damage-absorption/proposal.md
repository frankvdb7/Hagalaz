# Proposal

## Why

`CharacterCombat.Attack` and `NpcCombat.Attack` contain the same damage-type-to-absorption mapping and damage-soak calculation. Keeping both copies risks their soak threshold, rounding, or sentinel behavior drifting.

## What Changes

- Add the shared damage-absorption calculation to the existing `ICreatureCombatRules` composition.
- Replace the duplicated calculation in both attack overrides while keeping their character- and NPC-specific hooks in place.
- Add direct tests for the public rules interface operation.

## Scope and Acceptance Criteria

- Only the shared absorption calculation, its two callers, focused interface tests, and this change record are affected.
- Full and standard melee, ranged, and magic damage use their matching absorption bonus.
- Other damage types remain unabsorbed.
- Damage is considered for absorption only when it is greater than 200; the soak remains truncated to `int`, is applied only when positive, and reports `-1` when no damage was soaked.
- Character defence processing, NPC script hooks, life-point damage, attacker tracking, retaliation, and attack results retain their current ordering and behavior.

## Non-Goals

- Do not merge character and NPC attack orchestration.
- Do not change damage types, bonus values, soak thresholds, rounding, or sentinels.
- Do not refactor other exact clones from the repository-wide scan.

## Capabilities

### New Capabilities

None

### Modified Capabilities

None. This is a behavior-preserving implementation refactor.

## Impact

- `Hagalaz.Services.GameWorld/Model/Creatures/ICreatureCombatRules.cs`
- `Hagalaz.Services.GameWorld/Model/Creatures/CreatureCombatRules.cs`
- `Hagalaz.Services.GameWorld/Model/Creatures/Characters/CharacterCombat.cs`
- `Hagalaz.Services.GameWorld/Model/Creatures/Npcs/NpcCombat.cs`
- `Hagalaz.Services.GameWorld.Tests/CreatureCombatRulesTests.cs`
