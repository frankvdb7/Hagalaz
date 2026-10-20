## Context

`CharactersPermission` is a legacy relational entity with the same role names
that are now stored in ASP.NET Identity's `aspnetroles` and
`aspnetuserroles` tables. The Contacts service is the only live production
consumer: it queries the legacy table to populate the existing `Claims` field
on contact-message sender data, which the GameWorld service converts to the
client's sender-rights value.

The change crosses the current EF model, the Contacts projection, and the
database schema. Existing contact-message consumers and the client protocol
are compatibility boundaries.

## Goals / Non-Goals

**Goals:**

- Make Identity role assignments the sole source of role names used in
  contact-message sender-rights enrichment.
- Remove the obsolete live entity and repository surface.
- Remove the legacy table through one reversible EF migration.
- Preserve the existing `Claims` DTO/message shape and rights calculation.

**Non-Goals:**

- Renaming claims to roles in the message contract.
- Changing Identity role definitions, role assignment APIs, token claims, or
  authorization policies.
- Backfilling or synchronizing rows from `characters_permissions`.
- Adding a role repository, cache, worker, retry path, or new persistence
  mechanism.

## Decisions

### Project role assignments through the existing character query

The Contacts `CharacterProfile` will project `Character.Aspnetuserroles`
through each assignment's `Role` navigation into the existing claim DTO. The
existing `CharacterService` lookup remains the owner of the contact sender
read and no second enrichment query or repository is retained.

This is preferred over a new role repository because the role names are
projection data already reachable from the character query. It is also
preferred over calling `UserManager.GetRolesAsync` because that would add a
service-level Identity round trip and duplicate the query responsibility
without changing the authoritative store.

### Keep the external claims payload stable

The role names continue to be emitted as `CharacterDto.ClaimDto.Name` and
`ContactMessageNotification.SenderDto.Claims`. `ClientPermissionProvider`
already consumes those names, so preserving the message shape keeps the
distributed boundary and client behavior unchanged.

### Remove the live model and drop the legacy table

Delete the obsolete entity, character navigation, `DbSet`, EF mapping, and
Contacts permission repository. Add one generated, reversible migration that
drops `characters_permissions` and recreates its former schema in `Down`.
Historical migrations and designers remain immutable so fresh databases can
replay history before applying the cleanup migration.

The existing EF migration runner owns the database state transition. No new
retry or reconciliation owner is introduced; the runner's existing migration
execution and deployment retry behavior remains authoritative.

### Do not convert legacy rows

Identity role assignments are already the source used by authorization and
GameWorld. Copying legacy rows would require precedence rules for conflicts and
would preserve the duplicate source this change removes. The cleanup is
therefore intentionally drop-only, with the migration's `Down` path restoring
schema but not discarded row contents.

## Risks / Trade-offs

- [Risk] A database may contain permission rows that were never copied into
  Identity roles. → Mitigation: verify the live role-assignment path before
  deployment; do not silently invent a conversion policy in this refactor.
- [Risk] Removing the table is destructive to legacy rows. → Mitigation: use
  the normal database backup and migration deployment process; retain a
  reversible schema definition in `Down`.
- [Risk] Historical migration designers still mention the removed entity. →
  Mitigation: leave historical files unchanged and validate only current live
  source/model references.
- [Risk] A nullable Identity role name could produce an invalid claim. →
  Mitigation: exclude role assignments without a name from the projection;
  Identity-created roles are expected to have names.

## Migration Plan

1. Deploy the code and cleanup migration through the existing migration runner.
2. The migration drops only `characters_permissions`; Identity role tables are
   untouched.
3. Verify no pending migrations, absence of the legacy table, presence of
   `aspnetroles` and `aspnetuserroles`, and successful role-backed contact
   sender projection.
4. If rollback is required, revert application code and apply the migration's
   `Down` operation to recreate the former table schema. Legacy row contents
   are not recoverable from this migration.

## Open Questions

None.
