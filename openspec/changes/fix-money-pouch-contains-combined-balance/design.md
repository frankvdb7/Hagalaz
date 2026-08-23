## Context

`MoneyPouchContainer` already owns the coin-specific containment override and already has access to the pouch and eligible inventory state. The defect is limited to the query path: its current fallback structure does not consistently evaluate the combined balance. See `proposal.md` and the capability delta for the externally observable contract.

## Goals / Non-Goals

**Goals:**

- Keep the fix local to `MoneyPouchContainer.Contains`.
- Read both existing balance sources for coin queries and compare their combined value using a wider intermediate type.
- Add direct regression tests for all specified balance distributions and the overflow boundary.

**Non-Goals:**

- Changing mutation, payment, notification, or generic item-container code.
- Introducing a generic multi-container abstraction or a new owner for balances.

## Decisions

- **Use the existing pouch and eligible-inventory access paths.** This preserves the current ownership and eligibility rules. A new aggregation service or generic container API would broaden the change without solving a second problem.
- **Perform the addition in a wider numeric type before comparison.** This prevents a mathematically valid combined balance from wrapping through an `int`. Casting only after an `int` addition is rejected because it would be too late to prevent overflow.
- **Branch only on the coin identifier.** Non-coin containment remains delegated to the existing behavior, avoiding changes to generic item semantics.
- **Test the container directly.** Direct tests prove the owner of the corrected behavior and avoid coupling this bug fix to shop, trade, or payment integration flows.

## Risks / Trade-offs

- [Risk] The test fixture may make inventory eligibility implicit. → Reuse the existing fixture/setup conventions and assert through the public containment method so the test exercises the same eligible inventory path as production.
- [Risk] A future change could alter the supported balance type. → Keep the wider intermediate local to the comparison and avoid changing storage or mutation types.

## Migration Plan

No data or deployment migration is required. Deploy the local query and regression tests together; rollback is a source revert if necessary.
