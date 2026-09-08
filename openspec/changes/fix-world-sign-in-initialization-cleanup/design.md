## Context

`HandshakeHub.SignInWorld` commits authentication and transfers the protocol,
then publishes `WorldSignInCommand` without awaiting it. The consumer invokes
`ICharacter.OnRegistered`, sends contact/world presence messages, and must abort
the connection if initialization fails. `ConnectionHub` and
`AuthenticationService` own the resulting disconnect cleanup.

## Decision

Keep `WorldSignInCommandConsumer` responsible only for post-authentication
initialization. On any initialization or presence-publication exception it logs,
aborts the session, and rethrows the original exception. `ConnectionHub` and
`AuthenticationService.SignOutAsync` remain the cleanup owner, so character
destruction, persistence, session release, detachment, and world-presence
cleanup occur through one lifecycle path.

## Alternatives considered

- Making `MapRegion.Add` idempotent would hide a lifecycle error and would not
  release the leaked session or external presence.
- Moving all work into `AuthenticationService` would expand the authentication
  boundary and still leave asynchronous post-authentication work without a
  clear owner.
- Relying on the existing disconnect cleanup is intentional: aborting the
  connection invokes the established lifecycle owner and avoids two competing
  cleanup paths.
