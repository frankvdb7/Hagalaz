# Proposal

## Why

`CharacterCombat.CursesTick` and `NpcCombat.CursesTick` duplicate the mapping from attacker curse states to the instant attack, strength, defence, ranged, and magic bonuses they affect. Character combat repeats the same mapping for its own active curses. These copies can drift independently.

## What Changes

- Put the shared state-to-bonus mapping in the existing `ICreatureCombatRules` composition and aggregate recent attackers in their existing `CreatureCombat` owner.
- Use the shared result in character self checks and both character/NPC attacker checks.
- Add direct interface tests for each curse-state mapping and the no-curse case.

## Scope and Acceptance Criteria

- `SapWarriorState` affects attack, strength, and defence; `SapRangerState` affects defence and ranged; `SapMagerState` affects defence and magic.
- Each leech state affects only its corresponding bonus: attack, strength, defence, ranged, or magic.
- Character and NPC combat continue aggregating active attacker curses and clearing stale instant bonuses at their current tick point.
- Aggregation across recent attackers remains an OR of each attacker's active curse effects.
- Character self-curse effects continue to be applied to the target at the existing timing and value.
- No combat tick ordering, state lifetime, or bonus mutation behavior changes.

## Non-Goals

- Do not merge character and NPC curse tick orchestration.
- Do not change which states apply, how bonuses are cleared or applied, or when those operations run.
- Do not add a new service, DTO, or general state-mapping framework.

## Capabilities

### New Capabilities

None

### Modified Capabilities

None. This is a behavior-preserving implementation refactor.

## Impact

- `Hagalaz.Services.GameWorld/Model/Creatures/ICreatureCombatRules.cs`
- `Hagalaz.Services.GameWorld/Model/Creatures/CreatureCombatRules.cs`
- `Hagalaz.Services.GameWorld/Model/Creatures/CreatureCombat.cs`
- `Hagalaz.Services.GameWorld/Model/Creatures/Characters/CharacterCombat.cs`
- `Hagalaz.Services.GameWorld/Model/Creatures/Npcs/NpcCombat.cs`
- `Hagalaz.Services.GameWorld.Tests/CreatureCombatRulesTests.cs`
