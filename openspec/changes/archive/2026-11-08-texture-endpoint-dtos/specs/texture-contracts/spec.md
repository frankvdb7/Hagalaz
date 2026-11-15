## ADDED Requirements
### Requirement: Texture JSON uses dedicated DTOs
Material and HDR routes SHALL return dedicated DTOs preserving every material field and HDR float value without exposing decoder records.
#### Scenario: Texture inspection
- **WHEN** the admin requests material or HDR data
- **THEN** it consumes matching local DTOs and retains existing rendering behavior
#### Scenario: PNG image
- **WHEN** the PNG route is requested
- **THEN** its binary content, alpha handling and texture metadata headers remain unchanged
