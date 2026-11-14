# archive-contracts Specification

## Purpose
Keep Cache API archive read responses on dedicated DTOs instead of shared or internal models.

## Requirements

### Requirement: Archive reads use dedicated DTOs
The archive read endpoint SHALL return a dedicated archive DTO containing ordered entry DTOs with unchanged file IDs, base64 payloads and lengths.
#### Scenario: Read archive
- **WHEN** an existing archive is requested
- **THEN** the existing JSON shape and member order are preserved, including empty members
