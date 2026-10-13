# Design

## Context

See proposal.md. The current boundary/transaction design already owns synchronization, rollback, and post-commit publication; the fixes use those existing scopes.

## Goals / Non-Goals

**Goals:** Validate the identities captured by Equipment preflight while its existing transaction owns inventory and Equipment. Preserve committed storage while attempting each independent completion/publication stage once.

**Non-Goals:** New locks, transaction helpers, retry behavior, public APIs, or changes to generic equality-based transfer semantics.

## Decisions

- Keep `CanEquipItem` and `CanUnEquipItem` outside the transaction. Validate the captured incoming, weapon, and shield instances and incoming slot/count immediately after acquiring the existing replacement transaction and before mutation.
- When the weapon/shield path captured both slots empty before acquisition, verify both are still empty immediately after the existing transaction begins; reject stale state without recomputing equipment requirements.
- In `UnEquipItem`, validate the exact equipped instance and select the preferred destination after acquiring its existing transaction. If the requested slot is occupied, retain the existing ordinary destination-selection fallback.
- Keep standalone transfer ownership and transaction cleanup as implemented. Catch failures around each independent before-publication hook, source/destination publisher, completion owner, and changed boundary. Preserve phase order and propagate one original exception or aggregate multiple failures.
- Update the canonical specification and the active completed transfer delta so neither prescribes first-failure suppression.

## Risks / Trade-offs

- [Risk] A publisher can fail after storage commits and later independent publishers can also fail. → Attempt each once, retain all failures, and never roll back or retry authoritative storage.
- [Risk] Eligibility callbacks can alter storage before ownership. → Revalidate exact object identity under the existing transaction before any transfer.
