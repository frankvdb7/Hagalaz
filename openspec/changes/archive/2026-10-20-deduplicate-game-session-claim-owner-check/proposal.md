# Proposal

## Why

`FusionCacheGameSessionClaimStore.ReleaseAsync`, `RenewAsync`, and `ExecuteIfOwnerAsync` repeat the same cache lookup and claim-owner comparison under the existing distributed lock. Keeping that ownership rule in three places risks the public operations disagreeing about who may modify or act on a claim.

## What Changes

- Extract the shared cache lookup and exact-owner comparison into a private method on the existing `FusionCacheGameSessionClaimStore`.
- Add public API tests for mismatched-owner behavior in renewal and callback execution.

## Scope and Acceptance Criteria

- Only `ReleaseAsync`, `RenewAsync`, `ExecuteIfOwnerAsync`, their focused tests, and one private owner-check method change.
- The three public operations remain inside the existing `WithClaimLockAsync` boundary and use the same cache key, entry options, token, and exact string comparison.
- A missing or different claim owner returns `false` without removing, refreshing, or invoking the callback.
- A matching owner retains each operation's existing behavior: release removes the claim, renew refreshes it, and execute-if-owner refreshes it before invoking the callback and returns the callback result.
- Existing public API tests and new mismatched-owner tests pass.
- If consolidation requires changing lock ownership, claim transitions, or callback semantics, stop and propose a separate change.

## Non-Goals

- Do not change `TryClaimAsync`, `ExecuteIfOwnerAndReplaceAsync`, cache configuration, keys, or distributed-lock behavior.
- Do not introduce a new class, interface, static helper, callback framework, or additional state owner.
- Do not modify public API signatures or runtime registrations.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

None. This is an implementation-only refactor; claim ownership behavior remains the contract.

## Impact

- `Hagalaz.Services.GameWorld/Services/FusionCacheGameSessionClaimStore.cs`
- `Hagalaz.Services.GameWorld.Tests/FusionCacheGameSessionClaimStoreTests.cs`

No API signature, dependency, registration, or runtime topology changes are required.
