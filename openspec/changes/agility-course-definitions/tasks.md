# Tasks

## 1. Characterization tests

- [x] 1.1 Add script tests locking current Gnome literals (obstacle XP 7.5/5/25, completion 39 and 605 with state clearing) and verify they pass before refactor

## 2. Persistence and seeding

- [x] 2.1 Add entities, DbSets, model configuration and a schema-only migration; verify migration builds and snapshot is updated
- [x] 2.2 Add Agility seeder (all Gnome object ids incl. 69377/69378 and 2312/4059, both courses), register in `GameWorldSeeding`; verify seeder tests for values and idempotency

## 3. Service

- [x] 3.1 Add `IAgilityService`, DTOs, repository, service, mapping profile and DI; verify service lookup tests (known, unknown)

## 4. Scripts

- [x] 4.1 Replace literals in Gnome obstacle scripts and `AgilitySkillService` with service lookups; verify tests from 1.1 pass against a substituted service

## 5. Integration

- [x] 5.1 Run `dotnet build Hagalaz.sln` and affected test projects; verify no new warnings
