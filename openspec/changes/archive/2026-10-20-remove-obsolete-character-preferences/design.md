## Context

`CharactersProfile` and its profile repository already own character profile JSON persistence. `CharactersPreference` is not referenced by application code, but it remains registered in `HagalazDbContext`, mapped to `characters_preferences`, and represented in the current model snapshot. The change crosses the EF model and schema only; historical migrations must remain replayable.

## Goals / Non-Goals

**Goals:**

- Make the profile entity the sole live persistence boundary for character preferences.
- Remove the unused legacy entity and current EF metadata.
- Let the existing EF migration runner own one reversible schema transition that drops `characters_preferences`.

**Non-Goals:**

- Backfilling or interpreting legacy preference rows.
- Changing profile JSON keys, repositories, or distributed character persistence contracts.
- Adding a compatibility type, adapter, second store, or cleanup worker.

## Decisions

### Remove the live model and preserve migration history

Delete `CharactersPreference.cs`, its `DbSet`, and its `OnModelCreating` mapping. Remove only the entity block from the current model snapshot; historical migrations and their designers remain unchanged so a fresh database can replay the original table creation before applying the cleanup migration.

The alternative of retaining the obsolete model with a warning suppression would leave a second persistence source and would not satisfy the cleanup goal. Editing historical migrations would break migration history and is rejected.

### Use the existing EF migration runner for the schema transition

Add one reversible migration that drops exactly `characters_preferences`. Its `Down` operation recreates the historical table definition from `InitialCreate`, without attempting to restore discarded rows. The existing migration application service remains the sole owner of schema state and retry behavior.

The alternative of a startup SQL script or background cleanup task would create a second schema-transition mechanism and is rejected.

### Keep profile data authoritative without conversion

Do not copy legacy rows into `characters_profiles`. Current application callers already use the profile repository, and no conversion or conflict policy was requested. Database backup and the migration's schema-only `Down` path are the rollback boundary.

## Risks / Trade-offs

- [Risk] A deployment may contain legacy rows not represented in profile JSON. → Mitigation: verify the absence of live callers before migration; intentionally make row conversion a separate change if evidence appears.
- [Risk] Generated EF snapshot output may include unrelated provider churn. → Mitigation: review the generated diff and retain only removal of the obsolete entity from the current snapshot.
- [Risk] Applying the migration is destructive to the legacy table's rows. → Mitigation: use the normal database backup process and keep the historical schema in `Down`; row restoration is explicitly out of scope.

## Migration Plan

1. Deploy the code and new migration through the existing database migration runner.
2. The migration drops only `characters_preferences`; `characters_profiles` and all other tables remain unchanged.
3. Verify there are no pending migrations and the legacy table is absent in integration coverage.
4. If rollback is required, revert the application code and apply the migration `Down` operation to recreate the schema. Legacy row contents are not restored.

## Open Questions

None.
