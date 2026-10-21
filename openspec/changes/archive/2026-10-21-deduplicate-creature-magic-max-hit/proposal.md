# Proposal

## Why

`CharacterCombat.GetMagicMaxHit` and `NpcCombat.GetMagicMaxHit` contain the same boost, magic-damage bonus, and truncation formula. Keeping both copies risks combat damage drifting between characters and NPCs.

## What Changes

- Add the shared formula to the existing `ICreatureCombatRules` composition.
- Replace the duplicated formula while leaving character- and NPC-specific level lookups in their current owners.
- Add direct tests for the public rules-interface operation.

## Scope and Acceptance Criteria

- Only the shared formula, its two callers, focused rules-interface tests, and this change record are affected.
- Each magic level above the supplied base level multiplies base damage by 1.03.
- The supplied magic-damage bonus is applied after level boosts, and the final damage is truncated to `int`.
- Current/base level and bonus retrieval remain in their existing character and NPC implementations.

## Non-Goals

- Do not unify character and NPC statistics access or combat orchestration.
- Do not change magic damage accuracy, spell base damage, or the order of calculation steps.
- Do not refactor other clone candidates in this change.

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
