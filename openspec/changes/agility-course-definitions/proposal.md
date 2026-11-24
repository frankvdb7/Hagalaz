# Proposal

## Why

Gnome Agility obstacle XP and course completion XP (39 basic, 605 advanced) are literals inside obstacle scripts and `AgilitySkillService`. Content tuning requires code changes, unlike skills whose definitions already live in database-backed services. See GitHub issue #379 (child of #373).

## What Changes

- Add two small tables: course definitions (completion XP) and obstacle definitions (XP and required level per object id).
- Seed the current Gnome values (all obstacle object ids, basic and advanced course completion) through an idempotent game-world seeder.
- Add `IAgilityService` with DTOs, backed by repository + service + AutoMapper profile, registered in GameWorld DI.
- Gnome obstacle scripts and `AgilitySkillService` read XP from the service instead of literals.
- Non-goals: movement, queued tasks, states, messages, render ids, animations and locations stay in C#; no generic obstacle engine; no value rebalancing.

## Capabilities

### New Capabilities
- `agility-course-definitions`: persisted Agility obstacle and course-completion XP definitions consumed by scripts through a service.

### Modified Capabilities

## Impact

- `Hagalaz.Data` (entities, DbContext, migration), `Hagalaz.Database.Migrations` (seeder), `Hagalaz.Game.Abstractions` (service + DTOs), `Hagalaz.Services.GameWorld` (repository, service, profile, DI), `Hagalaz.Game.Scripts` (Gnome course scripts), and tests in the matching test projects.
