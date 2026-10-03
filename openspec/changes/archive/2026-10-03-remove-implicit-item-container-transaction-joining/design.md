# Design

## Context

See proposal.md for the motivation and specs/item-container-storage/spec.md for the behavior contract. The existing `Begin` API already resolves participants, deduplicates storage, locks deterministically, snapshots and rejects nested scopes. The hidden `BeginIfNeeded` path and several naked cross-storage transfers are the remaining ownership ambiguity.

## Goals / Non-Goals

**Goals:** Remove nullable transaction ownership and make every cross-storage mutation occur under an explicitly owned scope. Preserve existing item semantics, rollback, deferred completion and observable ordering.

**Non-Goals:** Redesign the transaction lifecycle, add ambient transaction lookup APIs, nested scopes, savepoints, or generic transaction/result abstractions.

## Decisions

- Keep `ItemContainerTransaction.Begin` as the only transaction creation API. It always returns a non-null caller-owned scope.
- Make mutation-boundary transfer a participation primitive: both boundaries must already be bound to the same active scope before storage changes.
- Keep standalone domain entry points responsible for `Begin` and `Commit`. Add narrowly named money-pouch exact mutation methods for cross-assembly callers that already own the full pouch/inventory scope; these methods mutate only and never commit.
- Multi-storage workflows continue to enlist all participants once at their existing owner and invoke participating primitives. Expected capacity rejection remains a bool result; lifecycle misuse throws.
- Remove tests that depend on implicit joining and replace them with strict scope misuse and single-owner workflow cases.

## Risks / Trade-offs

- **Missed transaction owner in a transfer caller** → audit every `TryTransferTo` callsite and run the abstractions, GameWorld, and Scripts suites.
- **Composable pouch API called without its owner scope** → use storage access checks to fail loudly and document its scope precondition.
- **Failure messages occur before rollback** → preserve existing explicit dispose-before-message behavior at owning operations.

## Migration Plan

Convert each standalone transfer caller to begin with source and destination participants. For already-scoped trade, duel, shop, bank, and pouch flows, replace standalone pouch operations with transaction-participating exact operations. Remove `BeginIfNeeded`, update focused tests and OpenSpec, then validate the requested suites and solution build.
