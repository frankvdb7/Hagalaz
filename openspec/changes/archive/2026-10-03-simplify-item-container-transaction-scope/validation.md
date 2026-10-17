# Validation

## Previous review results before the contention correction

- Abstractions: 292 passed, 0 failed.
- Game Scripts: 335 passed, 0 failed.
- GameWorld: 1,277 passed, 0 failed.
- Solution build: succeeded, 155 warnings and 0 errors; existing compiler/analyzer/package warnings remain.
- Strict change validation, all 73 active artifacts/current specifications and four archived changes: passed.
- Pinned jscpd 5.3.2 against PR #516 base `9c9f8622305d18ca05fa7099848a0cdb01cf09b6`: 2,288 files scanned, 502 historical clone pairs, zero new pairs.
- Git diff whitespace check: passed.

Focused tests used positional `dotnet test <project> --no-restore` commands and ran serially. The solution built with `--no-restore -m:1 -p:BuildInParallel=false -p:UseSharedCompilation=false`.

The exact CI command, `dotnet test --no-build --no-restore -p:TestingPlatformCommandLineArguments="--report-trx --coverage --coverage-output-format cobertura"`, completed locally with 3,159 passed, 14 failed and four skipped. All failures were five-second timeouts in authentication, sign-out and rate-limiter tests. The same complete scope with `-m:1` passed all 27 assemblies: 3,173 passed, zero failed and four skipped. GameWorld also passed independently. Solution-wide parallel test timeouts are recorded as a validation follow-up; no unrelated test or CI behavior was changed.

The review implementation commit `4ab268e634a34c9152c3067150d7fa3d1c18823a` passed [hosted CI run 37124157820](https://github.com/frankvdb7/Hagalaz/actions/runs/37124157820): the exact full CI test command completed all 27 assemblies with 3,173 passed, zero failed and four skipped; the build reported 541 warnings and zero errors. Duplication, OpenSpec and frontend checks also passed. The earlier hosted run that ended with worker termination/cancellation is not successful test evidence. A live gameplay client was not exercised.

[CodeQL run 37124157729](https://github.com/frankvdb7/Hagalaz/actions/runs/37124157729) passed C#, Actions and JavaScript/TypeScript analysis for the same implementation head. The PR merge analysis `ddbb0b391504fc37b992474a7c80ccaa3a9a8ef5` has seven informational C# notes, zero warning/error findings and zero analysis errors; Actions and JavaScript/TypeScript have zero findings. The four prior test lifetime/unused-variable warnings are fixed. Five broad-catch notes remain deliberately: transaction cleanup/restoration and owned hook completion must attempt subsequent work after arbitrary failures, and the thread test helper must report arbitrary assertion failures back to its owner. Two LINQ filtering suggestions remain informational; the direct loops preserve readable publication and recovery ordering. No alert suppression or baseline change was added.

The final documentation commit is checked separately on the PR before handoff; its results are reported in the handoff rather than recording a self-referential commit here.

## PR #516 review evidence

- `Begin_PouchWithUnsupportedInventoryRejectsBeforeLockingOrMutation` proves unsupported inventory participation throws exactly ArgumentException during contribution resolution, without locking/binding either original store, mutating coins or sending a message. A fresh scope succeeds after the owner returns to a supported inventory.
- `Begin_PouchIncludesEveryInventoryContributionAndRollsBackAllStorage` proves a pouch contributes its private store and both stores of a composite inventory; an extra inventory alias belongs to the same scope, and disposal restores all three stores.
- `Begin_PouchRejectsMissingInventoryContributionsBeforeLocking` covers null and empty contributions. `Commit_PouchCompositeAndInventoryAliasPublishInventoryOnceBeforePouch` proves normal contributions, alias deduplication and inventory -> message -> pouch ordering.
- Trade tests share only throwing-publisher setup and closed-escrow/session assertions. Completion, cancellation and recovery actions, terminal states, exception identity, recipient counts, repeated operations and conservation assertions remain explicit.
- Exception-safe `using` cleanup now covers the double-disposal test even if mutation/assertion fails. Rollback tests no longer bind unused scope variables.
- That review retained Begin/Commit/Dispose, pre-lock argument resolution, ordered acquisition, snapshot-before-binding, strict joining and thread ownership, irreversible commit, best-effort release before completion, domain-owned hook policy, first-failure publication and separate publication order. The later contention review below corrects its treatment of foreign bindings without changing the architecture.

## Acceptance evidence

| Criterion | Evidence |
| --- | --- |
| AC1: safe construction | Argument/contribution validation precedes locking; an item Count getter fault after both locks are acquired proves snapshot failure leaves unchanged references/counts, no bindings, no locks, and no publication. A fresh scope succeeds afterward. |
| AC2: rollback and ownership | Disposal restores all storage, including suppressed notifications and enumerator revisions. Mutation exceptions preserve original errors. Double disposal is inert. Dedicated-thread tests reject mutation, Commit and Dispose on another thread without ending the owner scope. |
| AC3: irreversible commit and cleanup | Commit marks storage irreversible before dropping snapshots, removing bindings and releasing locks. Hook/publisher assertions observe unbound/unlocked storage. Cleanup attempts every binding and reverse-order lock release before surfacing any mechanics failure; external completion is reached only after cleanup succeeds. |
| AC4: ordered publication | Tests distinguish first-seen publication order from MutationOrder, aggregate changed slots, publish once, reject Commit retry, and retain committed storage after first/later container or pouch failure. Shop regressions prove stock -> inventory -> pouch -> bought event and skipped later effects after failures. |
| AC5: equipment effects | Existing equipment lifecycle tests pass. A weapon/shield hook fault still attempts shield, profile and incoming hooks; simultaneous inventory publication failure retains both original exceptions and stack traces in AggregateException. The existing occupied non-weapon command path remains outside transaction locks and retains rejection/publication order; an active outer scope is rejected before invoking the interactive command. |
| AC6: complete participation | Exact transfers join complete current-thread scopes and reject partial or conflicting current-thread scopes before missing locks/mutations. Independent foreign owners serialize through deterministic locks. Composite aliases snapshot/publish each storage once. Real pouch tests reject partial inventory enlistment without a nested scope and publish inventory once before pouch effects. |
| AC7: existing domains and issue 347 | Pouch overflow/underflow, sentinel, bank, familiar, shop, duel, equipment and all trade regression suites pass. Terminal session state is assigned immediately before Commit under the existing gate, with cleanup in finally. Failed mutation attempts dispose before refund/recovery begins. |

No public mutation/staging context, receipts, committed-state queries, dynamic enlistment, separate publication call or unit-of-work abstraction remains. Domain operations continue to own item semantics. Begin/Commit/Dispose is the public transaction lifecycle.

## Contention review correction

The prior preflight rejected every binding, including a foreign owner that should simply hold the lock until completion. Begin now rejects only current-thread nesting before acquisition, then checks binding after acquiring each ordered lock. BeginIfNeeded finds current-thread owners only; absent one, it creates an independent scope and waits on deterministic locks. Joining still requires complete enlistment in one current-thread scope. The public marker, eager snapshots and synchronous owner-thread lifecycle are unchanged. XML docs now state unsynchronized-reader isolation limits and prohibit await/thread transfer.

- `Begin_IndependentContenderWaitsForSameOrOverlappingStorage` covers both same-storage and overlapping two-storage contention, released by commit and by disposal. Events coordinate entry, a bounded completion wait catches immediate rejection, and a bounded join catches deadlock. Each contender verifies sorted locks and its own binding. No sleeps or state polling are used.
- `TryTransfer_OppositeDirectionWaitsForForeignActiveScopeAndSucceedsAfterRelease` proves the internal standalone helper waits rather than joining a foreign scope; both transfers succeed and conserve items.
- Existing partial/conflicting-scope and explicit nested-Begin tests remain. `Scope_WrongThreadCommitDisposeAndMutationRejectWithoutEndingScope` retains wrong-thread lifecycle and direct mutation rejection; the former independent-helper rejection assertion was replaced with contention coverage.
- `Commit_MultipleHookFailuresAreFlatAndRetainOriginalExceptions` covers three hook failures with and without a publication failure. Equipment's owned multi-hook batch is also tested with three lifecycle failures plus publication. Aggregation retains original leaf exceptions, hook order, stack traces, committed storage and no retry.
- Bank pouch deposit, pouch withdrawal, equipment unequip and weapon/shield capacity failures cover owned and joined invocation. Owned failure messages assert unbound/unlocked storage; joined failures send no message, preserve the outer binding and restore mutations when the outer scope disposes. The design records existing script/preflight UI effects that remain outside this correction.
- Construction and rollback pass original failures into the existing best-effort release error list. All bindings and locks are attempted before errors propagate. One original failure uses ExceptionDispatchInfo; independent primary and cleanup failures use the same flat aggregation. Correct-owner field assignment and Monitor.Exit remain normally nonthrowing; no artificial recovery or cleanup injection API was introduced.

Local validation: 33 focused transaction cases and 66 focused domain cases passed. Full suites passed 298 Abstractions, 1,282 GameWorld and 335 Scripts tests. Solution build succeeded with 73 warnings and zero errors. The exact CI command completed all 27 assemblies with 3,184 passed, zero failed and four skipped. Strict OpenSpec passed 73 active items and four archives. Pinned jscpd against the actual PR base scanned 2,288 C# files with 502 historical pairs and zero new pairs. Diff whitespace validation passed. Hosted CI and CodeQL are checked separately on the pushed correction before handoff. A live gameplay client was not exercised.

The correction commit `63483aef150373db4b49d367f89f2734e2b3735d` passed [hosted CI 37131853934](https://github.com/frankvdb7/Hagalaz/actions/runs/37131853934). The exact full test command completed 27 assemblies with 3,184 passed, zero failed and four skipped; build had 541 warnings and zero errors. Frontend, OpenSpec, duplication and dependency checks passed.

[CodeQL 37131853870](https://github.com/frankvdb7/Hagalaz/actions/runs/37131853870) passed all three languages. C# merge analysis `79751d0aa7cbebcc2f23370017502179e02fc70c` has ten informational notes, zero warning/error findings and no analysis error; Actions and JavaScript/TypeScript have zero findings. Seven broad catches are intentional best-effort restoration/release, hook/publication error retention or test-thread error transport. Two direct-loop filtering suggestions are retained for readability. The final note suggests using for the contention test's owner, which already has a using declaration; its explicit Dispose in finally deliberately releases the owner before joining the contender and tests idempotent disposal. The three new review threads were reviewed together against the transaction invariants and resolved without suppression or compensating code. No merge-blocking correctness finding remains. The documentation-only completion commit is checked separately before handoff.

## Domain-neutral after-publication refactor

`_pouchNotifications` and `DeferPouchNotification` are replaced by `_afterPublicationActions` and internal `DeferAfterPublication`, including the existing mutation-boundary gateway. The neutral names state when the actions execute. `_hooks` becomes `_postCommitHooks` to clarify irreversible storage; DeferBeforePublication keeps its existing name. No public API, abstraction, collection ownership or control flow changes. MoneyPouch's deferred callback body still owns previous count, change amount, message, event and their order.

- `Commit_PublishesOnceAfterUnlockAndMakesStoragePermanent` now explicitly asserts hook, both containers in participant order, then both registered actions. Actions observe unbound/unlocked storage and permanent mutations; repeated disposal/commit cannot republish.
- `Commit_LaterContainerFailurePreservesEarlierPublicationAndSkipsLaterContainersAndActions` proves container 1 publishes, container 2 throws, container 3 and both after-publication actions are skipped while all storage stays permanent.
- `Commit_AfterPublicationFailureStopsLaterActionsAfterContainerPublication` proves containers publish, action 1 succeeds, action 2 throws its original exception, action 3 is skipped, and storage cannot roll back or retry completion.
- Existing hook/flat-aggregation, ownership/contention and real MoneyPouch inventory-before-message/event and failure-skipping assertions remain. Trade pouch fixtures use the renamed internal gateway; domain assertion bodies remain unchanged.

Local validation passed 32 focused transaction cases, 298 Abstractions, 1,282 GameWorld and 335 Scripts tests. The exact full CI test command passed all 27 assemblies with 3,184 passed, zero failed and four skipped. Solution build: 73 warnings, zero errors. Strict OpenSpec: 73 active items and four archives passed. Pinned jscpd against the actual PR base: 2,288 C# files, 502 historical pairs and zero new pairs. Diff whitespace checks passed; repository searches find no old registration/list name and no pouch terminology in ItemContainerTransaction code or XML docs. Hosted results for the pushed refactor head are reported in the PR description and final handoff, so recording them does not create an additional head to validate.

## Fixed owner completion review

The transaction and mutation boundary no longer register or store arbitrary deferred callbacks. A fixed internal owner contract provides discard, before-publication and after-publication completion. Equipment keeps ordered item/profile facts in owned batches; MoneyPouch keeps previous-count/change-count facts. Scope-keyed BCL collections isolate pending facts across post-unlock reentrant or contending scopes. One integer ordinal in the transaction preserves interleaved mutation order without retaining executable work. The small boundary scan deliberately avoids a generic completion scheduler.

| Requirement | Regression evidence |
| --- | --- |
| No executable transaction fields | Transaction_StoresNoExecutableCallbacksOrCallbackCollections |
| Rollback clears equipment and pouch state; later scope has no stale effects | Dispose_EquipmentAndPouchEffectsAreDiscardedWithoutLeakingIntoLaterScope |
| Multiple equipment operations and aliases complete once before publication | Commit_MultipleEquipmentMutationsCompleteOnceBeforePublicationDespiteAliases |
| Equipment ordering, attempt-all, original flat failures | EquipItem_HookAndPublisherFailuresPreserveBothAfterAttemptingOwnedHooks |
| Interleaved pouch data retains previous-count/message semantics independently of participant order | Commit_MultiplePouchChangesKeepMutationOrderAcrossOwnersAndAliases |
| Inventory before pouch message/event; aliases publish once | Commit_PouchCompositeAndInventoryAliasPublishInventoryOnceBeforePouch |
| Container failure skips/discards pouch completion, including later scope | TryRemoveExact_WhenInventoryPublisherThrows_KeepsCommittedStorageAndSkipsPouchPublication |
| Pouch failure skips later changes without retry or stale effects | Commit_PouchFailureSkipsLaterChangesAndDiscardsThemBeforeNextScope |
| Reentrant scopes keep separate pending facts | Commit_ReentrantPouchMutationCannotConsumeOuterScopePendingChanges |
| Fixed stages still run after unlock; commit/dispose idempotence and ordered first failure | Commit_PublishesOnceAfterUnlockAndMakesStoragePermanent; Commit_LaterContainerFailurePreservesEarlierPublicationAndSkipsLaterContainersAndActions; Commit_AfterPublicationFailureStopsLaterActionsAfterContainerPublication |

Focused transaction tests: 33 passed. Focused equipment/pouch tests: 71 passed. Full Abstractions suite: 299 passed; GameWorld: 1,287 passed on an unchanged independent rerun; Scripts: 335 passed. The initial GameWorld invocation had 14 five-second authentication/rate-limiter timeouts, retained separately in the local validation logs. No unrelated timeout or test configuration was changed.

The fixed completion contract has exactly three operations and is internal. Its production owners are equipment and pouch only. The transaction field list is thread id, boundaries, lock order, snapshots, changed slots, completion count, acquired-lock count and state. There is no executable-work collection or wrapper. The public API and all issue #347 trade/session ownership remain unchanged. Existing eager-reader and preflight gameplay-validation limitations remain unchanged; no live gameplay client was exercised.

Final local solution build: zero errors and 73 warnings. The exact CI command `dotnet test --no-build --no-restore -p:TestingPlatformCommandLineArguments="--report-trx --coverage --coverage-output-format cobertura"` completed all 27 assemblies with 3,190 passed, zero failed and four skipped. Strict OpenSpec passed the change, all 73 active items and four archives. Pinned jscpd 5.3.2 against PR base `9c9f8622305d18ca05fa7099848a0cdb01cf09b6` reported 502 historical clone pairs and zero new pairs. Diff checks passed. No baseline or suppression changed. Hosted CI/CodeQL must be checked against the final pushed head and recorded in the PR description and handoff.

A final test-only correction asserts that observed MoneyPouchChangedEvent instances are non-null, removing two newly introduced nullable warnings without suppression. The focused equipment/pouch suite still passed all 71 cases. The latest solution build had zero errors and 180 warnings, reflecting a different incremental compilation scope. The exact full CI command initially hit two existing WorldSignInCommandConsumerTests timing failures; its unchanged rerun completed all 27 assemblies with 3,190 passed, zero failed and four skipped. These transient runs remain recorded, with no timeout/configuration changes. Final strict OpenSpec, duplication and diff checks passed again.

## Direct typed equipment execution cleanup

Removed ToAction, RunPostCommitActions(Action[]) and all conversion of equipment facts into executable objects. Matching transactional batches are dequeued and iterated directly through ExecuteEquipmentEffect. Standalone equipment changes explicitly attempt all lifecycle effects, then publication, then throw retained failures. A single exception is rethrown through ExceptionDispatchInfo; multiple failures retain their original leaf objects in order in a flat AggregateException. DiscardPendingCompletion is a mechanical rename across the fixed owner bridge and callers. EquipmentEffect remains the existing small enum/record with optional Incoming for weapon profile updates.

| Requirement | Regression evidence |
| --- | --- |
| Pending facts and execution contracts have no delegates or delegate arrays | EquipmentCompletion_FactsAndExecutionContractsContainNoDelegates |
| Standalone and transaction completion attempt every effect and publication; flat original failures and no retry/stale effects | EquipmentCompletion_AttemptsEffectsAndPublicationRetainsFlatFailuresAndNeverRetries, both data rows |
| Ordered weapon/shield/profile/incoming attempt-all behavior | EquipItem_HookAndPublisherFailuresPreserveBothAfterAttemptingOwnedHooks |
| Multiple effects, aliases and publication order | Commit_MultipleEquipmentMutationsCompleteOnceBeforePublicationDespiteAliases |
| Rollback/stale owner state and reentrant pouch isolation remain intact | Dispose_EquipmentAndPouchEffectsAreDiscardedWithoutLeakingIntoLaterScope; Commit_ReentrantPouchMutationCannotConsumeOuterScopePendingChanges |

Focused equipment/pouch suite: 74 passed. Full suites: Abstractions 299, GameWorld 1,290, Scripts 335 passed. No change to transaction ordering, scope-keyed queues, ordinals, locks, snapshots, contention, pouch behavior or public lifecycle. The transaction and pouch production diff is only the discard-method rename. Broad catches remain intentional for attempt-all completion and best-effort cleanup; no suppression is added. Changes remain local under the user's instruction not to commit, push or monitor hosted checks automatically. Hosted CI/CodeQL do not validate this local cleanup and have not been inspected for it.

Final local validation after sharing duplicated failure-observation setup in one private test helper: focused equipment/pouch tests 74 passed; exact full CI command completed all 27 assemblies with 3,193 passed, zero failed and four skipped. Solution build had zero errors and six warnings. Strict OpenSpec passed the change, all 73 active items and four archives. Pinned jscpd 5.3.2 against the existing verified PR base 9c9f8622305d18ca05fa7099848a0cdb01cf09b6 reports 502 historical pairs and zero new pairs; no baseline/ignore changes. Diff checks passed. Review found no remaining correctness defect or reason to change the approved architecture. No commits, pushes, external PR edits or hosted monitoring were performed for this cleanup.
