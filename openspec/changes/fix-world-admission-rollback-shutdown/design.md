## Context

World admission owns compensation until the registered Character has been
removed. Character removal and destruction remain GameWorker-owned because
they mutate the exact Character and its lifecycle hooks. The shared scheduler
currently has no lifecycle boundary, so a queued rollback can outlive its
executor.

## Goals / Non-Goals

**Goals:**

- Make lifecycle-critical rollback scheduling and shutdown atomic enough that
  accepted rollback work cannot become orphaned.
- Execute only explicitly marked rollback actions during the shutdown drain.
- Keep normal rollback on the ordinary GameWorker tick path while healthy.
- Preserve the existing cleanup order and exact-owner checks.

**Non-Goals:**

- Do not drain ordinary gameplay tasks.
- Do not change logout, map-region, archive-decoder, claim identifiers, or
  persistence serialization.
- Do not add a durable compensation framework or a second worker.

## Decisions

1. Extend the existing `IRsTaskService` with three lifecycle operations:
   lifecycle-critical scheduling, shutdown commencement, and shutdown
   completion. The scheduler remains the single owner of task acceptance and
   execution state.

2. Represent lifecycle-critical work as a one-shot action tracked separately
   from ordinary queued tasks. While the worker is running or stopping, the
   action is queued for a normal tick or the final shutdown drain. The
   scheduler transitions to stopped only after repeatedly draining actions that
   were accepted during the drain, closing the enqueue/drain race.

3. If an action is submitted after the scheduler is stopped, the scheduler
   executes that marked lifecycle action synchronously under its serialized
   terminal handoff. This is safe for this boundary because the stopped state
   is published only after the GameWorker execution task has completed and the
   action is limited to admission compensation. It avoids abandoning exact
   Character ownership without running arbitrary gameplay work.

4. `WorldSessionAdmissionService` uses the lifecycle-critical operation only
   for registered-character removal. Persistence release and session removal
   remain after successful exact removal, so shutdown does not weaken the
   existing ownership ordering.

5. `GameWorkerService` begins scheduler shutdown before cancelling its loop and
   completes the scheduler shutdown from the worker's final execution path.
   The explicit post-stop completion path covers a worker that was never
   started or has already completed.

## Risks / Trade-offs

- [Risk] A lifecycle callback can block shutdown just as the previous rollback
  wait could block admission. → Mitigation: only the bounded, existing
  registered-character compensation action is admitted to this lane; no
  ordinary queued work is drained.
- [Risk] A lifecycle callback can schedule another lifecycle callback during
  shutdown. → Mitigation: the drain loops over newly accepted lifecycle work
  before publishing the stopped state.
- [Risk] A post-stop callback runs outside the normal tick loop. → Mitigation:
  the scheduler publishes stopped only after the worker loop is complete and
  serializes this terminal handoff; the admission character is not published
  as a committed gameplay session.

## Migration Plan

Deploy the scheduler contract, GameWorker boundary, admission rollback, and
tests together. No data migration is required. A source rollback restores the
previous scheduler and admission behavior.
