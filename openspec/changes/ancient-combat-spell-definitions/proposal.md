# Proposal

## Why

The Ice, Blood, Smoke, Shadow and Miasmic Ancient spell families each keep private static arrays of rune types and counts, required levels, autocast config ids, experience and base damage, while `skills_magic_combat_definitions` already persists exactly those fields for the standard book. `AncientMiasmicSpellData` is an unused third copy. Parallel sources of truth can diverge. See GitHub issue #376 (child of #373).

## What Changes

- Add a `spell_book` discriminator to `skills_magic_combat_definitions` and make the key `(button_id, spell_book)`. Ancient book buttons 20-39 collide with standard-book buttons (25, 28, 30, 32, 34, 36, 39), exactly like the teleport table, which already keys on `(button_id, spell_book)`. Existing rows become `StandartBook`.
- Seed the 20 Ancient combat rows (current values, exactly) through an idempotent game-world seeder. The EF migration is schema-only.
- `CombatSpellDto` gains `SpellBook`; the standard-book loop in `SpellBookTab` only wires standard rows, and the Ancient spells are built from their persisted rows by button id.
- `AncientCombatSpell` takes a `CombatSpellDto` (like `StandardCombatSpell`); the `AncientSpellData` record, the per-family arrays and `AncientMiasmicSpellData` are removed.
- The Ancient cast handlers resolve a spell from its button id instead of hard-coded `if` chains. This also fixes button 31 (smoke barrage), which was never castable because its check compared against 21.
- Non-goals: animation, graphic and projectile ids, poison damage, slow durations, staff requirement and all combat behavior stay in C# (the persisted visual columns are seeded `-1`); no rebalancing; no generic property bag.

## Capabilities

### New Capabilities
- `ancient-combat-spell-definitions`: Ancient combat spell requirements, experience, damage and autocast config are persisted per spellbook and consumed by scripts through the existing magic service.

### Modified Capabilities

## Impact

`Hagalaz.Data` (entity, DbContext, migration), `Hagalaz.Database.Migrations` (seeder), `Hagalaz.Game.Abstractions` (DTO), `Hagalaz.Services.GameWorld` (profile), `Hagalaz.Game.Scripts` (Ancient spells, `SpellBookTab`) and matching tests.
