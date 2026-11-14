# cache-studio-resource-navigation Specification

## Purpose
Provide one navigation surface for choosing a cache type in the admin Cache Studio.

## Requirements

### Requirement: Single cache-type navigation
Cache Studio SHALL display one outlined Material tab row with the existing definition types and Models. It SHALL NOT display a separate Definitions/Models/Sprites row.

#### Scenario: Open a type
- **WHEN** an administrator selects Items, NPCs, Sprites or another definition type
- **THEN** the existing browser opens for that type and only that type tab is selected

#### Scenario: Open Models
- **WHEN** an administrator selects Models in the same tab row
- **THEN** the existing model viewer opens with shared Material search and result cards
- **AND** loading a model and reloading the same model retain their existing behavior

#### Scenario: Open a detail or reject navigation
- **WHEN** a definition detail is open
- **THEN** its path type is selected in the single row
- **AND** rejecting navigation through the existing dirty-draft guard retains the current route and selected tab
