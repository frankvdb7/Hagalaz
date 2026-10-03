# Validation

## Local results

- Abstractions: 292 passed, 0 failed.
- Game Scripts: 335 passed, 0 failed.
- GameWorld: 1,277 passed, 0 failed.
- Solution build: succeeded, 155 warnings and 0 errors; existing compiler/analyzer/package warnings remain.
- Strict change validation, all 73 active artifacts/current specifications and four archived changes: passed.
- Pinned jscpd 5.3.2 against PR #516 base `9c9f8622305d18ca05fa7099848a0cdb01cf09b6`: 2,288 files scanned, 502 historical clone pairs, zero new pairs.
- Git diff whitespace check: passed.

Focused tests used positional `dotnet test <project> --no-restore` commands and ran serially. The solution built with `--no-restore -m:1 -p:BuildInParallel=false -p:UseSharedCompilation=false`.

The exact CI command, `dotnet test --no-build --no-restore -p:TestingPlatformCommandLineArguments="--report-trx --coverage --coverage-output-format cobertura"`, completed locally with 3,159 passed, 14 failed and four skipped. All failures were five-second timeouts in authentication, sign-out and rate-limiter tests. The same complete scope with `-m:1` passed all 27 assemblies: 3,173 passed, zero failed and four skipped. GameWorld also passed independently. Solution-wide parallel test timeouts are recorded as a validation follow-up; no unrelated test or CI behavior was changed.

Hosted validation of the review fixes is pending. The earlier hosted build run ended with worker termination/cancellation and is not successful test evidence. A live gameplay client was not exercised.

## PR #516 review evidence

- `Begin_PouchWithUnsupportedInventoryRejectsBeforeLockingOrMutation` proves unsupported inventory participation throws exactly ArgumentException during contribution resolution, without locking/binding either original store, mutating coins or sending a message. A fresh scope succeeds after the owner returns to a supported inventory.
- `Begin_PouchIncludesEveryInventoryContributionAndRollsBackAllStorage` proves a pouch contributes its private store and both stores of a composite inventory; an extra inventory alias belongs to the same scope, and disposal restores all three stores.
- `Begin_PouchRejectsMissingInventoryContributionsBeforeLocking` covers null and empty contributions. `Commit_PouchCompositeAndInventoryAliasPublishInventoryOnceBeforePouch` proves normal contributions, alias deduplication and inventory -> message -> pouch ordering.
- Trade tests share only throwing-publisher setup and closed-escrow/session assertions. Completion, cancellation and recovery actions, terminal states, exception identity, recipient counts, repeated operations and conservation assertions remain explicit.
- Exception-safe `using` cleanup now covers the double-disposal test even if mutation/assertion fails. Rollback tests no longer bind unused scope variables.
- Complete base-to-head review retained Begin/Commit/Dispose, pre-lock argument resolution, ordered acquisition, snapshot-before-binding, strict joining and thread ownership, irreversible commit, best-effort release before completion, domain-owned hook policy, first-failure publication and separate publication order. No additional production correctness defect or transaction abstraction was found.

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
