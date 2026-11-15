# model-transparency-ordering Specification

## Purpose
Make overlapping transparent cache model faces display in a deterministic order that respects GameClient face priorities in admin previews.

## Requirements

### Requirement: Model previews preserve face priorities
The system SHALL preserve effective per-face priorities from supported cache model formats through standalone and assembled model previews. A uniform priority SHALL apply to every face in its source part.

#### Scenario: Uniform and per-face priorities
- **GIVEN** supported legacy and newer models with uniform or per-face priorities
- **WHEN** their geometry is requested for preview
- **THEN** each face retains its effective source priority

#### Scenario: Assembled parts have different defaults
- **GIVEN** an item, NPC or object model assembled from parts with different default priorities
- **WHEN** the assembled model is previewed
- **THEN** faces retain the priority of their original part and their material and particle associations

### Requirement: Transparent faces respect client priorities
The viewer SHALL render opaque and cutout faces with depth writes before blended faces. Blended faces SHALL follow ascending GameClient face priority, then camera-space back-to-front depth within a priority, then source face order for ties. Face alpha and blended texture alpha SHALL both select the blended pass. This behavior SHALL NOT claim complete client renderer parity.

#### Scenario: Priority overrides geometric depth
- **GIVEN** overlapping blended faces whose priorities conflict with their geometric depth
- **WHEN** the model is rendered
- **THEN** lower-priority faces are submitted before higher-priority faces, including priorities 10 and 11

#### Scenario: Equal priorities follow camera depth
- **GIVEN** overlapping blended faces with equal priorities
- **WHEN** the camera orbits to the opposite side
- **THEN** their submission order changes to keep farther faces before nearer faces
- **AND** equal-depth ties retain source face order

#### Scenario: Mixed transparency sources
- **GIVEN** opaque, cutout, texture-alpha-blended and face-alpha-blended faces
- **WHEN** the model is rendered
- **THEN** only blended faces participate in the transparent ordering pass
- **AND** the blended pass depth-tests without writing depth

### Requirement: Ordering tracks current model state
The viewer SHALL update transparency ordering after geometry, camera or face-alpha changes without changing source topology, UVs, material identity or particle face attachments. Replacing or disposing a model SHALL discard its ordering state.

#### Scenario: Animation changes depth and transparency
- **GIVEN** a model animation that moves faces and changes face alpha
- **WHEN** a new frame is shown
- **THEN** face ordering and pass membership reflect that frame
- **AND** texture and particle attachments still refer to the same source faces

#### Scenario: Model replacement and disposal
- **GIVEN** a model with transparent faces
- **WHEN** it is replaced or the viewer is destroyed
- **THEN** its ordering data is released with its viewer resources
- **AND** a replacement model uses only its own faces and priorities
