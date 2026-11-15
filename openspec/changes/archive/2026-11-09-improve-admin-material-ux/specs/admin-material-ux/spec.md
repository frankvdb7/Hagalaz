## ADDED Requirements

### Requirement: Consistent Material administration
The admin application SHALL use Angular Material components and its existing theme for navigation, forms, and feedback, without bespoke decorative skins or CSS variables in templates.

#### Scenario: Navigate between cache tools
- **GIVEN** an authorized administrator
- **WHEN** they navigate between Types and Sprites
- **THEN** the selected page is indicated by Material navigation and its browse and edit operations are discoverable

#### Scenario: Use the admin app on a narrow screen
- **WHEN** the viewport is narrow
- **THEN** controls reflow without page-level horizontal scrolling and result content scrolls within its own region

### Requirement: Clear operation feedback
The cache tools SHALL prevent invalid and duplicate pending submissions and expose loading, errors, empty results, and successful edits accessibly.

#### Scenario: Invalid request
- **WHEN** a required field is empty or a numeric value is outside its allowed range
- **THEN** the relevant field explains the error and the operation cannot issue a request

#### Scenario: Request failure and retry
- **WHEN** an operation fails
- **THEN** an alert displays the error, loading ends, and the administrator can retry the same operation

#### Scenario: Successful request
- **WHEN** a valid operation completes
- **THEN** its result appears in a named result region and successful edits are explicitly announced

#### Scenario: Pending operation
- **WHEN** an operation is pending
- **THEN** the page exposes its busy state and prevents another request until completion
