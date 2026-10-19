# Design

## Context

`ItemContainerChangeTracker` already owns committed and provisional changed-slot batches. Today, generic publication delegates consume those batches from mutation-boundary cleanup and from `ItemContainerTransaction.Commit()`. Domain containers expose the same mutable `IItemContainer` to scripts, so those operations can bypass the domain owner.

The production audit found callback-backed Inventory, Bank, Reward, Familiar Inventory, Shop Stock, Trade Offer, and Equipment construction. There are 148 direct domain `Items` mutation call sites in GameWorld and Scripts, mostly Inventory. Equipment and MoneyPouch also have owner-local lifecycle/notification facts.

## Goals / Non-Goals

**Goals:**

- Make ChangeTracker the only changed-slot accumulator and remove generic publication delegates.
- Make domain-owned storage read-only to external consumers and route existing writes through owner commands.
- Preserve atomic mutation, rollback, deterministic lock ordering, publication ordering, and post-commit failure behavior.
- Keep standalone raw `ItemContainer` operations usable without a transaction; callers consume their tracker explicitly.

**Non-Goals:**

- Adding a publisher interface, notification bus, generic mutation gateway, callback registry, second transaction type, or new tracker.
- Changing item-storage algorithms, PriceChecker's pull-based refresh, or unrelated gameplay rules.
- Changing archived specifications or remote PR state.

## Decisions

### Domain interfaces own writes and publication

Domain interfaces expose `IReadOnlyItemContainer Items` and only the mutation commands already required by production. Each command uses the existing concrete `ItemContainer` and its boundary. If no current-thread transaction owns the storage, the command opens a short `ItemContainerTransaction` over the domain participant, performs the ordinary mutation, commits, consumes its tracker, and publishes the domain event before disposing the scope. If an outer transaction already owns the storage, the command participates and leaves publication to that operation's domain coordinator.

The public domain participant itself implements `IItemTransactional`; its internal transaction source contributes the same storage boundaries as before. No storage or boundary is exposed to Scripts.

### Commit retains logical ownership through explicit publication

`Commit()` is the irreversible storage transition: it promotes pending tracker data and releases physical monitors, but retains transaction bindings until `Dispose()`. An owner can consume committed changes on the originating thread while the committed transaction remains bound. Foreign mutation and reads continue waiting until scope disposal. Disposal before commit restores snapshots; disposal after commit never rolls back and always clears bindings and pending owner facts.

This scope lifetime lets owner commands publish outside physical locks without allowing another mutation to interleave between commit and publication. Multi-owner coordinators publish each affected owner in the established domain order before disposing the transaction.

### Specialized completion stays with its domain

Equipment consumes its tracker only after attempting its owner-local FIFO lifecycle effects, then publishes equipment state. MoneyPouch retains its immutable pending change facts and publishes them after the operation's ordinary container publications. Transaction rollback continues to discard pending domain facts through the existing small completion-owner bridge; the transaction does not publish container events.

For a multi-owner operation, its existing domain coordinator attempts all independent publications required by that operation and preserves its existing error policy. A publisher consumes its tracker batch before sending the event, so a failure is not retried. Scope disposal releases retained ownership even when publication throws.

### Raw containers and transfers remain pull-based

`ItemContainer` constructors take only storage configuration. Standalone raw-container mutations and atomic standalone transfers record committed tracker changes without invoking external code. Temporary containers such as PriceChecker and Trade Offer keep their own explicit UI refresh behavior. `TryTransferTo` remains the atomic storage primitive and still participates in an already-bound transaction.

### Rejected alternatives

- Keeping the delegate under a publisher interface or observer registration would preserve the same hidden invocation mechanism under a new name.
- Leaving mutable `Items` properties would allow callers to bypass domain publication.
- Publishing after disposing a transaction would release ownership before event delivery and allow another mutation to interleave.
- A generic `Mutate(Action<IItemContainer>)` API would recreate a mutation gateway and hide domain ownership.

## Risks / Trade-offs

- **Domain API migration touches many call sites.** Limit commands to operations that existing production code uses; keep reads on the read-only view.
- **An explicit transaction can be disposed without owner publication.** Migrate every transaction coordinator, document the ordering, and add tests for each coordinated domain flow. Do not add generic transaction publication registration.
- **Publisher exceptions occur after committed storage.** Consume before send, release bindings during disposal, and preserve the operation-specific attempt-all/first-failure policy.
- **Transaction use now spans explicit publication.** Keep all operations synchronous and on the originating thread; never await while a committed binding is retained.

## Migration Plan

1. Remove the delegate and preserve tracker/transaction behavior with focused Abstractions tests.
2. Update transaction completion so Commit releases monitors and Dispose releases the retained binding.
3. Add read-only domain item views, transaction participation, and minimal owner commands.
4. Move each domain event and specialized completion to its owner; migrate all direct mutation and transaction call sites.
5. Update behavior specs and tests, then run the full requested validation and three focused self-review passes.

Rollback is source-level: if the ownership/publication tests cannot demonstrate the same guarantees, do not commit the architectural migration.
