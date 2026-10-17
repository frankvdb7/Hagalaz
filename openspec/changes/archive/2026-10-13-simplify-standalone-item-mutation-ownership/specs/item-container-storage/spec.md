# Spec Delta

## MODIFIED Requirements

### Requirement: Domain-owned containers control mutation publication
Ordinary domain containers MUST expose item reads through `IReadOnlyItemContainer`; Equipment MUST expose a read-only item view and transaction participation without generic mutation. Writes MUST use the owning domain's existing or narrowly required mutation commands. A standalone command MUST use boundary-owned mutation ownership rather than a rollback transaction solely to retain ownership through publication, and MUST publish its committed tracker batch after releasing physical locks and before releasing its logical ownership. A caller-owned transaction MUST publish each affected domain through that owner after Commit and before disposing the transaction. Raw `ItemContainer` remains directly mutable and publishes no domain events; its owner consumes `ChangeTracker` explicitly.

#### Scenario: Domain item views cannot bypass their owner
- **WHEN** a consumer accesses Inventory, Bank, Reward, Familiar Inventory, or Shop Stock items through its public interface
- **THEN** it can query the items but cannot invoke generic storage mutations

#### Scenario: Equipment exposes a read-only item view
- **WHEN** a caller accesses equipment through `IEquipmentContainer`
- **THEN** it can query its items and participate in a transaction, but cannot invoke generic storage mutations through the item view

#### Scenario: Standalone domain command publishes committed changes
- **WHEN** a domain mutation succeeds without an enclosing transaction
- **THEN** its owner publishes the committed changed slots once after the storage mutation lock is released and before logical ownership is released

#### Scenario: Standalone publication does not require rollback state
- **WHEN** an ordinary single-container domain mutation succeeds outside a caller-owned transaction
- **THEN** it retains boundary ownership through domain publication without capturing a rollback snapshot

#### Scenario: Multi-owner transaction publishes before releasing ownership
- **WHEN** a caller commits mutations to multiple domain containers
- **THEN** it publishes each affected owner in the operation's established order before disposing the scope, and foreign mutations wait until disposal

#### Scenario: Rollback does not publish domain changes
- **WHEN** a domain mutation participates in a transaction that is disposed without Commit
- **THEN** storage and tracker state are restored or discarded without a domain notification
