## 1. Remove the obsolete live model

- [x] 1.1 Remove `CharactersPreference.cs`, its `HagalazDbContext` `DbSet`, and its EF mapping; verify no live production references remain.
- [x] 1.2 Update the current EF model snapshot to retain `CharactersProfile` while excluding `CharactersPreference` and `characters_preferences`; leave historical migrations unchanged.

## 2. Apply and verify the schema cleanup

- [x] 2.1 Add a reversible EF migration that drops exactly `characters_preferences` and recreates its historical schema in `Down`.
- [x] 2.2 Update the existing MySQL migration integration assertions for the new migration and legacy-table removal.
- [x] 2.3 Run focused model/migration validation and the solution build; record any environment-limited checks.

Validation note: OpenSpec validation, EF pending-model validation, focused Data and integration-test-project builds, the full solution build, and all three MySQL integration tests passed.
