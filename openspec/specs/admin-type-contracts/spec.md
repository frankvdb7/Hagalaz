# admin-type-contracts Specification

## Purpose
Define the local DTOs that the admin cache type services use for typed definition reads and updates.

## Requirements

### Requirement: Admin cache type APIs use local DTOs
Admin cache type services SHALL use explicit local request and response DTOs matching backend fields, nullability and JSON types.
#### Scenario: Read a definition
- **WHEN** a typed resource loads a definition, range or search
- **THEN** its DTO type remains available through the shared store feature
#### Scenario: Update item
- **WHEN** the editor saves an item
- **THEN** it submits UpdateItemDefinitionDto stored fields without response-only computed fields
