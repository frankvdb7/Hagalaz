## ADDED Requirements

### Requirement: Persistence is explicit
Character dehydration MUST include only active states that implement the persistent-state capability. Runtime-only, activity, and equipment-derived states MUST NOT be serialized merely because they are present in the creature state collection.

#### Scenario: Runtime-only state is excluded
- **WHEN** a character has a runtime-only state and a persistent state
- **THEN** dehydration contains only the persistent state

#### Scenario: Persistent timed state retains its duration
- **WHEN** a persistent timed state is dehydrated
- **THEN** its stable ID and remaining tick count are stored

#### Scenario: Persistent until-removed state is supported
- **WHEN** a persistent state has no timed capability
- **THEN** it is stored by stable ID without requiring a fake duration sentinel

### Requirement: Registry activation is narrow and predictable
The state registry MUST create persistent states and resolve stable IDs through explicit operations without exposing raw implementation-type lookup to gameplay or persistence callers. Runtime-only `IState` implementations MUST NOT be discovered merely because they carry legacy metadata.

#### Scenario: Known state ID activates
- **WHEN** hydration requests a registered persistent state ID
- **THEN** the registry returns a new state instance for the registered implementation

#### Scenario: Unknown state ID is compatible
- **WHEN** hydration requests an unknown or removed state ID
- **THEN** registry activation returns no state and character hydration continues without a raw dictionary exception

#### Scenario: Duplicate IDs fail registration
- **WHEN** startup discovers two state implementations with the same stable ID
- **THEN** state registration fails clearly and does not silently choose the first implementation

#### Scenario: Runtime-only state is not registered
- **WHEN** startup discovers a metadata-bearing state that does not implement `IPersistentState`
- **THEN** the state is excluded from the persistence registry and does not require a stable persistence identity

#### Scenario: Persistent state without metadata cannot crash save
- **WHEN** a runtime persistent-state instance has no registered stable ID
- **THEN** generic dehydration omits that instance and completes without a null-forgiving reflection failure

### Requirement: Equipment-derived state is not durable truth
Equipment-derived state MUST remain runtime-only for this change and MUST be rebuilt by the authoritative equipment path rather than restored from independent character state data.

#### Scenario: Bow condition survives normal ticks
- **WHEN** a standard bow is equipped and the creature processes subsequent game ticks
- **THEN** the bow condition remains active until the bow is unequipped

#### Scenario: Unequipping revokes bow condition
- **WHEN** the equipped bow is removed through the equipment path
- **THEN** the bow condition is removed and is not retained by character persistence
