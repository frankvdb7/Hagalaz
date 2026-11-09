## ADDED Requirements

### Requirement: Region atmosphere lighting

The map preview SHALL initially use the selected anchor region's decoded atmosphere light color, intensity, direction and response when available.

#### Scenario: Region supplies atmosphere lighting

- **WHEN** the selected anchor region contains a supported lighting record
- **THEN** the map render response includes its lighting values and the viewer applies them to the scene

#### Scenario: Region has no usable atmosphere lighting

- **WHEN** the lighting record is absent or malformed
- **THEN** renderable terrain remains available and the viewer uses its existing lighting defaults

#### Scenario: Compare lighting modes

- **WHEN** the user switches between region atmosphere and default lighting in the viewport toolbar
- **THEN** the viewer updates the shading while preserving the camera and current region selection
