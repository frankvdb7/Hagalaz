## ADDED Requirements

### Requirement: Browse cached widgets

Cache Studio SHALL provide a Widgets tab in the existing cache tab row and a paginated table of the widget IDs present in the cache.

#### Scenario: Browse widgets
- **WHEN** an administrator opens Widgets
- **THEN** the table lists existing widgets and shows the number of components in each widget
- **AND** the administrator can choose how many widgets to show per page

### Requirement: Inspect a cached widget

Cache Studio SHALL provide a direct URL for each widget and display the widget ID and existing component IDs.

#### Scenario: Open a widget from the table
- **WHEN** an administrator selects a widget
- **THEN** its detail page opens, Widgets remains selected, and returning to the table restores the page and page size

#### Scenario: Open an absent widget
- **WHEN** an administrator opens a detail URL for an absent widget
- **THEN** Cache Studio shows a missing-widget message with a link to the table

#### Scenario: Inspect a component
- **WHEN** an administrator selects a component in the hierarchy or preview
- **THEN** the inspector displays its properties and the preview highlights it
- **AND** changes to supported layout and visibility fields update the local preview

#### Scenario: Preview text with a cached font
- **WHEN** a visible text component references an available cache font
- **THEN** the preview uses that font's glyphs and advances with the component's alignment, spacing, and shadow

#### Scenario: Preview a cached model component
- **WHEN** a visible model component references an available cached model
- **THEN** the preview draws it within its widget bounds using the authored camera when present
- **AND** the inspector links to the model's interactive view

#### Scenario: Update a live model preview
- **WHEN** a model camera is edited or a cached model animation advances
- **THEN** WebGL updates the visible model inside its widget bounds
- **AND** the PNG preview remains available when WebGL is unavailable
