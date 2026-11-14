# reference-table-contracts Specification

## Purpose
Keep Cache API reference-table reads on dedicated DTOs.

## Requirements

### Requirement: Reference-table reads use dedicated DTOs
Reference-table responses SHALL use dedicated table, entry and child DTOs while preserving all existing numeric metadata, sparse IDs, counts/capacities, ordering and base64 digests.
#### Scenario: Inspect sparse table
- **WHEN** a reference table is requested
- **THEN** its JSON fields, numeric flags and entry/child metadata retain their existing values
