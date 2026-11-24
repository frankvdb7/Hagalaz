# Design

## Context

Obstacle scripts are `GameObjectScript`s created per object with constructor DI; click handlers are synchronous and existing scripts (`RawFood`, `StandardLog`) already resolve DB-backed services with `.Result`. Firemaking provides the entity → repository → service → AutoMapper → DI pattern, and `FarmingSeedSeeder` the idempotent seeder pattern.

## Goals / Non-Goals

**Goals:** persist obstacle XP and course completion XP; preserve values exactly; no EF access in scripts.

**Non-Goals:** obstacle behavior, render/animation/location ids, a generic engine.

## Decisions

- **Two tables.** `skills_agility_obstacle_definitions(object_id PK, experience decimal(11,2), required_level tinyint default 1)` and `skills_agility_course_definitions(id PK, completion_experience decimal(11,2))`. No obstacle→course FK because basic and advanced courses share obstacles. Alternative: one table per course with duplicated rows — rejected as duplicate data.
- **Course identity** is a code-side `AgilityCourseId` enum (GnomeBasic=1, GnomeAdvanced=2) matching the seed rows; state-completion logic stays in `Agility`.
- **Service** `IAgilityService`: synchronous `FindObstacleByObjectId(int)` and `FindCourseById(AgilityCourseId)` returning DTOs or null. Definitions are preloaded into an in-memory read model by a startup service (the `EquipmentStore` pattern), so obstacle callbacks on the game loop never perform database I/O. Scripts treat null as "no experience" with no hard-coded fallback. Alternative: per-click async EF queries with `.Result` — rejected because they block the shared scheduler.
- **Scripts** inject `IAgilityService` and use `Owner.Id`; `Agility.CheckGnomeCourseCompletion` takes the service.
- **Seeding** via an `IGameWorldSeeder` in C#; the EF migration is schema-only (no raw SQL).

## Risks / Trade-offs

- Definition edits need a restart to take effect (same as other startup-loaded stores).
- Missing seed row silently grants no XP → covered by seeder tests asserting all Gnome ids.
