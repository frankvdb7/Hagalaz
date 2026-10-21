# Tasks

## 1. Define and test inventory capacity behavior

- [x] 1.1 Add direct read-only container API tests for nonpositive counts, stack boundaries and slot requirements, AlwaysStack behavior, and partial/full nonstack transfers.
- [x] 1.2 Add the focused capacity operation to `IReadOnlyItemContainer`, `ItemContainer`, and `ItemContainerStorage`.

## 2. Reuse inventory capacity behavior

- [x] 2.1 Replace the duplicated bank and reward capacity checks while preserving messages, count updates, and transactions.

## 3. Verify the scoped refactor

- [x] 3.1 Run the focused item-container API and GameWorld item-transfer tests.
- [x] 3.2 Re-run the strict exact-clone scan and confirm the bank/reward pair is gone.
- [x] 3.3 Review the scoped and cumulative PR diffs and validate the OpenSpec change.
