# sprite-tool-retirement Specification

## Purpose
Limit the admin app to the current sprite tools after retiring the legacy sprite manager.

## Requirements

### Requirement: Current sprite tools only
The admin application SHALL expose the current sprite browser and Sprite Studio without the legacy sprite manager page or its dedicated metadata/search operations.

#### Scenario: Open Sprites
- **WHEN** an administrator selects Sprites in Cache Studio
- **THEN** the current browser opens at `/cache/types?kind=sprites`
- **AND** only Sprites is selected in resource navigation

#### Scenario: Open a sprite
- **WHEN** the current sprite browser opens a sprite detail
- **THEN** the current Sprite Studio is displayed with Sprites selected
- **AND** existing preview, replacement and unsaved-draft protection remain effective
