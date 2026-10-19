# Spec Delta

## MODIFIED Requirements

### Requirement: Equipment exposes semantic mutation operations
`IEquipmentContainer` MUST expose domain-specific mutation operations rather than generic `Add`, `Replace`, `Remove`, and `Clear` commands. Direct restoration, in-place transformation, removal, and clearing MUST use explicitly named equipment operations. Equipment replacement MUST require the caller's expected current item instance so a stale command cannot replace a different equipped item. Simple Equipment storage operations MUST participate when Equipment storage is enlisted; typed lifecycle completion MUST be deferred through the owning transaction. When unbound, lifecycle completion and publication occur immediately. `TryMoveTo(...)` MUST own a short transaction when neither required storage is enlisted, participate without committing or disposing when both belong to the same active current-thread transaction, and throw before mutation for partial or conflicting participation. Full removal MUST defer lifecycle completion until the owning transaction commits. `EquipItem()` and `UnEquipItem()` MUST remain standalone gameplay commands and MUST NOT participate in a caller-owned transaction over their required storage.

#### Scenario: Stale equipment replacement is rejected
- **WHEN** the expected item instance no longer occupies the requested slot
- **THEN** replacement returns false without changing storage, publishing an equipment update, or running lifecycle callbacks

#### Scenario: Equipment storage mutation participates in an active scope
- **WHEN** restoration, replacement, removal, or clearing is called while Equipment storage belongs to an active transaction
- **THEN** the storage mutation participates in that transaction, with lifecycle completion and publication deferred until commit

#### Scenario: Equipment transfer owns a standalone transaction
- **WHEN** `TryMoveTo(...)` moves an equipped item while neither Equipment nor destination belongs to a current-thread transaction
- **THEN** it commits one short transaction and publishes the committed Equipment and destination changes

#### Scenario: Equipment transfer participates in a caller-owned transaction
- **WHEN** `TryMoveTo(...)` moves an equipped item while Equipment and destination belong to the same active current-thread transaction
- **THEN** it stages the move and defers lifecycle completion and publication until the caller commits

#### Scenario: Equipment transfer rejects partial or conflicting participation
- **WHEN** only one required storage is enlisted or required storages belong to different active current-thread transactions
- **THEN** `TryMoveTo(...)` throws before changing either storage

#### Scenario: Equipment gameplay commands remain standalone
- **WHEN** a caller invokes `EquipItem()` or `UnEquipItem()` for an operation requiring inventory and Equipment
- **THEN** the command owns its workflow and does not join a caller-owned transaction over those storages
