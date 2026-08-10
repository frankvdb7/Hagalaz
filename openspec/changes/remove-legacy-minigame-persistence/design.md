## Context

The four character-specific minigame entities are already marked obsolete and
have no live callers. Barrows, Duel Arena, Godwars, and TzHaar character state
is read from and written to the existing `Character.Profile`, which is persisted
through `characters_profiles.data`. `MinigamesTzhaarCaveWave` is different: it
stores static wave definitions used by the GameWorld repository and remains
needed.

The current EF model still exposes both the profile store and the four legacy
tables. The change must remove only the obsolete character-state surface and
must preserve migration history.

## Goals / Non-Goals

**Goals:**

- Make `Character.Profile` the only live persistence surface for the four
  minigames.
- Remove the four obsolete CLR entities, navigations, `DbSet`s, and mappings.
- Drop the four obsolete tables through the existing EF migration mechanism.
- Keep the TzHaar wave-definition entity/table and all minigame runtime code.
- Verify the schema cleanup through MySQL migration integration coverage.

**Non-Goals:**

- Migrating rows from the legacy tables into profile JSON.
- Changing profile keys, DTOs, serializers, minigame behavior, or wave data.
- Editing historical migrations or adding a compatibility wrapper.
- Adding a new persistence service, worker, queue, or dependency.

## Decisions

### Use the existing profile store

The active scripts already own minigame character state through
`Character.Profile`; no replacement code is needed. Keeping a relational
minigame model would preserve duplicate ownership, while adding a new service
or abstraction would expand the scope without a requirement.

### Remove the live EF model and add a schema migration

Delete the obsolete entity surface from the current model and add one new
reversible migration that drops exactly the four tables. Historical migration
files and designers remain immutable so a fresh database can still replay the
original schema before applying the cleanup migration.

### Drop legacy rows without backfill

The approved compatibility policy treats profile JSON as authoritative and
allows legacy rows to be discarded. A conversion would require collision
precedence for existing profile keys and would add migration-specific business
logic that is outside this cleanup.

### Keep TzHaar wave definitions

Only `MinigamesTzhaarCave` is character state. `MinigamesTzhaarCaveWave` is a
static definition source used by `ITzhaarWaveDefinitionRepository` and must not
be removed with the character-state entities.

## Risks / Trade-offs

- [Risk] Applying the migration permanently removes legacy table rows. →
  Mitigation: document the drop-only policy, deploy through the existing
  migration runner, and retain the reversible `Down` schema definition; take
  the normal database backup before deployment.
- [Risk] Historical migration metadata still contains the old entity/table
  names. → Mitigation: leave historical files unchanged and validate live source
  references separately from migration history.
- [Risk] A generated model snapshot accidentally removes the wave-definition
  entity as well. → Mitigation: review the snapshot and assert the wave table
  remains in integration coverage.

## Migration Plan

1. Deploy the code and generated migration through the existing database
   migration runner.
2. The migration drops the four unused tables; no data conversion runs.
3. Verify no pending migrations, absence of the four tables, and presence of
   `minigames_tzhaar_cave_waves`.
4. If rollback is required, roll back the application and apply the migration's
   `Down` operation to recreate the former schema. Legacy row contents are not
   recoverable from the migration itself.

## Open Questions

None. The drop-only policy and preserved TzHaar wave-definition scope are
approved decisions.
