# Spec Delta

## MODIFIED Requirements

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

#### Scenario: Dragging a flag brush

- **WHEN** the user drags a set-flag or clear-flag brush, with the walkability overlay on or off
- **THEN** the brushed tiles are marked during the drag, in that flag's colour when setting and in a pale wash when clearing

#### Scenario: Cancelling a stroke

- **WHEN** the pointer interaction is cancelled before release
- **THEN** the stroke's preview disappears and no edit is made

#### Scenario: Handing over on release

- **WHEN** the stroke is released
- **THEN** the preview stays until the finished scene is drawn, with no visible gap
