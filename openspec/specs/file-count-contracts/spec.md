# file-count-contracts Specification

## Purpose
Keep Cache API file-count operations on dedicated DTOs.

## Requirements

### Requirement: File-count operations use dedicated DTOs
File-count and sub-file-count routes SHALL return their own DTOs while retaining the count JSON field and existing counting/validation behavior.
#### Scenario: Count files or members
- **WHEN** an existing count route is requested
- **THEN** its count and JSON representation remain unchanged
#### Scenario: Invalid ID
- **WHEN** a negative index or file ID is submitted
- **THEN** the route retains its validation response
