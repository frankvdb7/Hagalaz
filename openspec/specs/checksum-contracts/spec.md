# checksum-contracts Specification

## Purpose
Keep Cache API checksum responses on dedicated DTOs.

## Requirements

### Requirement: Checksum responses use dedicated DTOs
The checksum table route SHALL return a dedicated table DTO containing ordered entry DTOs with unchanged index IDs, CRC values, versions and base64 digests.
#### Scenario: Read checksum table
- **WHEN** the checksum table is requested
- **THEN** populated and empty indexes retain their existing JSON representation and ordering
