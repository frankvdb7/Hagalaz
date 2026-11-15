# Proposal

## Why

`DegradeTask` (corrupted equipment dust countdown) stops and saves its remaining ticks at the wrong lifecycle points (#521):

- It saves on `CreatureDestroyedEvent`, which fires in `Character.Destroy()` **after** `CharacterLogoutService` has already dehydrated the final snapshot, so the persisted `ExtraData[0]` is stale and lifetime is restored on login.
- On `EquipmentChangedEvent` it saves only when a changed slot **still contains** the tracked item. The initial equip publication matches and stops the countdown immediately; a real unequip or replacement leaves the slot without the item, so the task keeps ticking and degrades an item that is no longer equipped.

**Goal:** the countdown runs once while the item is equipped, is saved on the exact item when it departs or when the character detaches, and cancels exactly once.

## What Changes

- Add `CharacterDetachingEvent`, sent synchronously by `Character.OnDetach()` before script detach callbacks, i.e. inside the existing pre-snapshot boundary in `CharacterLogoutService`.
- `DegradeTask` saves remaining ticks on `CharacterDetachingEvent`, and on `EquipmentChangedEvent` only when the tracked instance is no longer equipped.
- `DegradeTask.Save` cancels directly (idempotent) instead of queueing a delayed cancel task. `CreatureDestroyedEvent` remains only for cleanup (unregister and cancel), not persistence.

## Non-goals

Trade/duel or item-container refactors, a new timer service or queue, generalized lifecycle coordination, reordering `CreatureDestroyedEvent`, `DegradingEquipment` per-attack charges.

## Acceptance criteria

- A newly equipped item keeps ticking after the initial equipment publication and after unrelated slot changes.
- Unequip or replacement stores remaining ticks on that exact item and cancels the task once; re-equip resumes from the remainder.
- Detach writes remaining ticks before dehydration; the pre-snapshot event is raised by `OnDetach`.
- Expiry removes the equipped item once; a cancelled task never removes a replacement.

## Stop conditions

Stop if the fix needs a new scheduler, queue, or reordering of destruction events.
