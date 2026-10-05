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
