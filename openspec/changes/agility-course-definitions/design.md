# Design

## Context

Obstacle scripts are `GameObjectScript`s created per object with constructor DI; click handlers are synchronous and existing scripts (`RawFood`, `StandardLog`) already resolve DB-backed services with `.Result`. Firemaking provides the entity → repository → service → AutoMapper → DI pattern, and `FarmingSeedSeeder` the idempotent seeder pattern.

## Goals / Non-Goals

**Goals:** persist obstacle XP and course completion XP; preserve values exactly; no EF access in scripts.

**Non-Goals:** obstacle behavior, render/animation/location ids, a generic engine.

## Decisions

- **Two tables.** `skills_agility_obstacle_definitions(object_id PK, experience decimal(11,2), required_level tinyint default 1)` and `skills_agility_course_definitions(id PK, completion_experience decimal(11,2))`. No obstacle→course FK because basic and advanced courses share obstacles. Alternative: one table per course with duplicated rows — rejected as duplicate data.
- **Course identity** is a code-side `AgilityCourseId` enum (GnomeBasic=1, GnomeAdvanced=2) matching the seed rows; state-completion logic stays in `Agility`.
- **Service** `IAgilityService`: `FindObstacleByObjectId(int)` and `FindCourseById(AgilityCourseId)` returning DTOs or null. Scripts treat null as "no experience" with no hard-coded fallback.
- **Scripts** inject `IAgilityService` and use `Owner.Id`; `Agility.CheckGnomeCourseCompletion` takes the service. Lookups use `.Result` per existing precedent.
- **Seeding** via an `IGameWorldSeeder` in C#; the EF migration is schema-only (no raw SQL).

## Risks / Trade-offs

- Script-held services capturing a disposed scoped context → follow the lifetime registration used by Firemaking/Cooking services and verify scripts are built from a live scope.
- Missing seed row silently grants no XP → covered by seeder tests asserting all Gnome ids.
