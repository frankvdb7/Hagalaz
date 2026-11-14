## ADDED Requirements

### Requirement: Overlay spreading

The map preview SHALL blend overlays authored to spread into the perimeter of neighbouring tiles according to overlay shape, rotation and priority.

#### Scenario: Neighbour spreads over an underlay

- **WHEN** a spreading overlay covers a perimeter point shared with an underlay tile
- **THEN** that underlay vertex takes the overlay's colour and texture

#### Scenario: Competing overlays

- **WHEN** several spreading overlays claim one perimeter point
- **THEN** the highest priority, then the highest definition ID, owns it, and another overlay's face uses it only if it outranks that face

#### Scenario: Non-spreading overlay

- **WHEN** an overlay does not set the spreading flag
- **THEN** neighbouring tiles are unchanged

### Requirement: Overlay render properties

The map render API SHALL expose the decoded overlay priority, spreading flag and water parameters, using client defaults when opcodes are absent, and SHALL omit them for underlays.

#### Scenario: Overlay omits the opcodes

- **WHEN** an overlay definition sets none of these opcodes
- **THEN** the response carries priority 8, no spreading, fog colour `0x122B3D`, scale 512, bias 255 and parameters 63, 0 and 64
