# Tasks

## 1. Characterize public ownership behavior

- [x] 1.1 Add tests that `RenewAsync` and `ExecuteIfOwnerAsync` reject a different owner without writing or invoking the callback.
- [x] 1.2 Preserve the existing release, renew, and execute-if-owner success and absent-claim tests.

## 2. Consolidate the owner check

- [x] 2.1 Extract the cache lookup and exact-owner comparison into a private instance method and call it from the three public operations.
- [x] 2.2 Run the focused `FusionCacheGameSessionClaimStoreTests` and verify owner acceptance, rejection, and operation-specific effects.

## 3. Verify the scoped refactor

- [x] 3.1 Rerun `npx jscpd --mode strict --kind exact --reporters ai Hagalaz.Services.GameWorld/Services/FusionCacheGameSessionClaimStore.cs` and confirm the owner-check clone is gone.
- [x] 3.2 Run strict OpenSpec validation and `git diff --check`.
