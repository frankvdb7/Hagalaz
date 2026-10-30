# Cache model viewing

## Purpose

Allow administrators to inspect cache models and their definition-specific appearances and animations.

## ADDED Requirements

### Requirement: Administrators can inspect cache models
The admin site SHALL display raw models and item, NPC and object model definitions with their available textures and verified rendering effects.

#### Scenario: Model selection
- **WHEN** an administrator selects an available model or definition
- **THEN** its geometry and supported appearance are displayed with interactive camera controls

#### Scenario: Item preview
- **WHEN** an item has an inventory model
- **THEN** Item Forge displays that model with its applicable appearance overrides

### Requirement: Associated animations can be played
The viewer SHALL offer available animations associated with the selected definition and playback controls.

#### Scenario: Animation selection
- **WHEN** an administrator selects an available associated animation
- **THEN** the model plays the animation and can be paused

### Requirement: Unsupported data is visible
The viewer SHALL identify missing assets and unsupported or unverified formats and effects without presenting them as faithful rendering.

#### Scenario: Unavailable data
- **WHEN** an asset or required rendering feature cannot be loaded or supported
- **THEN** the viewer reports the relevant limitation and allows another selection

### Requirement: Rendering components are reusable
The model viewing component SHALL be usable by both web applications independently of admin routes and authentication.

#### Scenario: Shared consumer
- **WHEN** either web application supplies render data to the component
- **THEN** the component can display it without depending on admin services

### Requirement: Cache rendering data is protected
Rendering endpoints SHALL follow the existing cache authorization policy and expose decoded renderer-neutral data without changing existing API behavior.

#### Scenario: Unauthorized request
- **WHEN** a client without the required authorization requests rendering data
- **THEN** access is denied
