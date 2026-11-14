## ADDED Requirements

### Requirement: Underwater terrain layer

The map preview SHALL draw a region's underwater terrain and objects beneath its surface when the region has underwater data, and SHALL let the user hide them.

#### Scenario: Region has underwater data

- **WHEN** the anchor or neighbouring selected region has a `um` archive
- **THEN** its floors render at the surface height plus the decoded depth, and its `ul` objects render on that layer

#### Scenario: Region has no underwater data

- **WHEN** the `um` archive is absent
- **THEN** the region renders as before

#### Scenario: Hide the layer

- **WHEN** the user clears the underwater terrain option
- **THEN** the layer's floors and objects are removed without reloading the region

#### Scenario: Hide the water surface

- **WHEN** the user clears the water surface option
- **THEN** floors whose material uses a water effect (2, 4, 5, 8 or 9) are omitted from the surface planes, revealing the layer beneath, while other floors remain

### Requirement: Underwater depth fog

The map preview SHALL fade underwater floors towards the fog colour of the tile's surface overlay using `(depth - 1) * 256 / scale + bias / 255`.

#### Scenario: Saturated fog

- **WHEN** every corner of an underwater floor triangle reaches full fog
- **THEN** the triangle draws the flat fog colour without its texture

#### Scenario: No surface overlay

- **WHEN** an underwater tile has no surface overlay
- **THEN** the floor is drawn without fog
