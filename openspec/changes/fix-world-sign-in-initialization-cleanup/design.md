## Context

`HandshakeHub.SignInWorld` commits authentication and transfers the protocol,
then publishes `WorldSignInCommand` without awaiting it. The consumer invokes
`ICharacter.OnRegistered`, sends contact/world presence messages, and currently
has no failure cleanup. `AuthenticationService` cannot clean this failure because
its sign-in operation has already returned successfully.

## Decision

Keep `WorldSignInCommandConsumer` as the single owner of post-authentication
initialization and add a local failure path there. On any initialization or
presence-publication exception it will:

1. Destroy the character, removing any region membership created before the
   failure.
2. Remove the character from the character store.
3. Release the game session and always remove its local store entry.
4. Publish the existing `WorldUserSignOutMessage` so world/contact presence can
   converge.
5. Rethrow the original initialization exception.

Cleanup failures are logged individually and do not replace the original
failure. The existing strict duplicate check in `MapRegion.Add` remains intact.

## Alternatives considered

- Making `MapRegion.Add` idempotent would hide a lifecycle error and would not
  release the leaked session or external presence.
- Moving all work into `AuthenticationService` would expand the authentication
  boundary and still leave asynchronous post-authentication work without a
  clear owner.
- Relying on disconnect cleanup would leave the client stuck and makes recovery
  depend on transport behavior rather than the failed initialization boundary.
