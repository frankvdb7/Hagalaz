## Context

See proposal.md for the reported behavior. `NpcCombat.OnDeath` currently schedules permanent removal as a delayed synchronous task whose callback calls `UnregisterAsync(...).Wait()`. The existing `RsAsyncTask` and creature task extensions already provide cooperative, tick-driven asynchronous execution.

## Goals / Non-Goals

**Goals:**

- Keep permanent NPC removal off the blocking path of a game tick.
- Retain the existing delay and service ownership for destruction and store removal.
- Cover the pending-operation case deterministically.

**Non-Goals:**

- Redesign the task scheduler or NPC service.
- Change combat, loot, respawn timing, or client protocol behavior.
- Sweep unrelated synchronous database or service calls in other gameplay features.

## Decisions

Use the existing delayed `RsTask` only as a timing gate for permanent death cleanup, then enqueue the existing `RsAsyncTask` through the creature's task scheduler. The respawn fallback uses the same asynchronous task directly because its timing gate already exists in the respawn task. This preserves the NPC task owner and current timing while allowing the asynchronous unregister operation to yield between ticks.

The simpler direct `.Wait()` is rejected because it owns the game-loop thread until the store operation completes. Adding a new worker or queue is rejected because `RsAsyncTask` already owns asynchronous task progression in this runtime.

The NPC service remains the authoritative owner of destruction and store removal. No second cleanup or retry owner is introduced.

## Risks / Trade-offs

- [Risk] The NPC can remain in the store briefly while removal is pending. → Mitigation: preserve the existing scheduled delay and let the existing asynchronous operation finish under the same creature task owner.
- [Risk] A failed asynchronous removal is surfaced by the existing task scheduler logging path. → Mitigation: do not hide or duplicate error handling; keep the established `RsAsyncTask` behavior.
