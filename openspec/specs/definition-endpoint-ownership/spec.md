# definition-endpoint-ownership Specification

## Purpose
Give each cache definition type one endpoint file that owns its routes.

## Requirements

### Requirement: Definition endpoint files own each type
Each definition endpoint file SHALL own its type's read and mutation routes and mappings, while the type route-group registration remains a composition root.
#### Scenario: Existing route
- **WHEN** an existing type route is invoked
- **THEN** its route, DTO, validation, authorization and cache behavior remain unchanged through its named definition endpoint owner
