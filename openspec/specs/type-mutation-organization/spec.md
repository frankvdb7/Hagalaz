# type-mutation-organization Specification

## Purpose
Organize Cache API type mutation endpoints by the definition type that owns them.

## Requirements

### Requirement: Endpoint ownership follows definition types
Each definition type SHALL own its mutation mappings and handlers while sharing the existing persistence implementation.
#### Scenario: Existing mutation request
- **WHEN** an existing item, NPC, object, sprite, varp-bit or config-definition route receives a request
- **THEN** the same route, JSON contract, validation, authorization and cache write behavior apply through its type-specific endpoint owner
