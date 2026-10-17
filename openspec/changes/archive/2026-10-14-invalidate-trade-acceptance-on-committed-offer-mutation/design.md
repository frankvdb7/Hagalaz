# Design

## Context

See proposal.md for the failure scenario. `ItemContainerStorage` already advances a revision on storage mutations, and transaction rollback restores the captured revision. The item-container boundary already synchronizes reads of change-tracker state with active transactions and publication ownership.

## Goals / Non-Goals

**Goals:** Use the synchronized storage revision as the trade offer's acceptance revision and clear acceptance state when an offer mutation committed but its operation exits exceptionally.

**Non-Goals:** Change transaction completion ordering, retry publication, or add trade-specific revision bookkeeping.

## Decisions

- Expose the existing synchronized storage revision through `ItemContainerChangeTracker.Revision`. The boundary performs the read under its existing mutation lock and waits for foreign transaction/publication ownership to finish; rollback therefore remains invisible to readers. This avoids a second revision counter that could drift from storage.
- Have `TradeContainer.Revision` read that tracker revision, preserving the existing initial revision offset. Publication continues to consume slot changes but no longer defines offer identity.
- In the existing session-gated operation wrapper, capture the offer revision before the operation. If the operation throws and the revision changed, clear both acceptance snapshots before rethrowing. This handles committed changes whose publisher failed while leaving rollback failures alone.

The simpler option of incrementing `TradeContainer.Revision` from `PublishChanges()` is rejected because publication can throw before that method runs. A second trade-owned counter is rejected because storage already owns the authoritative mutation revision.

## Risks / Trade-offs

- The revision getter may wait for another thread's transaction or publication, matching existing `HasChanges` synchronization semantics. It does not consume pending changes.
- A publisher exception still propagates and may leave UI refresh incomplete; this change only protects acceptance validity and does not retry notifications.
