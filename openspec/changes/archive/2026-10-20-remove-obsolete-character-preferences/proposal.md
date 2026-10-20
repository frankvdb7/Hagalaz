## Why

`CharactersPreference` is an obsolete relational character-settings model. Character preferences are now stored in the profile JSON represented by `CharactersProfile`, but the legacy entity, EF mapping, and `characters_preferences` table remain part of the live model.

## What Changes

- Remove the obsolete `CharactersPreference` entity, `DbSet`, and EF mapping. **BREAKING**
- Add a reversible EF migration that drops the unused `characters_preferences` table.
- Keep `CharactersProfile`/`characters_profiles` as the authoritative character profile store.
- Update the existing MySQL migration integration assertions and current EF model snapshot; leave historical migrations unchanged.

## Capabilities

### New Capabilities

- `character-profile-persistence`: Defines the profile-backed character persistence boundary and removal of the legacy preferences table.

### Modified Capabilities

- None.

## Impact

- `Hagalaz.Data` entity types, `HagalazDbContext`, and the current EF model snapshot.
- The database schema through one new migration that removes `characters_preferences`; the migration's `Down` path restores its historical schema.
- MySQL migration integration coverage.

## Non-Goals

- Do not migrate or transform rows from `characters_preferences` into profile JSON.
- Do not change the `CharactersProfile` JSON contract, profile repositories, or character persistence behavior.
- Do not edit historical migrations or add a second settings store, worker, cache, or compatibility wrapper.

## Acceptance Criteria

- No live production source outside historical migrations references `CharactersPreference` or `CharactersPreferences`.
- The current EF model and snapshot contain `CharactersProfile` but not `CharactersPreference` or `characters_preferences`.
- The cleanup migration drops exactly `characters_preferences`, and its `Down` operation recreates the former table schema.
- The existing migration integration test asserts that `characters_preferences` is absent and that the migration set is fully applied.
- Focused validation and the solution build pass.

## Stop Conditions

- Stop if a live application caller of `CharactersPreference` is discovered; record that caller as follow-up work rather than deleting its behavior.
- Stop if removing the table requires a profile-data conversion policy not stated in this change.
