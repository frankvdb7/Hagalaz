# Design

## Context

`SpellBookTab` used to load `SkillsMagicCombatDefinition` rows (projected to `CombatSpellDto`) through a blocking EF query every time the widget opened, and only standard spells used them; the Ancient families ignored the table.

## Decisions

- **Spellbook discriminator, not new table.** `spell_book` enum column (same values as the teleport table, default `StandartBook`) plus composite primary key. A dev database holds only standard rows, whose button ids overlap the Ancient ones, so a button-only key cannot hold both. The MySQL provider ignores `DropPrimaryKey`/`AddPrimaryKey`, so the migration adds the column through EF and changes the key with one DDL statement (as `UpdateOpenIddict7` already does for provider gaps).
- **Startup-loaded catalog, no I/O on widget open.** `ICombatSpellCatalog` is a singleton `IStartupService` (the Agility/Fletching pattern) that projects all combat rows once at GameWorld startup into memory. `SpellBookTab` reads it synchronously for both books and splits it by `SpellBook`, so the synchronous widget lifecycle never waits on the database; `FindAllCombatSpells` is removed. Readiness depends on the database migration and seeders having completed before GameWorld starts (the AppHost already uses `WaitForCompletion` on the migrations project); a row missing at startup leaves that spell unavailable until the next restart. Operator edits to any combat spell definition, standard or Ancient, now take effect after a GameWorld restart instead of the next widget open. No reload mechanism is added.
- **Spell type stays in code.** The Ancient book lays each family out as rush, blitz, burst, barrage on four consecutive buttons (type 0, 2, 1, 3). Each family maps its first button id to a base; `SpellType` (multi-hit, effect selection, slow/poison scaling) stays a code constructor argument next to the DTO.
- **Visual ids stay in code.** Animations, graphics and projectiles vary per type and per effect (some spells render none, some render on the caster). Persisting them would diverge from what the code renders, so rows carry the `-1` defaults and the code keeps them.
- **Staff requirement stays in code** (Zuriel's staff ids). It is a single behavioral restriction, not duplicated persisted data.
- **Seeding** through an `IGameWorldSeeder`; rows are only added when the key is absent and never overwritten.

## Risks

- A missing Ancient row would leave a spell unavailable; the seeder tests assert all 20 rows and the migration is applied before seeding.
- Definition edits (standard and Ancient) need a GameWorld restart to be visible, as with the other startup-loaded stores.
