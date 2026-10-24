# cache-reference-tables Specification

## Purpose

Defines how reference tables represent sparse archive identifiers independently from the number of groups present.

## Requirements

### Requirement: Archive identifier capacity is distinct from group count

A reference table MUST preserve each archive's identifier independently of the number of groups it contains. `Count` MUST report the number of present groups. `Capacity` MUST report one greater than the greatest present archive identifier, or zero when no groups are present. Missing identifiers within that capacity MUST remain absent.

#### Scenario: A table contains sparse archive identifiers

- **WHEN** a reference table contains archive identifiers 1 and 100
- **THEN** its Count is 2, its Capacity is 101, and lookup by identifier 100 returns the corresponding entry

#### Scenario: A lookup targets a gap between sparse identifiers

- **WHEN** a caller looks up archive identifier 50 in that table
- **THEN** the lookup reports that the archive is absent

### Requirement: Reference-table encoding preserves sparse identifiers and group count

Reference-table encoding and decoding MUST preserve every present archive identifier and its associated entry while using the number of present groups as the serialized group count.

#### Scenario: A sparse table is encoded and decoded

- **WHEN** a reference table with archive identifiers 1 and 100 is encoded and decoded
- **THEN** the decoded table has Count 2 and Capacity 101, and both entries remain available under their original identifiers

### Requirement: Reference-table responses report both group count and ID capacity

The reference-table response MUST report the number of present groups and the exclusive upper bound of the archive-ID space as separate values, and MUST preserve each entry's archive identifier.

#### Scenario: A sparse table is returned by the reference-table endpoint

- **WHEN** the endpoint returns a reference table containing archive identifiers 1 and 100
- **THEN** the response reports Count 2, Capacity 101, and entries keyed by identifiers 1 and 100

### Requirement: Concurrent reference-table initialization is coordinated per index

The reference-table provider MUST publish one decoded table for concurrent cold reads of the same index. Invalidation MUST synchronize with initialization so the table is cleared before invalidation returns. Initialization for different indices MUST remain independent.

#### Scenario: Concurrent cold reads share one initialization

- **WHEN** concurrent callers request the same uncached reference-table index
- **THEN** the provider reads and decodes that table once and returns the same published instance to each caller

#### Scenario: Invalidation overlaps cold initialization

- **WHEN** a table is invalidated while its first read is decoding
- **THEN** invalidation waits for initialization, clears the published table, and a later read loads the table again
