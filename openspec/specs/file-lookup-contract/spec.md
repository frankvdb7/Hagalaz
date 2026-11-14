# file-lookup-contract Specification

## Purpose
Keep the Cache API file lookup operation on a dedicated DTO.

## Requirements

### Requirement: File lookup uses a dedicated DTO
The file-name lookup route SHALL return GetFileIdDto with the existing fileId field and preserve lookup, missing-name and validation behavior.
#### Scenario: Name lookup
- **WHEN** a file name is queried
- **THEN** its mapped ID or the existing -1 missing-name value is returned
