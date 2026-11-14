# admin-model-workspace Specification

## Purpose
Give administrators a reference-based inspection workspace for actual cache models without implying unsupported model persistence.

## Requirements

### Requirement: Inspect models in a responsive workspace
The admin Models page SHALL retain validated lookup and show loaded geometry in a large viewport with inspection tabs, a texture strip and a properties sidebar derived from the model response.

#### Scenario: Load and reload
- **WHEN** an administrator loads a nonnegative integral model ID and repeats the lookup
- **THEN** the workspace shows that model and reloads it through the existing request owner

#### Scenario: Invalid lookup
- **WHEN** an administrator enters an invalid ID
- **THEN** loading is unavailable and no invalid request is sent

#### Scenario: Data and failure inspection
- **WHEN** an administrator switches inspection tabs or a model, texture or animation request fails
- **THEN** actual data, an explanatory empty state or a retryable error is visible without inventing metadata

### Requirement: Preview controls do not persist cache edits
The workspace SHALL provide grid, wireframe, camera focus and reversible preview transforms. It SHALL clearly identify preview-only values and unavailable save/duplicate operations.

#### Scenario: Preview transform and reset
- **WHEN** an administrator changes valid scale, rotation or offset values and then resets the preview
- **THEN** rendered geometry changes and returns to its original values without modifying the source response or sending a mutation

#### Scenario: Change model or dispose viewer
- **WHEN** the selected model changes or the viewer is destroyed
- **THEN** inspection transforms reset for the new model and the existing request and GPU owners clean up their resources

#### Scenario: Embedded consumer
- **WHEN** another page embeds the existing model preview without workspace mode
- **THEN** its existing appearance, animation controls and failure handling remain available
