# Design

## Context

`MagicService.FindAllCombatSpells` already projects `SkillsMagicCombatDefinition` to `CombatSpellDto`, and `SpellBookTab` already loads it once when the widget initialises. Only standard spells use it; the Ancient families ignore it.

## Decisions

- **Spellbook discriminator, not new table.** `spell_book` enum column (same values as the teleport table, default `StandartBook`) plus composite primary key. A dev database holds only standard rows, whose button ids overlap the Ancient ones, so a button-only key cannot hold both. The MySQL provider ignores `DropPrimaryKey`/`AddPrimaryKey`, so the migration adds the column through EF and changes the key with one DDL statement (as `UpdateOpenIddict7` already does for provider gaps).
- **One query.** `SpellBookTab` splits the single `FindAllCombatSpells` result by `SpellBook`; no second blocking query.
- **Spell type stays in code.** The Ancient book lays each family out as rush, blitz, burst, barrage on four consecutive buttons (type 0, 2, 1, 3). Each family maps its first button id to a base; `SpellType` (multi-hit, effect selection, slow/poison scaling) stays a code constructor argument next to the DTO.
- **Visual ids stay in code.** Animations, graphics and projectiles vary per type and per effect (some spells render none, some render on the caster). Persisting them would diverge from what the code renders, so rows carry the `-1` defaults and the code keeps them.
- **Staff requirement stays in code** (Zuriel's staff ids). It is a single behavioral restriction, not duplicated persisted data.
- **Seeding** through an `IGameWorldSeeder`; rows are only added when the key is absent and never overwritten.

## Risks

- A missing Ancient row would leave a spell unavailable; the seeder tests assert all 20 rows and the migration is applied before seeding.
