## Context

`BaseItemContainer` currently checks a range with a lightweight counts array and then applies the range through `ApplyAddRange`. The check does not represent newly allocated slots or non-stackable quantities, so the apply phase can fail after mutating earlier items. See `proposal.md` and the atomic insertion scenarios for the required behavior.

## Goals / Non-Goals

**Goals:**

- Make the capacity check use the same insertion rules as the real range operation.
- Validate the complete range against an isolated temporary item-slot representation before changing the real `Items` array.
- Keep existing slot selection, stacking, notifications, revisions, and derived-container ownership unchanged.

**Non-Goals:**

- No generic transaction or mutation-plan abstraction.
- No new lock, async API, transfer coordinator, or changes to single-item add/remove contracts.
- No money-pouch, shop, or cross-container transfer work.

## Decisions

1. **Reuse the existing range application routine for validation.** The insertion rules already live in `ApplyAddRange`. It will operate on an explicit slot array so validation and the real operation share the same stacking, overflow, and slot-allocation branches. A separate count-only algorithm is rejected because it already diverged from insertion behavior.

2. **Validate with cloned slot and incoming item objects.** Existing slots and incoming items will be copied with their current counts before the shared apply routine runs. This lets the simulation model stacks created by earlier incoming items without changing real item instances or invoking update callbacks. The real operation continues to use the existing item instances and clone behavior for non-stackable slots.

3. **Keep the existing notification owner.** `AddRange` and `TradeItemContainer` continue to notify and advance revisions only after the shared real-container apply succeeds. The simulation produces no notifications and does not advance the revision.

4. **Retain the virtual free-slot boundary for real containers.** The shared routine will resolve free slots through the existing virtual method for the real `Items` array, while the isolated validation array uses the equivalent first-null-slot lookup. No derived container contract changes are needed.

## Risks / Trade-offs

- [Risk] Cloning items for validation adds temporary allocations proportional to container capacity and range size. → Validation is local, synchronous, and bounded by the existing container/range sizes; it avoids a second persistent state owner and is limited to the atomicity boundary.
- [Risk] A caller could mutate the container concurrently between validation and application. → This change preserves the current container synchronization boundaries; trade containers already serialize their public operations. Adding a new locking model is outside issue #434.

## Migration Plan

No data or deployment migration is required. Existing callers use the same `HasSpaceForRange` and `AddRange` contracts.
