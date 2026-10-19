# Tasks

## 1. Resolve completion owners independently

- [x] 1.1 Build a reference-deduplicated completion-owner list from resolved boundary order before lock sorting; remove global transaction completion ordinals and invoke each owner once per phase. Verify transaction completion and failure-order tests pass.
- [x] 1.2 Change the internal owner contract to parameterless completion/discard methods and validate owner-local enqueue against the active transaction, originating thread, boundary binding, and mutation lock. Verify the Abstractions and Scripts test projects compile and focused completion tests pass.

## 2. Keep completion facts owner-local

- [x] 2.1 Replace Equipment's transaction-keyed completion dictionary with an owner-local FIFO while retaining attempt-all hook behavior. Verify `EquipmentCompletion_AttemptsEffectsAndPublicationRetainsFlatFailuresAndNeverRetries` and related equipment completion tests pass.
- [x] 2.2 Replace MoneyPouch's transaction-keyed queue with an owner-local FIFO and update the FIFO, first-seen participant order, alias, and failure-order regressions. Verify the focused MoneyPouch tests pass.
- [x] 2.3 Update the Scripts trade pouch test double to parameterless FIFO completion. Verify focused `TradeExchangeTests` pass.

## 3. Specify and validate the final behavior

- [x] 3.1 Update the canonical and delta item-container-storage requirement to distinguish owner order, boundary publication order, and lock order. Verify `openspec validate --all --strict` passes.
- [x] 3.2 Run focused Abstractions, GameWorld Equipment/MoneyPouch, and Scripts trade tests, then build `Hagalaz.sln` and run `git diff --check`; review the final status and cumulative diff.
