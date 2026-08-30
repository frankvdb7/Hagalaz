# Tasks

## Authorization and GameWorld

- [x] 1.1 Add tokenless credential-validation request/response backed by `PasswordGrantCommand`, preserving existing outcomes and returning the subject without token work.
- [x] 1.2 Add `AuthenticateWorldReconnectAsync` through the existing auth resilience/rate-limit pipeline without fresh-login side effects.
- [x] 1.3 Add exact-type reconnect handling, existing session resolution, master-ID ownership checks, and #477 delegation without new session/character registration.

## Raido handoff

- [x] 2.1 Enable stateful reconnect only for GameWorld and add the one private physical candidate-owned claim inside the existing transition.
- [x] 2.2 Add the smallest physical response write/flush path that fails closed on exceptions, cancellation, unsafe closure/completion, or ambiguity.
- [x] 2.3 Commit protocol installation, physical publication, input-boundary relinquishment, and the existing waiter in the required order; make stale candidate continuations harmless.

## Protocol and tests

- [x] 3.1 Add response 15 and the exact 4,608-byte player-entry encoder while preserving standard-map output.
- [x] 3.2 Add focused authorization, GameWorld, response, and Raido tests for ownership, concurrency, claim ownership, flush ordering/outcomes, timeout/abort, cleanup, and protocol publication.

## Validation

- [x] 4.1 Run strict OpenSpec validation, focused tests serially, solution build, full solution tests, and review the cumulative diff for prohibited scope. Full solution integration tests remain environment-blocked because Docker is unavailable.
