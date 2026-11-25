# Proposal

## Why

Herb definitions are persisted, but `HerbloreService` builds the potion catalogue as a static `PotionDto[]`, and the overload recipe (level 96, five extreme potions + torstol, product, 1000 XP) and the extreme-potion ingredients (avantoe, dwarf weed, lantadyme, ground mud runes, five grenwall spikes) live in `HerbloreSkillService`/`HerbloreConstants`. Lookups block on `.Result`. See GitHub issue #375 (child of #373).

## What Changes

- New `skills_herblore_potion_definitions` table (schema-only migration) with a `kind` discriminator (standard, overload) and a per-recipe secondary ingredient count.
- Idempotent seeder: the 30 current recipes verbatim, the extreme-potion ingredients as their secondary ingredients (grenwall spikes ×5), and overload as a recipe row.
- `HerbloreService` becomes a startup-preloaded singleton: synchronous potion lookups (by primary ingredient; by base item + secondary ingredient per kind; all of a kind). Herb queries keep using the repository.
- Scripts and item script factories use `IHerbloreService`; `IHerbloreSkillService.GetPotionBy*`, `.Result`, and the recipe constants are removed. `MakeOverload` takes the overload definition.
- Non-goals: dialogue/task flow, messages, tick delays, potion effects and dose ids stay in code; no value changes.

## Capabilities

### New Capabilities
- `herblore-potion-definitions`: persisted Herblore potion recipes consumed by scripts through `IHerbloreService`.

### Modified Capabilities

## Impact

`Hagalaz.Data`, `Hagalaz.Database.Migrations` (seeder), `Hagalaz.Game.Abstractions`, `Hagalaz.Services.GameWorld`, `Hagalaz.Game.Scripts`, docs, and matching tests.
