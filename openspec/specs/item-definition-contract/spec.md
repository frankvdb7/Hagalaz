# item-definition-contract Specification

## Purpose
Define the DTO through which the Cache API reads and replaces full item definitions.

## Requirements

### Requirement: Item definition REST uses a DTO
The full item definition API SHALL accept and return explicit DTOs, preserving stored fields and existing derived response fields without exposing cache implementation metadata.
#### Scenario: Full definition save
- **WHEN** a complete DTO is submitted
- **THEN** its stored fields are mapped onto the persisted item and encoded normally while opaque opcode data remains unchanged
#### Scenario: Read definition
- **WHEN** an item definition is requested
- **THEN** the response contains the existing definition JSON fields and no preserved opcode data
