# type-endpoint-dtos Specification

## Purpose
Keep Cache API type REST contracts on DTOs specific to each operation.

## Requirements

### Requirement: Type REST contracts are operation-specific DTOs
JSON type routes SHALL use explicitly named DTO contracts for their read results, body requests and mutation results. Item reads and updates SHALL use separate DTOs, with derived fields limited to the read DTO.
#### Scenario: Read or mutate a type
- **WHEN** an existing type route is used
- **THEN** its DTO preserves the existing JSON fields, validation and persistence behavior
#### Scenario: Collection or image
- **WHEN** a range, search or binary image route is used
- **THEN** collections retain their DTO elements and existing envelope shape, and images retain their binary response
