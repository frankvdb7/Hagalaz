## ADDED Requirements

### Requirement: Inspect selected terrain tiles

The admin map editor SHALL let administrators select a visible terrain tile with the Select tool and inspect it in the existing Inspector. The Inspector SHALL show the region, world and region-local tile coordinates, plane, height, floor IDs, and terrain flags from the current terrain preview.

#### Scenario: Select terrain and inspect it

- **GIVEN** the Select tool is active and visible terrain is loaded
- **WHEN** the administrator clicks terrain without hitting an object and then hovers over a different tile
- **THEN** the Inspector continues to show the clicked tile
- **AND** the viewport keeps the outline around the clicked tile
- **AND** its details reflect pending terrain edits

#### Scenario: Select an object over terrain

- **GIVEN** a terrain tile is selected
- **WHEN** the administrator clicks a rendered placement
- **THEN** the placement replaces the tile selection and the Inspector shows the object

#### Scenario: Clear tile selection

- **GIVEN** a terrain tile is selected
- **WHEN** the administrator presses Escape, clicks empty space, hides the selected plane, or unloads the tile's region
- **THEN** the Inspector clears the tile details and the viewport removes the selection outline
