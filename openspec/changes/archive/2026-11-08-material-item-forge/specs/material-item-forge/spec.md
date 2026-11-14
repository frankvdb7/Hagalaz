## ADDED Requirements

### Requirement: Material item editor presentation
The admin Item Forge SHALL use existing Angular Material controls and admin theme tokens while preserving its editor and inspector layout.

#### Scenario: Edit an item
- **WHEN** an administrator opens an item definition
- **THEN** outlined Material fields show the ID, name, base value and readonly description state
- **AND** Material cards and chips group editor panels and metadata
- **AND** existing validation, write confirmation and pending-change handling remain effective

#### Scenario: Inspect an item
- **WHEN** an administrator changes the Material zoom slider or expands the model inspector
- **THEN** the existing sprite zoom updates or the existing model preview is revealed
- **AND** tabs, clipboard and navigation retain their existing operations

### Requirement: Unified admin Material theme
The admin application SHALL use one shared owner for Material control colors, shapes, density and feedback, with page styles limited to contextual layout and specialized preview rendering.

#### Scenario: Navigate between admin tools
- **WHEN** an administrator navigates between overview, Cache Studio, Item Forge, Sprite Studio and definition details
- **THEN** cards, fields, buttons, tables and expansion panels use the shared admin Material defaults
- **AND** page-specific grids and preview behavior remain intact

#### Scenario: Sign in
- **WHEN** the sign-in form is displayed
- **THEN** Material form-field labels identify the email and password inputs without positioned substitute labels
- **AND** existing validation and authentication behavior remain unchanged
