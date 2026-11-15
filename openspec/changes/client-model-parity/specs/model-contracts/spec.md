## MODIFIED Requirements

### Requirement: Model assets use dedicated DTOs
Model endpoints SHALL return dedicated DTOs for geometry, appearances, animation sequences/frames/skeletons/transforms and particle settings without exposing cache decoder types.
#### Scenario: Asset read
- **WHEN** an existing model or animation route is read
- **THEN** its existing JSON shape, geometry/animation/particle values and error behavior remain compatible except for supported inverted object previews

#### Scenario: Additional model attachment data
- **WHEN** an asset contains a verified effector or animated attachment
- **THEN** the model response exposes it through additive dedicated DTO fields with indices valid for the returned geometry
