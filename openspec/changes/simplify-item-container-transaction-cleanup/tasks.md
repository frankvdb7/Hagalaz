# Tasks

## 1. Cleanup Behavior

- [x] 1.1 Simplify construction and rollback cleanup; verify snapshot-failure and rollback regressions pass with no construction/rollback waiter pulse.
- [x] 1.2 Remove synchronization failure aggregation from commit cleanup while retaining domain aggregation; verify interrupted teardown, hook, and publication regressions pass.
- [x] 1.3 Preserve the first standalone publication failure through interrupted cleanup; verify publication ownership is cleared and the publisher exception propagates directly.
- [x] 1.4 Update the canonical `item-container-storage` requirements; verify the change delta matches those requirements.

## 2. Integration Validation

- [x] 2.1 Run focused Abstractions transaction and GameWorld Equipment/MoneyPouch tests; verify their reported results.
- [x] 2.2 Build `Hagalaz.sln` and run `openspec validate --all --strict`; verify both commands succeed.
- [x] 2.3 Run `git diff --check` and inspect the cumulative local diff; verify there are no whitespace errors or unrelated changes.
