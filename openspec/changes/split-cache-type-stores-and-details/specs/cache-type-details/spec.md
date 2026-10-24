## ADDED Requirements

### Requirement: Independent definition state
Each supported cache type SHALL have a dedicated NgRx SignalStore with resource-backed reads, and SHALL expose only its supported list, search and mutation operations.

#### Scenario: Independent types
- **WHEN** an item and NPC definition are loaded
- **THEN** their data and request states remain independent

#### Scenario: New route ID during a pending read
- **WHEN** the route ID changes before the read completes
- **THEN** the stale read is cancelled and only the new ID is displayed

### Requirement: Dedicated detail pages
Each supported kind SHALL have its own guarded detail route and component, with customization owned by that type.

#### Scenario: Inspect definition
- **WHEN** a list definition or numeric query is opened
- **THEN** the matching dedicated page loads that type and ID

#### Scenario: Supported edit
- **WHEN** an administrator submits a valid supported edit and confirms it
- **THEN** the existing endpoint is called once and the server confirmation is displayed

### Requirement: Preserve cache workflows
Archive counts, list pagination and binary sprite editing SHALL remain available through existing endpoints.

#### Scenario: Paginated search
- **WHEN** the next page is selected
- **THEN** the submitted query and selected page size are sent to the existing search endpoint

### Requirement: Integrated sprite images
Sprite detail pages SHALL provide frame previews and confirmed image replacement for the selected ID through existing sprite API endpoints, with recoverable errors while the API is unavailable.

#### Scenario: Preview selected sprite
- **WHEN** a sprite detail is loaded
- **THEN** frame zero is requested and the image controls keep the selected sprite ID read only

#### Scenario: Replace selected sprite image
- **WHEN** the administrator selects a PNG and confirms replacement
- **THEN** the image endpoint receives that file for the selected ID and successful replacement refreshes metadata and frame zero

#### Scenario: API unavailable
- **WHEN** an image request fails
- **THEN** the page displays the failure and permits retry without fabricating success
