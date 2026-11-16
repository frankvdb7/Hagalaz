## MODIFIED Requirements

### Requirement: Save terrain edits to the cache

The map editor SHALL review terrain changes in the existing publish flow. The review SHALL show affected region, source plane, coordinates and before and after values for each changed tile field. The editor SHALL submit only confirmed changed tiles.

#### Scenario: Saving terrain and objects

- **GIVEN** both kinds of edits in one region
- **WHEN** the administrator confirms the review
- **THEN** the editor submits terrain before landscape and reloads successfully saved data

#### Scenario: A tile returned to its original state

- **WHEN** a tile's edits leave it equal to its stored values
- **THEN** the tile is omitted from the review and write

#### Scenario: Failure keeps edits

- **WHEN** a request fails after a terrain request succeeded
- **THEN** the editor reports the separate outcomes, reloads confirmed terrain and keeps unsaved edits available without resending the confirmed terrain

#### Scenario: Unsaved flags warning

- **GIVEN** edits that change heights without changing flags
- **WHEN** the administrator is asked to confirm
- **THEN** the review states that walkability and other tile flags are not changed
