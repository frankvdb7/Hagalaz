# Design

`ItemContainerTransaction.Begin(...)` already resolves and binds participant storage. Keep that explicit membership model. Remove the `EnsureOutsideTransaction` guard from ordinary container operations and rely on `ItemContainerStorage.EnsureMutationAccess()` for thread/active-scope validation. Existing `NotifyChanges` records changes when storage is bound and publishes immediately otherwise; use this single path for both standalone and enlisted operations.

Keep `IItemContainerMutationBoundary` as the participant contract for transaction creation plus the special atomic cross-storage `TryTransferTo` operation. Remove generic add, range add, exact remove, sort, clear, and outside-transaction operations from it. Ordinary mutations remain on `IItemContainer`.

MoneyPouch retains a private composite participant contributing pouch and inventory boundaries. Its public exact methods inspect those existing storage bindings in one private helper: no bound storage means the method owns a scope; every required storage bound to the same active transaction means it participates; partial or conflicting binding throws before mutation. Its core methods use normal inventory APIs so enlisted inventory mutations flow through existing storage notification. Keep immutable completion facts and their current ordering.

For simple Equipment storage operations, retain low-level access validation and locks. `TryRestoreEquippedItem` and partial removal use normal `PublishChanges`, which defers automatically when bound. Full removal, replacement, and clear choose immediate lifecycle completion when unbound and transaction-owned deferred effects when bound. Explicitly transaction-owning Equipment workflows and interactive unequip behavior remain as they are.

Migrate ordinary `.Mutations` calls to their corresponding container methods. Keep participant arguments in transaction creation and keep `.Mutations.TryTransferTo` for cross-storage transfer. Do not dynamically add storage. Update current and delta specs plus behavior-focused tests.
