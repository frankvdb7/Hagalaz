# Design

## Context

See proposal.md for the motivation and scope. `ItemContainerMutationBoundary` already accepts the fixed internal completion owner, and `ItemContainer` already stores the boundary plus the boundary's `ItemContainerStorage`. GameWorld is an existing friend assembly of Game Abstractions, so its container implementations can construct these internal types without adding API or friend-assembly access.

## Goals / Non-Goals

**Goals:** Make the boundary the explicit completion-owner association while keeping ItemContainer's generic operations and public construction ergonomics.

**Non-Goals:** Change storage identity, transaction participation, lock ordering, completion ordering, publication, or any gameplay operation.

## Decisions

- Replace the two internal completion-owner overloads with a single `ItemContainer(ItemContainerMutationBoundary)` constructor. It validates the boundary, stores that exact instance, and reads storage from that boundary. It must not create another boundary.
- Keep the existing public constructors unchanged. Each creates the same `ItemContainerStorage` with the same arguments, creates one ownerless boundary, and delegates to the composed constructor. This preserves normal generic construction without a factory.
- Each domain container that currently passes itself as completion owner creates one storage and one boundary with itself as owner, then wraps that boundary. It retains only its existing `_items` facade field. Shop Stock retains initial items, capacity, type, and reset-to-zero behavior.
- Adapt the owner-aware test helper/factory to construct one storage, one owner-bound boundary, and one ItemContainer. Existing behavior assertions remain the proof of publication and transaction semantics.
- Keep Equipment and MoneyPouch as they are; keep transaction, mutation-boundary, storage, and change-tracker algorithms unchanged.

The rejected alternatives are mutable completion-owner registration or another factory/helper. Both add a lifecycle or abstraction where direct construction already expresses the ownership relationship.

## Risks / Trade-offs

- A migration could accidentally create a second boundary or lose a completion owner. Mitigation: review each construction site and verify the boundary is constructed once and passed unchanged.
- Initial storage arguments could drift, especially Shop Stock's reset count. Mitigation: preserve each prior constructor argument exactly and run the existing GameWorld suites.

## Migration Plan

No data or deployment migration is needed. The refactor is source-compatible for public constructors and changes only internal construction. Reverting the commit restores the prior constructors and call sites.
