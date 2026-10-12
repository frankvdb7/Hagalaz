# Tasks

## 1. Equipment identity and placement

- [x] 1.1 Revalidate captured weapon/shield replacement identities inside the existing transaction; verify with stale incoming, stale weapon, stale shield, and successful replacement tests.
- [x] 1.2 Validate the exact unequipped instance and choose preferred/fallback inventory placement inside the transaction; verify stale, preferred-slot, occupied-slot fallback, and normal success tests.

## 2. Committed publication attempts

- [x] 2.1 Attempt standalone before-publication, source, and destination stages independently; verify single/multiple failures, committed storage, lock release, order, and no replay in `ItemContainerTransferTests`.
- [x] 2.2 Attempt each transactional boundary and completion owner independently; verify MoneyPouch failure cases and Equipment hook plus publisher failure cases preserve committed state and all applicable exceptions.

## 3. Contract and integration validation

- [x] 3.1 Update canonical and active item-container specs; verify no active item-container delta contradicts the attempt-all policy and strict OpenSpec validation passes.
- [x] 3.2 Run targeted Abstractions, GameWorld, and Scripts tests, solution build/tests, the existing duplication gate, and `git diff --check`; targeted suites, build, OpenSpec, duplication, and diff checks pass. Full solution tests were run but Docker-backed integration tests fail because Docker is unavailable in this environment.
