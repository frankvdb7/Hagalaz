# Spec Delta

## MODIFIED Requirements

### Requirement: Region availability is reported independently for terrain and landscape

The render API and workspace MUST distinguish absent terrain, absent landscape, landscape decode failures and request failures for each selected region. The render API accepts optional per-region XTEA keys, which MUST contain exactly four signed 32-bit integers and MUST remain outside URLs and persistent browser storage. Unavailable landscape MUST NOT prevent available terrain from rendering. A stored terrain, landscape, underwater terrain or underwater landscape file that cannot be decoded, for any corrupt-data reason, MUST be reported for that file with a warning and MUST NOT fail the region request.

#### Scenario: Terrain exists but landscape needs keys
- **GIVEN** terrain is readable and landscape cannot be decoded without the region's keys
- **WHEN** the region is loaded without working keys
- **THEN** available terrain remains visible
- **AND** landscape is reported as unavailable
- **AND** the region is not reported as a successfully decoded empty landscape

#### Scenario: Correct keys are supplied
- **GIVEN** the render API receives four valid keys for an affected region
- **WHEN** the region is retried
- **THEN** the server decrypts the landscape container before decoding its placements
- **AND** successful placements replace the previous unavailable result without duplication
- **AND** the keys do not enter the address bar or persistent browser storage

#### Scenario: Key input is malformed
- **WHEN** supplied keys have the wrong length, fractional values or values outside signed 32-bit bounds
- **THEN** input validation identifies the affected region
- **AND** the API does not start decoding that invalid input

#### Scenario: A neighboring region or required border is absent
- **GIVEN** part of a selected rectangle or its authoritative terrain border is unavailable
- **WHEN** the scene finishes loading its available data
- **THEN** the affected region or incomplete boundary is identified
- **AND** other valid regions remain viewable
- **AND** the missing data is not silently replaced with flat terrain, repeated edge heights or generated objects

#### Scenario: Landscape data is malformed
- **GIVEN** terrain is readable and the stored landscape decodes to a placement outside the four source planes
- **WHEN** the region is rendered
- **THEN** the region request succeeds with its terrain
- **AND** landscape is reported as failed with a warning naming the region
