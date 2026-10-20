## 1. Remove the obsolete live model surface

- [x] 1.1 Remove the four obsolete entity files and their `Character` navigation properties, `DbSet`s, and EF mappings while retaining `MinigamesTzhaarCaveWave` and its mapping.
- [x] 1.2 Confirm the remaining production source references only the profile-backed minigame state and the static TzHaar wave-definition entity.

## 2. Apply the schema cleanup

- [x] 2.1 Add a reversible EF migration that drops exactly the four legacy minigame tables and recreates their former columns, indexes, and foreign keys in `Down`.
- [x] 2.2 Regenerate and review the current EF model snapshot without modifying historical migrations or designers.

## 3. Verify migration and regression coverage

- [x] 3.1 Update the MySQL migration integration tests for the new migration count and assert the four legacy tables are absent while the TzHaar wave-definition table remains.
- [x] 3.2 Run focused data/minigame tests, the solution build, OpenSpec validation, and live-reference checks; record any environment-limited validation.
