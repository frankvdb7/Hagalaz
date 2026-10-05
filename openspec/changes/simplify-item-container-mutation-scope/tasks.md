# Tasks

- [x] Add `MutationScope` and remove `TryDeferChanges`.
- [x] Remove self-locking from ordinary `ItemContainerStorage` mutators while retaining lock assertions; preserve transfer and rollback synchronization.
- [x] Migrate ordinary `ItemContainer` mutations and remove `ExecuteUnderMutationLock`.
- [x] Migrate Shop and Equipment while preserving ordering and publication ownership.
- [x] Update MoneyPouch and transfer attribution to record directly into their known transaction.
- [x] Audit hydration, restoration, and every direct storage-mutator caller.
- [x] Add focused tests for lock ownership, attribution, lock lifetime, and publication ordering; retain existing transaction/concurrency regressions.
- [x] Update canonical OpenSpec behavior and validate the change.
- [x] Run targeted tests, solution build, full serial suite, duplication gate, and diff checks; report local-only status without committing or pushing.
- [x] Remove rollback restoration's reentrant lock; require caller-owned transaction lock and cover the rejection path with a focused test.
- [x] Preserve and test lifecycle-before-publication ordering for the standalone custom Equipment replacement path.
- [x] Retain standalone storage ownership through post-unlock lifecycle and publication; make Begin wait/reject with deterministic prefix release and cleanup notification.
- [x] Reject transaction-owned interactive Equipment replacement before its first mutation and validate exact RecordChanges storage/lock ownership.
- [x] Add ownership, failure, interruption, Equipment, and RecordChanges regressions; reconcile active OpenSpec deltas and run requested local validation.
