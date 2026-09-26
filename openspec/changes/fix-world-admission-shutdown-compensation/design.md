## Context

See `proposal.md` for the failure being corrected. The admission service already
owns the reserve/initialize/commit/compensate sequence. Normal registered
character removal must stay on the GameWorker because character stores and
world objects are normally mutated at that boundary. `GameWorkerService` can
expose its actual execution completion, and
`IGameSessionService.RemoveSession` already owns exact-owner local and
distributed cleanup.

## Goals / Non-Goals

**Goals:**

- Complete a registered-character admission rollback only after no future
  GameWorker tick can occur.
- Dispose the partially admitted character's independently created scope.
- Preserve the normal worker rollback and the existing exact-owner recovery
  semantics.
- Make the worker-versus-terminal cleanup race single-owner.

**Non-Goals:**

- Changing `IRsTaskService` or `RsTaskService`.
- Adding scheduler lifecycle queues, task categories, post-stop worker work, or
  a generic lifecycle coordinator.
- Moving normal gameplay cleanup off the GameWorker.
- Reworking session lease or persistence ownership.

## Decisions

1. **Use the actual worker completion boundary.** `GameWorkerService` exposes
   one completion task that is completed when the inherited `ExecuteTask` has
   actually terminated. Admission observes that task rather than
   `ApplicationStopped`, because host shutdown can time out while the worker
   execution task is still active.

2. **Keep one cleanup owner.** The scheduled rollback and terminal fallback
   share a local atomic ownership gate. Whichever path claims it performs
   character removal, destruction, and persistence release; the other path is
   a no-op and waits for the result. This prevents double removal or double
   disposal when shutdown races a worker tick.

3. **Reuse the existing cleanup operation.** Terminal cleanup calls the same
   exact character removal and destruction sequence as normal rollback, then
   calls `RemoveSession`. That service remains the authoritative owner of local
   session removal and distributed claim release, including retention for
   retry when infrastructure is unavailable.

4. **Preserve the character's scope disposal.** Admission has not called
   `OnRegistered`, so the character has not entered region/presence publication;
   after actual worker completion, `Destroy()` can safely perform final unlinking,
   callbacks, and disposal without a concurrent worker tick.

Rejected alternatives:

- Cancelling the rollback await would abandon the character, persistence, and
  session ownership without compensation.
- Draining or extending the scheduler would expand a shared lifecycle API for a
  single admission owner and would make process shutdown depend on new worker
  semantics.
- Mutating the character from `ApplicationStopped` can race a worker that
  exceeded the host shutdown timeout.

## Risks / Trade-offs

- **[Risk]** A host killed before worker execution completes cannot execute in-process
  compensation. → The existing exact-owner session/persistence recovery paths
  remain authoritative, and the normal path is unchanged.
- **[Risk]** External infrastructure may already be unavailable at the terminal
  signal. → `RemoveSession` retains recoverable state according to its existing
  contract; persistence release remains generation-checked and observable.
- **[Trade-off]** The admission request can remain pending until actual worker
  execution terminates. → This avoids off-worker mutation even when host
  shutdown has already timed out.

## Migration Plan

No data migration or deployment ordering is required. Deploy the admission
service change with the focused regression tests. Reverting the code restores
the pre-fix behavior and does not require schema changes.

## Open Questions

None.
