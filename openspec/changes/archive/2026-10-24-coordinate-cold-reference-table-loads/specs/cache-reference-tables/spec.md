## ADDED Requirements

### Requirement: Concurrent reference-table initialization is coordinated per index

The reference-table provider MUST publish one decoded table for concurrent cold reads of the same index. Invalidation MUST synchronize with initialization so the table is cleared before invalidation returns. Initialization for different indices MUST remain independent.

#### Scenario: Concurrent cold reads share one initialization

- **WHEN** concurrent callers request the same uncached reference-table index
- **THEN** the provider reads and decodes that table once and returns the same published instance to each caller

#### Scenario: Invalidation overlaps cold initialization

- **WHEN** a table is invalidated while its first read is decoding
- **THEN** invalidation waits for initialization, clears the published table, and a later read loads the table again
