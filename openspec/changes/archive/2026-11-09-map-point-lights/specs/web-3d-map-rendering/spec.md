## ADDED Requirements

### Requirement: Region point light preview

The admin map editor SHALL preview cached surface point lights by default using their position, color, radius, tile footprint and plane coverage. The preview uses steady intensity and radial falloff.

#### Scenario: Lights affect loaded surface geometry

- **GIVEN** a loaded region contains a point light
- **WHEN** point lights are enabled
- **THEN** nearby terrain and objects on covered tiles and planes receive its color, including in an adjacent loaded region
- **AND** lights follow the terrain height at their source tile.

#### Scenario: Preview can be toggled independently

- **WHEN** the operator toggles Point lights in View > Lighting
- **THEN** the preview switches independently of region atmosphere and preserves the camera and selection.
- **AND** the menu shows whether point lights are enabled.

#### Scenario: Terrain data has no usable light records

- **GIVEN** a region has no light records or its terrain trailer is truncated
- **WHEN** the editor loads the region
- **THEN** the terrain remains available and the region contributes no lights.
