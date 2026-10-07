# Tasks

## 1. Narrow MoneyPouch Participation

- [x] 1.1 Add the boundary-owned current-thread transaction query and use it to distinguish full, partial, and absent MoneyPouch ownership; verify existing partial/conflicting-scope regressions pass.
- [x] 1.2 Capture the inventory's single boundary at MoneyPouch construction and contribute exactly pouch plus inventory; replace artificial multi-boundary tests with a two-boundary enlistment/rollback test and verify focused MoneyPouch tests pass.
- [x] 1.3 Update the canonical item-container storage requirements to match the two-boundary and foreign-contention semantics; verify the delta and canonical requirement agree.

## 2. Integration Validation

- [x] 2.1 Run focused MoneyPouch, character item-transfer, and shop stock tests; verify the reported test counts and results.
- [x] 2.2 Build `Hagalaz.sln`, run `openspec validate --all --strict`, and run `git diff --check`; verify each command succeeds.
