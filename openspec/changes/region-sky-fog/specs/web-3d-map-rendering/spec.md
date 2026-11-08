## ADDED Requirements

### Requirement: Region sky and fog

The map preview SHALL use the selected anchor region's decoded sky colour as the viewport background while region atmosphere is active, and SHALL offer linear fog derived from the region's raw fog value.

#### Scenario: Region supplies sky and fog

- **WHEN** the selected anchor region contains a lighting record with sky colour or fog flags
- **THEN** the map render response includes the sky colour and raw fog value, and the viewer clears to that colour

#### Scenario: Lighting record omits sky and fog

- **WHEN** the lighting record does not set the sky colour or fog flags
- **THEN** the response carries the client defaults `0xC8C0A8` and `0`

#### Scenario: Preview fog

- **WHEN** the user enables fog while region atmosphere is active
- **THEN** geometry fades linearly to the sky colour between `14332 - ((fogValue + 256) << 2)` and `14332` scene units of eye depth

#### Scenario: Default lighting

- **WHEN** region atmosphere is unavailable or the user selects default lighting
- **THEN** the viewer keeps its existing background and draws no fog
