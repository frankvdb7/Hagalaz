# Farming cycle progress

## ADDED Requirements

### Requirement: Preserve current-cycle progress across hydration

The GameWorld MUST represent current-cycle ticks as nonnegative elapsed progress within the active farming cycle. Hydration MUST combine normalized persisted progress with elapsed offline ticks using arithmetic that does not overflow for realistic offline intervals. It MUST NOT use oversized legacy tick values to replay cycles already represented by the persisted crop stage and condition state.

#### Scenario: Offline time does not complete the current cycle

- **GIVEN** a patch has saved progress within its current cycle
- **AND** elapsed offline time does not complete that cycle
- **WHEN** the patch is hydrated
- **THEN** its cycle and conditions remain unchanged
- **AND** its progress equals saved progress plus elapsed offline ticks

#### Scenario: Offline time completes one or more cycles

- **GIVEN** a patch has saved progress within its current cycle
- **AND** elapsed offline time crosses one or more growth thresholds
- **WHEN** the patch is hydrated
- **THEN** the existing growth transitions are applied
- **AND** the remaining current-cycle progress is nonnegative

#### Scenario: Oversized legacy progress on an active crop

- **GIVEN** persisted `CurrentCycle` and condition state already represent completed crop cycles
- **AND** persisted `CurrentCycleTicks` is larger than the active cycle duration
- **WHEN** the patch is hydrated
- **THEN** the legacy value is normalized to progress within the current cycle before offline time is applied
- **AND** its quotient does not advance the persisted crop stage again

#### Scenario: Mature or dead patch with legacy accumulated ticks

- **GIVEN** the persisted patch is mature or dead
- **AND** its legacy `CurrentCycleTicks` contains accumulated time outside a cycle
- **WHEN** the patch is hydrated
- **THEN** its terminal crop state is unchanged
- **AND** its live tick progress is zero

#### Scenario: Long offline interval

- **GIVEN** a patch has persisted progress and has been offline for multiple years
- **WHEN** the patch is hydrated
- **THEN** elapsed time is accumulated without signed integer overflow
- **AND** catch-up stops once terminal or non-progressing farming state is reached

#### Scenario: Hydrated progress is dehydrated for persistence

- **GIVEN** a patch has been hydrated from valid persisted progress
- **WHEN** the farming state is dehydrated
- **THEN** `CurrentCycleTicks` is nonnegative and represents the resulting progress
- **AND** the existing checked persistence conversion accepts the value

### Requirement: Preserve unsigned-to-signed farming progress safely

The Characters service MUST reject a persisted unsigned `CurrentCycleTicks` value that cannot be represented by the signed runtime DTO. It MUST NOT silently wrap such a value into negative progress.

#### Scenario: Database progress exceeds the signed runtime range

- **GIVEN** a persisted farming patch has `CurrentCycleTicks` greater than `Int32.MaxValue`
- **WHEN** Characters maps the database row to the runtime DTO
- **THEN** the mapping fails with overflow
- **AND** it does not return a wrapped negative progress value
