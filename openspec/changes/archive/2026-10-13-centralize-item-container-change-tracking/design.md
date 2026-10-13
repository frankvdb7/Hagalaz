# Design

## Context

`MutationScope` currently holds standalone slot sets, while `ItemContainerTransaction` holds a second merge dictionary. The mutation boundary already serializes mutations and coordinates publication ownership, so it is the synchronization owner for a single tracker.

## Goals / Non-Goals

**Goals:** Keep committed and transaction-provisional slot batches on the boundary-owned tracker; expose pull-based reads only on concrete `ItemContainer`; preserve transaction and publisher lifecycle; incrementally update PriceChecker.

**Non-Goals:** Change lock ordering, participant resolution, transaction lifecycle, publication sequencing, generic container contracts, or projected inventory construction.

## Decisions

- Use a sealed concrete tracker with only `HasChanges` and `GetSlots()` public. It delegates public reads to boundary methods; the boundary acquires its existing monitor and rejects same-thread reads during mutation, transaction, or publication ownership.
- Keep committed and pending state separate inside the tracker. Mutation scopes record to the tracker; commit promotes pending changes, and rollback discards pending changes. One merge method handles partial and full changes.
- Automatic publishers consume the boundary's committed tracker batch before invoking existing callbacks. Boundaries without publishers leave committed batches available for callers.
- Standalone transfers record both tracker batches while the deterministic pair locks are held, then keep existing source-before-destination publication and ownership cleanup.
- PriceChecker consumes tracked selection slots after successful operations, performs a forced full refresh on open, and leaves projected-inventory refresh independent.

## Risks / Trade-offs

- A tracker read from the same thread while it owns a mutation scope must fail clearly; foreign reads wait through existing boundary ownership and deadlock-prevention rules.
- A publisher failure occurs after its batch is consumed, matching the existing no-retry publication behavior.
- The public tracker is available only on concrete `ItemContainer`; general `IItemContainer` contracts remain unchanged.

## Migration Plan

Update the boundary and transaction internals in place, then migrate PriceChecker and add regression coverage. There is no data migration or deployment sequencing requirement. Revert the code and spec change together if validation finds a publication or transaction regression.
