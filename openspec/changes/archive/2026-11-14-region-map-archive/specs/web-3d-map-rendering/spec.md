# Spec Delta

## ADDED Requirements

### Requirement: Corrupt region map files are reported per file

The render API MUST report a stored terrain, landscape, underwater terrain or underwater landscape file that cannot be decoded, for any corrupt-data reason, for that file with a warning, and MUST NOT fail the region request because of it.

#### Scenario: Landscape data is malformed
- **GIVEN** terrain is readable and the stored landscape decodes to a placement outside the four source planes
- **WHEN** the region is rendered
- **THEN** the region request succeeds with its terrain
- **AND** landscape is reported as failed with a warning naming the region
