## Why

`CharactersPermission` is an obsolete authorization model that duplicates the
ASP.NET Identity role assignment already used by the authorization and
GameWorld services. The Contacts service still reads the legacy table when it
builds sender rights for chat notifications, so the live model retains a
second permission source that can drift from Identity roles.

## What Changes

- Replace Contacts-service permission enrichment with the existing
  `Character.Aspnetuserroles` / `Aspnetrole` relationship.
- Remove the obsolete `CharactersPermission` entity, character navigation,
  `DbSet`, EF mapping, repository, and unit-of-work surface. **BREAKING**
- Add a reversible EF migration that drops the obsolete
  `characters_permissions` table without changing the Identity role tables.
- Preserve the existing contact-message claim payload shape and client-rights
  calculation, with role names supplied by ASP.NET Identity.
- Add focused migration/model coverage and keep historical migrations intact.

## Capabilities

### New Capabilities

- `character-authorization`: Defines Identity roles as the authoritative source
  for character authorization names exposed to contact-message consumers.

### Modified Capabilities

- None.

## Impact

- `Hagalaz.Data` entity, `Character` navigation, EF model, migration snapshot,
  and database schema.
- `Hagalaz.Services.Contacts` character projection, service enrichment path,
  repository, and unit-of-work contract.
- Contact-message runtime behavior remains contract-compatible, but existing
  databases lose the unused `characters_permissions` table when the cleanup
  migration is applied.
- No new packages, workers, queues, caches, or authorization mechanisms.

## Non-Goals

- Do not rename the existing message `Claims` fields or change the game-client
  rights protocol.
- Do not modify historical migrations or migrate rows from the legacy table.
- Do not change role definitions, role seeding, or Identity authorization
  policies.

## Acceptance Criteria

- No live production source references `CharactersPermission` or
  `CharactersPermissions`; `characters_permissions` appears only in the
  cleanup migration, migration assertions, and historical migrations.
- Contacts-service character lookups expose the names of the character's
  ASP.NET Identity roles through the existing claim DTO and preserve the
  existing rights mapping.
- The latest migration drops exactly `characters_permissions`; its `Down`
  path recreates the former table schema and foreign key.
- The current EF model snapshot excludes the legacy entity/table while
  retaining `aspnetroles` and `aspnetuserroles`.
- Focused mapping/Contacts tests, migration validation, and the solution build
  pass.

## Stop Conditions

- Stop if removing the legacy table would require role-data conversion or a
  change to the external contact-message contract.
- Stop if another live caller of the legacy table is discovered outside the
  scoped Contacts authorization enrichment path; record it as follow-up work.
