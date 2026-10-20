# Proposal

## Why

Lobby and world sign-in repeat the same contact-feature lookup and snapshot-window startup before calling the existing `ContactSnapshotLoader`. Keeping this orchestration in two consumers can make snapshot ownership and cleanup diverge as either sign-in path changes.

## What Changes

- Move connection lookup, contact-feature discovery, and observation-window startup into the existing `ContactSnapshotLoader`.
- Have both sign-in consumers invoke that loader alongside their existing sign-in side effects.
- Preserve the request identity, snapshot behavior, sign-in messages, and current cancellation and failure handling.

## Scope and Acceptance Criteria

- Only contact-snapshot bootstrap shared by `LobbySignInCommandConsumer` and `WorldSignInCommandConsumer` changes.
- A missing connection or contacts feature remains a no-op for snapshot loading while sign-in continues.
- When a feature exists, the loader begins and closes its observation window and completes its initial-snapshot gate on success, timeout, fault, or cancellation.
- Lobby world-info dispatch and both sign-in publications retain their existing behavior.
- Focused public consumer tests pass, including request identity, snapshot application, timeout/fault continuation, and cancellation cleanup.
- If preserving the existing sequencing requires changing external behavior or adding another state owner, stop and propose a separate change.

## Non-Goals

- Do not change `IContactsFeature`, session generation, presence reconciliation, message contents, or contact snapshot validation.
- Do not add a static helper, interface, generic abstraction, retry path, or new state owner.
- Do not refactor other jscpd findings in this change.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

None. This is an implementation-only refactor; existing sign-in and contact-presence behavior remains the contract.

## Impact

- `Hagalaz.Services.GameWorld/Features/ContactSnapshotLoader.cs`
- `Hagalaz.Services.GameWorld/Mediator/Consumers/LobbySignInCommandConsumer.cs`
- `Hagalaz.Services.GameWorld/Mediator/Consumers/WorldSignInCommandConsumer.cs`
- Focused GameWorld consumer tests

No API contract, dependency, registration, or runtime topology changes are required.
