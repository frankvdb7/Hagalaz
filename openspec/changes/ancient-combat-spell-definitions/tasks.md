# Tasks

## 1. Persistence
- [x] 1.1 Add `spell_book` to the entity, DbContext and a schema-only migration; update the migration-count integration test
- [x] 1.2 Add `AncientCombatSpellSeeder` (20 rows) and register it; verify seeder tests for values and idempotency

## 2. Service and scripts
- [x] 2.1 Add `SpellBook` to `CombatSpellDto` and the mapping; verify mapping test
- [x] 2.2 Refactor Ancient spells to `CombatSpellDto`; remove `AncientSpellData` and `AncientMiasmicSpellData`
- [x] 2.3 Wire `SpellBookTab` by button lookup; verify per-button tests

## 3. Verification
- [x] 3.1 `dotnet build Hagalaz.sln` and affected test projects; no new warnings
