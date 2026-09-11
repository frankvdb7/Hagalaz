## Context

See `proposal.md` for motivation. The current branch already assigns map-region residency transitions to `MapRegionService`, tick phase ordering to `GameWorkerService`, and pending-abort ownership to `GameSessionStore`, but several lower-level implementations still duplicate those responsibilities.

## Goals / Non-Goals

**Goals:**

- Keep one authoritative owner for each lifecycle or shared-state transition.
- Preserve exact-instance, generation, cancellation, and retry semantics.
- Make failed callbacks recoverable on the next valid game tick.
- Keep external enumeration safe without exposing live mutable dictionaries.

**Non-Goals:**

- No new lifecycle framework, state machine, lock registry, queue, worker, or retry mechanism.
- No change to distributed session fencing, public sync/async gameplay APIs, scheduler ownership, or `MapRegionPart` update-buffer synchronization.
- No consolidation of independent character persistence/logout records without evidence that they represent the same transition.

## Decisions

1. **Remove creature phase state.** `GameWorkerService` already sequences major update, prepare, update, and reset. Creature methods will use `IsDestroyed` as the only lifecycle guard and otherwise execute their phase directly. Character rendering will no longer call `TryBeginClientUpdate`; the region/worker call path already admits it once per phase.

2. **Use ordinary residency dictionaries under the existing residency owner lock.** `Dimension` will keep private ordinary dictionaries and use the same residency synchronization boundary for service mutations and snapshot creation. `MapRegionService` remains the only production owner of compound transitions. A fresh read-only dictionary snapshot prevents enumeration from racing with mutation.

3. **Use one lock in `ContactSessionStore`.** Generation replacement, exact removal, lookup, and enumerator snapshots will all execute through the store lock. The store will not expose a live dictionary enumerator.

4. **Do not tidy a dead region internally.** `MapRegion.DestroyAsync()` will snapshot externally owned NPCs/items/objects, mark the region terminal, run the required external cleanup, and leave dead internal collections untouched. Ordinary removal methods and `MapRegionPart.RemoveDestroyed` are active-region semantics and are therefore deleted from the teardown path.

5. **Trust the pending-abort reservation.** `GameSessionStore` already reserves the connection slot while `PendingAbort` exists. Removing the coordinator's second lookup makes the store the single owner of replacement protection; the coordinator retains processing-marker release and completion handling.

6. **Use direct exception rethrow in NpcService.** Registration rollback catches retain their current cleanup and logging behavior but use `throw;` where no exception transformation is needed. Character persistence markers and `MapRegionPart._updatesLock` remain because they protect independent state owned by those components.

## Risks / Trade-offs

- [Risk] A live snapshot can be briefly stale after a residency transition. → This is already the correct read contract for enumeration; exact mutations still run under the owner lock and canonical lookup remains current.
- [Risk] Dead-region internal collections retain references until the region is collected. → The region is removed from residency before teardown and no longer serves active gameplay; avoiding active removal semantics prevents respawn, update, or collision side effects.
- [Risk] Removing phase admission exposes callers that bypass the game worker. → Production tracing shows the worker/region path owns all phase calls; tests will cover failure recovery and preserve the worker ordering contract.

