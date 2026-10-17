# Tasks

## 1. Synchronization ownership

- [x] 1.1 Implement `ItemContainerSynchronization` as the sole per-storage access authority and verify existing ownership tests pass.
- [x] 1.2 Refocus `ItemContainerLockSet` on ordered physical acquisition coordinated through synchronization owners; verify partial-acquisition and opposite-transfer tests pass.
- [x] 1.3 Extract `ItemContainerWriteScope` and migrate standalone and nested writes; verify standalone publication and no-op behavior tests pass.

## 2. Transaction, tracking, and transfer composition

- [x] 2.1 Migrate transaction participant resolution, snapshots, binding, commit, rollback, and finalization to synchronization owners; verify transaction lifecycle and rollback tests pass.
- [x] 2.2 Keep tracker batches in `ItemContainerChangeTracker` and route synchronized reads through the owner; verify committed/provisional reader tests pass.
- [x] 2.3 Move atomic transfer orchestration to one shared container-level path and verify transactional and standalone transfer tests pass.
- [x] 2.4 Migrate Equipment, MoneyPouch, Inventory, Bank, Rewards, Familiar Inventory, and Shop Stock composition and verify their focused tests pass.
- [x] 2.5 Remove `ItemContainerMutationBoundary` and all obsolete APIs; verify no source or test references remain.

## 3. Regression coverage and validation

- [x] 3.1 Update existing synchronization tests and add any missing deterministic regressions from the approved requirements; verify all three requested test projects pass.
- [x] 3.2 Run solution build, compare compiler/analyzer warnings to baseline, run jscpd, validate all OpenSpec changes, and run `git diff --check`.
- [x] 3.3 Perform the required adversarial ownership, behavior, and complexity review; archive this change only after its acceptance criteria pass and canonical specs are synchronized.
