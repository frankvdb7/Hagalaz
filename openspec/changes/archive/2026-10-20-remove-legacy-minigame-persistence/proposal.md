## Why

The four per-character minigame EF entities are obsolete: the active Barrows,
Duel Arena, Godwars, and TzHaar scripts already persist their state through the
shared `Character.Profile` JSON store. Keeping the unused entity types, model
mappings, and tables leaves a second persistence surface that can drift from the
authoritative profile data.

## What Changes

- Remove the `MinigamesBarrow`, `MinigamesDuelArena`, `MinigamesGodwar`, and
  `MinigamesTzhaarCave` entities, character navigations, `DbSet`s, and EF model
  mappings. **BREAKING**
- Add a reversible EF migration that drops the four obsolete tables.
- Keep `MinigamesTzhaarCaveWave` and its static wave-definition persistence.
- Keep all existing profile keys and minigame runtime behavior unchanged.
- Do not backfill legacy table rows into profile JSON; the profile is already
  authoritative for active behavior.
- Update migration integration coverage and the current model snapshot.

## Capabilities

### New Capabilities

- `character-profile-persistence`: Documents that active minigame character
  state is owned by the shared profile JSON store and that obsolete relational
  minigame tables are not part of the live model.

### Modified Capabilities

- None.

## Impact

- `Hagalaz.Data` public entity and `DbSet` surface, EF model metadata, and the
  database schema.
- The existing database migration runner and MySQL integration tests.
- No new packages, workers, queues, or runtime persistence mechanisms.
- Existing databases lose the four legacy tables when the migration is applied;
  their rows are intentionally not converted or retained.

## Acceptance Criteria

- The live source model contains no references to the four removed entities or
  tables outside historical migration files.
- The latest migration drops exactly the four obsolete tables and its `Down`
  path recreates their former schema.
- Profile-backed minigame scripts and the TzHaar wave-definition table remain
  unchanged and build successfully.
- Empty-database migration, concurrent migration startup, focused tests, and
  the solution build pass.

## Stop Conditions

- Do not modify historical migration files or migration designers.
- Do not change profile JSON keys, DTOs, or minigame runtime logic.
- Do not add a data conversion path, compatibility wrapper, or replacement
  relational table.
