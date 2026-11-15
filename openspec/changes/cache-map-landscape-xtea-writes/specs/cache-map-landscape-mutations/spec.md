## MODIFIED Requirements

### Requirement: Only replace landscapes that can be read

The Cache service MUST NOT overwrite a region's landscape unless its stored archive exists and decodes with the keys supplied for the save (none when none are supplied), because unread placements would otherwise be lost. Keys are never guessed or defaulted.

#### Scenario: Region has no landscape archive

- **WHEN** a save targets a region with no landscape archive
- **THEN** the service responds with not found and writes nothing

#### Scenario: Stored landscape cannot be decoded

- **GIVEN** a region whose landscape is corrupt, or is encrypted and the save supplies no keys or wrong keys
- **WHEN** a save targets that region
- **THEN** the service responds that the landscape cannot be replaced and writes nothing

#### Scenario: Uncompressed protected landscape

- **GIVEN** a save that supplies keys for a stored landscape that is not compressed
- **WHEN** the save is attempted
- **THEN** the service responds that the landscape cannot be replaced and writes nothing, because the keys cannot be confirmed

## ADDED Requirements

### Requirement: Save protected landscapes with their keys

The Cache service SHALL, when a save supplies four XTEA keys, read the stored landscape with them and store the replacement encrypted with them, so that reading with the same keys returns the submitted placements. The file's reference-table version SHALL advance and its CRC and digest SHALL describe the encrypted bytes as stored.

#### Scenario: Saving a protected landscape

- **GIVEN** a region whose landscape is encrypted with known keys
- **WHEN** an administrator submits placements with those keys
- **THEN** reading the landscape with those keys returns the submitted placements
- **AND** reading it without keys or with other keys fails
- **AND** the stored CRC equals the CRC of the stored bytes

#### Scenario: Unusable keys

- **WHEN** a save supplies keys that are not exactly four integers, or are all zero
- **THEN** the service responds with a validation error and reads and writes nothing
