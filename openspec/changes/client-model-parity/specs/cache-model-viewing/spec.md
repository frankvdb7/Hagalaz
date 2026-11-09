## MODIFIED Requirements

### Requirement: Administrators can inspect cache models
The admin site SHALL display raw models and item, NPC and object model definitions with their available textures and verified rendering effects.

#### Scenario: Model selection
- **WHEN** an administrator selects an available model or definition
- **THEN** its geometry and supported appearance are displayed with interactive camera controls

#### Scenario: Item preview
- **WHEN** an item has an inventory model
- **THEN** Item Forge displays that model with its applicable appearance overrides

#### Scenario: Inverted object preview
- **GIVEN** an inverted object with supported geometry
- **WHEN** its standalone model or preview image is requested
- **THEN** the preview shows the client-mirrored geometry with correct winding and lighting

### Requirement: Associated animations can be played
The viewer SHALL offer available animations associated with the selected definition and playback controls.

#### Scenario: Animation selection
- **WHEN** an administrator selects an available associated animation
- **THEN** the model plays the animation and can be paused

#### Scenario: Client animation transforms
- **WHEN** a measured animation exercises a verified face or attachment transform
- **THEN** playback applies the transform to its selected groups and lights any changed face colors

#### Scenario: Tweened and stepped playback
- **WHEN** playback advances between frames
- **THEN** tweened sequences follow the client's interpolation, flags and frame delays, and non-tweened sequences hold the current pose until the next frame

### Requirement: Unsupported data is visible
The viewer SHALL identify missing assets and unsupported or unverified formats and effects without presenting them as faithful rendering.

#### Scenario: Unavailable data
- **WHEN** an asset or required rendering feature cannot be loaded or supported
- **THEN** the viewer reports the relevant limitation and allows another selection

#### Scenario: Material fidelity gaps
- **WHEN** a referenced material contains an unverified effect, UV scrolling or HDR data
- **THEN** the viewer identifies the affected texture and limitation while retaining decoded metadata

#### Scenario: Unsupported transform
- **WHEN** an unsupported transform occurs in an animation that the client would continue playing
- **THEN** the viewer warns about the transform and continues the supported playback
