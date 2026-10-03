# Tasks

## 1. Committed terminal operations

- [x] 1.1 Return committed trade changes for explicit publication; set terminal state before publication and guarantee cleanup in `finally`, removing captured publication exceptions.
- [x] 1.2 Keep the single transaction commit/publication API, rollback, and at-most-once publication while removing collection and aggregation of publisher exceptions.
- [x] 1.3 Simplify pouch and shop publication to direct ordered calls; retain successful domain ordering and message amounts and the existing equipment lifecycle cleanup policy.
- [x] 1.4 Update regressions for first-error publication and terminal-state-before-publication; retain committed storage, rollback, cleanup, retry, and race coverage.

## 2. Safe confirmation and retry behavior

- [x] 2.1 Clear both accepted flags and revisions, refresh confirmation UI, and preserve escrow when completion and ordinary refund both fail; add a capacity-failure regression proving no recipient mutation and fresh confirmation is required.
- [x] 2.2 Bind offer-stage and final confirmation handlers to their captured session; add deterministic concurrent final-accept and stale-callback regressions through the registered widget handlers.

## 3. Lifecycle race coverage

- [x] 3.1 Add deterministic completion races against owner destruction, target destruction, and interruption; assert exactly one conserved outcome across inventories, escrow, and recovery containers.

## 4. Integration verification

- [x] 4.1 Run focused abstraction, script, and GameWorld suites and the solution build; sync and validate the current specs, validate this change strictly, and review the cumulative diff for scope and duplicate lifecycle paths.
- [x] 4.2 Run the pinned PR-only jscpd baseline gate against the pull request base and verify it reports no new clone pairs.

## Validation record after publication simplification

- `Hagalaz.Game.Abstractions.Tests`: 284 passed.
- `Hagalaz.Game.Scripts.Tests`: 335 passed.
- `Hagalaz.Services.GameWorld.Tests`: 1,267 passed, including production economic and equipment regression coverage.
- `dotnet build Hagalaz.sln --no-restore`: succeeded with 6 warnings and 0 errors on the final build.
- Current storage and trading-completion specs are synced; strict change validation and all 4 current specs passed.
- Cumulative diff, production callers, obsolete APIs, publication ordering, rollback, and ownership reviewed; `git diff --check` passed. Transaction, pouch, shop, and trade publication contain no captured or aggregated exceptions; the first publication failure propagates directly and committed trade cleanup runs in `finally`.
- PR #516 review: pinned jscpd 5.3.2 passed against actual PR base `9c9f8622305d18ca05fa7099848a0cdb01cf09b6` with zero new clone pairs. The approved transaction API in this historical change was superseded by `simplify-item-container-transaction-scope`; its trade lifecycle guarantees remain in force.
