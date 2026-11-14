# model-contracts Specification

## Purpose
Keep Cache API model asset responses and the admin model requests on dedicated DTOs.

## Requirements

### Requirement: Model assets use dedicated DTOs
Model endpoints SHALL return dedicated DTOs for geometry, appearances, animation sequences/frames/skeletons/transforms and particle settings without exposing cache decoder types.
#### Scenario: Asset read
- **WHEN** an existing model or animation route is read
- **THEN** its existing JSON shape, geometry/animation/particle values and error behavior remain compatible

### Requirement: Admin model requests use local DTOs
The admin SHALL use matching local DTOs for model HTTP responses while shared renderer contracts remain independent.
#### Scenario: Model preview
- **WHEN** the preview loads model and animation data
- **THEN** it passes compatible rendering inputs without using renderer types as REST contracts
