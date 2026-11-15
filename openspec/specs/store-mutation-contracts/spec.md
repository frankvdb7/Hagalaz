# store-mutation-contracts Specification

## Purpose
Keep Cache API store mutations on DTOs specific to each operation.

## Requirements

### Requirement: Store mutations use operation-specific DTOs
Raw-store write and copy routes SHALL use dedicated request/result DTOs while preserving existing fields, validation and persistence/invalidation behavior.
#### Scenario: Write or copy raw data
- **WHEN** a valid raw mutation is submitted
- **THEN** existing bytes and response fields are preserved, including null source IDs for writes
#### Scenario: Invalid input
- **WHEN** malformed base64 or invalid IDs are submitted
- **THEN** the existing validation response is returned without changing cache data
