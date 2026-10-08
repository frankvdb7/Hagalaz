# Design

## Context

`ItemContainerMutationBoundary` already checks whether its owned storage belongs to an active transaction. `EquipmentContainer` needs the same check for both its own storage and inventory storage before running a custom unequip command that may open interactive UI.

## Goals / Non-Goals

**Goals:** Express the existing lifecycle guard through `IItemContainerMutationBoundary` and remove the production concrete cast.

**Non-Goals:** Change transaction state ownership, exception behavior, locking, mutation, or publication.

## Decisions

- Move and rename the existing guard as public `EnsureOutsideTransaction()` on the existing boundary interface.
- Keep the implementation as a direct check of `_storage.Transaction`; it throws the same `InvalidOperationException` and message.
- Call it through `_mutations` and `_owner.Inventory.Items.Mutations`. Do not add transaction state properties, interfaces, or helper frameworks.

## Risks / Trade-offs

- **Risk:** Exposing the guard adds a public interface member. **Mitigation:** It is a narrow semantic operation already required by a production domain caller; it exposes no transaction state.

## Migration Plan

Update the interface and implementation together, migrate the existing Equipment checks, run focused tests and the requested repository validations.
