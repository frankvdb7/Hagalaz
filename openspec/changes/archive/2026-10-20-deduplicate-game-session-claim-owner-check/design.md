# Design

## Context

The three operations already compose their work inside `WithClaimLockAsync`. Each reads the same claim key and checks that a value exists and exactly matches the supplied claim id before taking its operation-specific action.

## Goals / Non-Goals

**Goals:**

- Keep the distributed lock and operation-specific mutations in the existing methods.
- Centralize the repeated claim-owner rule in the existing store.
- Keep the public operation results and side effects directly testable.

**Non-Goals:**

- Unify remove, refresh, and callback behavior behind a mode, delegate, or generic operation.
- Change claim locking, renewal duration, cache visibility, or cancellation.

## Decisions

- Add a private instance method that reads the claim with the existing `_cache`, `GetKey`, `EntryOptions`, and lock-scoped cancellation token, then returns whether the claim exists and matches exactly.
  - A static utility, interface, or new service would separate one store-owned rule from the cache and lock that give it meaning.
- Keep each method's existing action after the check. Release removes; renew refreshes; execute-if-owner refreshes and then calls its callback. Those actions have different contracts and do not belong in a shared callback abstraction.
- Extend the public API tests so mismatched owners are rejected by renewal and callback execution without writing or invoking the callback. Existing tests cover release rejection, missing claims, and matching-owner actions.

## Risks / Trade-offs

- The helper adds a private call in each operation, while removing repeated ownership logic and preserving the lock boundary.

## Migration Plan

No persisted data or public API signatures change. Rollback consists of restoring the repeated cache lookup and owner comparison to the three operations.
