## Authorization and GameWorld

- [x] 1.1 Add tokenless credential validation backed by `PasswordGrantCommand`,
      preserving existing validation outcomes and returning the subject.
- [x] 1.2 Add `AuthenticateWorldReconnectAsync` through the existing
      resilience/rate-limit pipeline without fresh-login side effects.
- [x] 1.3 Handle `WorldReconnectRequest` with existing connection lookup,
      master-ID/session/character ownership checks, response construction, and
      no fresh session or character registration.

## Raido bridge

- [x] 2.1 Enable stateful reconnect for GameWorld only.
- [x] 2.2 Reuse the existing dispatcher and handler lifecycle without adding a
      post-dispatch action or transport-lifetime bridge.
- [x] 2.3 Extend the existing `TryReconnect` publication path to obtain the
      temporary physical connection internally and install the replacement
      protocol before transport publication and waiter completion.

## Protocol and tests

- [x] 3.1 Add the response and required player-entry encoder while preserving
      standard-map serialization.
- [x] 3.2 Adapt focused authorization, GameWorld, response, and reconnect tests
      to the existing stateful reconnect behavior.

## Validation

- [x] 4.1 Run strict OpenSpec validation, focused tests, solution build, full
      solution tests, and review the cumulative diff for prohibited scope.
