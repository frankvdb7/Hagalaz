# Spec Delta

## MODIFIED Requirements

### Requirement: Equipment and MoneyPouch own specialized publication
Equipment MUST run its lifecycle effects before consuming and publishing committed Equipment changes. MoneyPouch MUST retain FIFO immutable change facts and publish its messages/events after other domain containers affected by the same operation. Both owners MUST discard pending facts on rollback. Post-commit failures MUST leave storage irreversible and consume each attempted batch once.

#### Scenario: Equipment effects precede its event
- **WHEN** a committed Equipment mutation has lifecycle effects
- **THEN** all required effects are attempted before the Equipment event is sent

#### Scenario: Equipment effects precede inventory and equipment publication
- **WHEN** a committed Equipment mutation has lifecycle effects and changes Inventory
- **THEN** all required effects are attempted before Inventory publication, which precedes Equipment publication

#### Scenario: MoneyPouch publishes captured facts after container events
- **WHEN** a committed operation changes both a pouch and an ordinary item container
- **THEN** the ordinary container event precedes the MoneyPouch message/event, which uses its captured counts and change amount

#### Scenario: MoneyPouch publishes captured facts after inventory events
- **WHEN** a committed operation changes both a pouch and its inventory
- **THEN** the inventory event precedes the MoneyPouch message/event, which uses its captured counts and change amount

#### Scenario: Rollback discards specialized facts
- **WHEN** Equipment or MoneyPouch changes are staged in a transaction that is disposed without Commit
- **THEN** no lifecycle effect, pouch message, or domain event is emitted

#### Scenario: Equipment hook failure does not roll back storage
- **WHEN** an Equipment lifecycle hook throws after Commit
- **THEN** all required hooks are attempted, Equipment state publication is attempted, storage remains committed, and the hook failures propagate after publication

#### Scenario: Equipment hook and publication failures are both preserved
- **WHEN** one or more Equipment lifecycle hooks fail and Equipment state publication also fails after commit
- **THEN** all required hooks and the Equipment publication are attempted, storage remains committed, and both hook and publication failures propagate
