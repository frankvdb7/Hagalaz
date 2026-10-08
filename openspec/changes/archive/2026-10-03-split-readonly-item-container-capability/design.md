# Design

## Context

See `proposal.md` for motivation. `IContainer<T>` already supplies fixed-capacity indexed and enumerable access. `ItemContainer` implements the item queries through `ItemContainerStorage`; Equipment owns the same storage type directly to preserve equipment-specific mutation rules.

## Goals / Non-Goals

**Goals:** Provide one read-only item query contract shared by mutable ordinary containers and Equipment. Preserve Equipment domain operations and keep its mutable storage boundary private.

**Non-Goals:** Change transaction, locking, publication, rollback, MoneyPouch, or other domain-container APIs.

## Decisions

- Add `IReadOnlyItemContainer : IContainer<IItem?>` with the existing item query surface: free/taken slots, ID and count queries, instance/slot lookup, containment, and space checks. `StorageType` and `Mutations` remain outside it.
- Make `IItemContainer` inherit the read-only contract and declare only its mutation-specific surface plus `Type`.
- Make `IEquipmentContainer` compose `IReadOnlyItemContainer Items`. Retain the `EquipmentSlot` indexer and domain operations, including its equipment-slot instance lookup.
- Add one internal `ReadOnlyItemContainer` projection over `ItemContainerStorage`. Equipment exposes this as `Items`; it does not replace its storage or implement a generic mutable interface. `ItemContainer` itself already supplies the same read-only members.
- Migrate generic reads to `.Equipment.Items`; leave `EquipmentSlot` access and equipment mutations on their existing domain methods.
- Update the current `item-container-storage` requirement because it currently requires Equipment to implement `IContainer<IItem?>`; add a focused requirement for the read-only capability shape.

## Risks / Trade-offs

- [Public source compatibility for callers using generic Equipment members] → The interface changes are intentional; migrate repository callers and prove the new interface shape in tests.
- [The projection could drift from storage behavior] → Keep it as direct forwarding to `ItemContainerStorage` and test indexed/enumerable reads and representative queries against Equipment.
