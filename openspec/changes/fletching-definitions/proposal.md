# Proposal

## Why

Fletching recipes (wood, bows/crossbows, ammunition, bolt tips) are static `FletchingDefinition[]` arrays in `FletchingSkillService`, although `skills_fletching_definitions` / `SkillsFletchingDefinition` already exists and is unused. Content tuning needs code changes. See GitHub issue #374 (child of #373).

## What Changes

- Reuse `SkillsFletchingDefinition`; add a `category` column (wood, bow, ammo, tips) and make `animation_id` nullable (no animation).
- Seed all 78 current recipes through an idempotent game-world seeder, values verbatim.
- Add `IFletchingService` (DTO + category enum), repository, startup-preloaded in-memory service, mapping and DI in GameWorld.
- `FletchingSkillService`, `FletchingTask` and the item script factories consume the service; the static arrays and `FletchingDefinition` are removed.
- Non-goals: task flow, inventory mutation, messages, dialogues and per-category tick delays stay in code; no value changes (including suspicious source values).

## Capabilities

### New Capabilities
- `fletching-definitions`: persisted Fletching recipes consumed by scripts through a service.

### Modified Capabilities

## Impact

`Hagalaz.Data`, `Hagalaz.Database.Migrations` (seeder), `Hagalaz.Game.Abstractions`, `Hagalaz.Services.GameWorld`, `Hagalaz.Game.Scripts`, and matching tests.
