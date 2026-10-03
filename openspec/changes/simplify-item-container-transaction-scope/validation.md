# Validation

## Local results

- Abstractions: 292 passed, 0 failed.
- Game Scripts: 335 passed, 0 failed.
- GameWorld: 1,273 passed, 0 failed.
- Solution build: succeeded, 0 errors; existing compiler/analyzer/package warnings remain.
- Strict change validation and all four current behavior specifications: passed.
- Git diff whitespace check: passed.

Tests used positional `dotnet test <project> --no-restore` commands and ran serially. The solution built with `--no-restore -m:1 -p:BuildInParallel=false -p:UseSharedCompilation=false`. These are local results; hosted CI and a live gameplay client were not exercised.

## Acceptance evidence

| Criterion | Evidence |
| --- | --- |
| AC1: safe construction | Argument/contribution validation precedes locking; an item Count getter fault after both locks are acquired proves snapshot failure leaves unchanged references/counts, no bindings, no locks, and no publication. A fresh scope succeeds afterward. |
| AC2: rollback and ownership | Disposal restores all storage, including suppressed notifications and enumerator revisions. Mutation exceptions preserve original errors. Double disposal is inert. Dedicated-thread tests reject mutation, Commit and Dispose on another thread without ending the owner scope. |
| AC3: irreversible commit and cleanup | Commit marks storage irreversible before dropping snapshots, removing bindings and releasing locks. Hook/publisher assertions observe unbound/unlocked storage. Cleanup attempts every binding and reverse-order lock release before surfacing any mechanics failure; external completion is reached only after cleanup succeeds. |
| AC4: ordered publication | Tests distinguish first-seen publication order from MutationOrder, aggregate changed slots, publish once, reject Commit retry, and retain committed storage after first/later container or pouch failure. Shop regressions prove stock -> inventory -> pouch -> bought event and skipped later effects after failures. |
| AC5: equipment effects | Existing equipment lifecycle tests pass. A weapon/shield hook fault still attempts shield, profile and incoming hooks; simultaneous inventory publication failure retains both original exceptions and stack traces in AggregateException. The existing occupied non-weapon command path remains outside transaction locks and retains rejection/publication order; an active outer scope is rejected before invoking the interactive command. |
| AC6: complete participation | Exact transfers join complete scopes, reject partial or conflicting scopes before missing locks/mutations, and reject foreign active scopes. Composite aliases snapshot/publish each storage once. Real pouch tests reject partial inventory enlistment without a nested scope and publish inventory once before pouch effects. |
| AC7: existing domains and issue 347 | Pouch overflow/underflow, sentinel, bank, familiar, shop, duel, equipment and all trade regression suites pass. Terminal session state is assigned immediately before Commit under the existing gate, with cleanup in finally. Failed mutation attempts dispose before refund/recovery begins. |

No public mutation/staging context, receipts, committed-state queries, dynamic enlistment, separate publication call or unit-of-work abstraction remains. Domain operations continue to own item semantics. Begin/Commit/Dispose is the public transaction lifecycle.
