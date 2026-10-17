# Design

## Context

See `proposal.md` for the motivation. The existing `MutationScope` already acquires a boundary lock, records tracker changes, and supports retaining standalone ownership while a domain performs completion.

## Goals / Non-Goals

**Goals:** Let a domain operation run ordinary `ItemContainer` mutations inside one boundary-owned scope, release the physical lock before publication, and keep logical ownership until the scope is disposed.

**Non-Goals:** Change transaction snapshot/rollback behavior, publication policy, domain events, or multi-container operation semantics.

## Decisions

1. Extend `MutationScope` rather than introducing another type. A domain scope may contain nested ordinary `ItemContainer` mutation scopes. The inner operation remains the only code that mutates storage and records changed slots.
2. `CompleteMutation()` returns whether the scope owns standalone publication. For standalone use it establishes logical publication ownership and releases the physical monitor; when called inside an active transaction or an enclosing scope it returns false, leaving publication to the existing owner.
3. Disposing before completion releases the physical lock without retaining ownership. Disposing after standalone completion releases logical ownership and wakes waiters. Both paths remain synchronous and thread-affine.
4. Keep snapshots for operations that require multi-storage or multi-step rollback. A planned Shop stock replacement is applied as one `ReplaceState` call after planning, so the standalone publication scope can protect it without taking an additional rollback snapshot.
5. Domain owners continue consuming their existing trackers and publishing explicitly. No publisher or callback is added to transaction or boundary infrastructure.

## Risks / Trade-offs

- [Risk] Nested mutation scopes could be confused with transaction participation. → Permit nesting only while the current thread holds the boundary lock in an active mutation scope; the transaction binding continues to take precedence.
- [Risk] A domain could publish while a transaction is still active. → `CompleteMutation()` returns false for transaction-owned scopes; migrated callers publish only when it returns true.

## Migration Plan

Migrate only single-boundary operations whose storage change is atomic or staged before one replacement. Leave cross-container transfers, economy settlement, Equipment lifecycle completion, and other operations requiring rollback inside `ItemContainerTransaction`.
