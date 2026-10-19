# Tasks

## 1. Encapsulate lifecycle state

- [x] 1.1 Make `TradeSessionState.State` privately settable, add the four specified guarded transition methods, and replace every production assignment; verify no `session.State = TradeState...` assignments remain and the Game Scripts project builds.
- [x] 1.2 Verify existing public-flow regressions cover terminal-before-commit cleanup and failed-completion recovery: `FinishTradeSession_WhenPublicationFailsAfterCommit_StillCompletesAndCleansUp`, `FinishTradeSession_WhenPouchPublicationFailsAfterCommit_StillCompletesAndCleansUp`, `CancelTradeSession_WhenRefundPublicationFailsAfterCommit_StillCancelsAndCleansUp`, and `FinishTradeSession_WhenExchangeAndRefundCannotFit_ResetsAcceptanceAndPreservesEscrow`.

## 2. Synchronize the behavior contract

- [x] 2.1 Sync the modified `trading-completion` requirement into the canonical spec and verify focused trade tests, the Game Scripts build, `openspec validate --all --strict`, and `git diff --check` pass.
