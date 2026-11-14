# map-editor-live-painting Specification

## Purpose
Defines how quickly and how visibly terrain brushes respond while an administrator paints in the map editor.

## Requirements

### Requirement: Show a stroke while dragging

The map editor SHALL show the effect of a terrain stroke on the brushed tiles while the pointer is moving, before the stroke is released.

#### Scenario: Dragging a height brush

- **WHEN** the user drags a raise, lower or flatten brush
- **THEN** the brushed tiles appear at their resulting height during the drag

#### Scenario: Dragging a floor brush

- **WHEN** the user drags an underlay or overlay brush
- **THEN** the brushed tiles show that floor during the drag

#### Scenario: Clearing an overlay

- **WHEN** the user drags the clear overlay brush
- **THEN** the brushed tiles show their ground without the overlay during the drag

#### Scenario: Cancelling a stroke

- **WHEN** the pointer interaction is cancelled before release
- **THEN** the stroke's preview disappears and no edit is made

#### Scenario: Handing over on release

- **WHEN** the stroke is released
- **THEN** the preview stays until the finished scene is drawn, with no visible gap

### Requirement: Keep painting responsive

Terrain brushes SHALL update during a drag within one frame at the largest brush size on a single loaded region.

#### Scenario: Largest brush on a large region

- **GIVEN** region 12850 is loaded and the largest brush is selected
- **WHEN** the pointer moves during a stroke
- **THEN** each overlay update takes under 16 ms

### Requirement: Only rebuild what a stroke changed

Releasing a stroke SHALL NOT rebuild or re-upload geometry the stroke did not change.

#### Scenario: Releasing a paint stroke

- **GIVEN** region 12850 is loaded
- **WHEN** a floor stroke is released
- **THEN** objects are not rebuilt or re-uploaded, and the ground is drawn within 250 ms

#### Scenario: Releasing a height stroke

- **GIVEN** region 12850 is loaded
- **WHEN** a height stroke is released
- **THEN** only objects on changed ground are re-fitted, and ground and objects are drawn within 350 ms

### Requirement: Preserve the finished scene

Separating terrain from objects MUST NOT change how the finished scene looks or how the editor's scene tools behave.

#### Scenario: Translucent surfaces

- **WHEN** a region with water and translucent objects is drawn
- **THEN** those surfaces overlap as they did before the change

#### Scenario: Selecting and placing objects

- **WHEN** the user clicks an object or drops one on the ground
- **THEN** the same object is selected or the same tile is used as before the change

#### Scenario: Scene display and capture

- **WHEN** the user toggles wireframe, undoes a stroke or saves an image
- **THEN** ground and objects are both included as before the change

#### Scenario: Other model views

- **WHEN** a single model is shown in the model preview
- **THEN** it renders as it did before the change
