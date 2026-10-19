# Tasks

## 1. Regression Coverage

- [x] 1.1 Add a trade regression where an accepted offer mutation commits, Inventory publication throws, and the stale acceptance cannot complete the trade; verify it fails before the fix.
- [x] 1.2 Verify a rolled-back offer mutation leaves its externally observed revision unchanged.

## 2. Acceptance Revision and Failure Handling

- [x] 2.1 Expose the synchronized item-storage revision through `ItemContainerChangeTracker` and use it for `TradeContainer.Revision`; verify committed mutations advance it and rollback restores it.
- [x] 2.2 Clear both trade acceptances when a session-gated operation throws after a committed offer revision change, then preserve the original exception; verify the publication-failure regression passes.

## 3. Contract and Validation

- [x] 3.1 Sync the canonical trading-completion and item-container-storage specs with publication-independent offer acceptance and verify `openspec validate --all --strict` passes.
- [x] 3.2 Run focused trade tests and `git diff --check`; inspect the final cumulative diff for scope.
