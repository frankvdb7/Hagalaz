## 1. Replace the live authorization source

- [x] 1.1 Project Identity role assignments into the existing Contacts sender
  claim DTO and remove the legacy permission enrichment query.
- [x] 1.2 Remove the obsolete `CharactersPermission` entity, character
  navigation, `DbSet`, EF mapping, Contacts repository, and unit-of-work
  contract; confirm no live references remain.

## 2. Apply the schema cleanup

- [x] 2.1 Add a reversible EF migration that drops exactly
  `characters_permissions` and recreates its former schema in `Down`.
- [x] 2.2 Regenerate and review the current EF model snapshot while leaving
  historical migrations and designers unchanged.

## 3. Verify behavior and regression coverage

- [x] 3.1 Add focused Contacts projection coverage for assigned and unassigned
  Identity roles while preserving the existing claim payload shape.
- [x] 3.2 Update MySQL migration integration coverage for the new migration and
  assert the legacy table is absent while Identity role tables remain.
- [x] 3.3 Run focused tests, migration/model validation, the solution build, and
  OpenSpec validation; record any environment-limited checks.
