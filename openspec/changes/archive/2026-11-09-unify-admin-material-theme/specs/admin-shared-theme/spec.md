## ADDED Requirements

### Requirement: Shared admin visual treatment
The admin application SHALL present Material cards, outlined fields, and primary actions consistently on login, signed-in pages, and overlays.

#### Scenario: Navigate admin pages
- **WHEN** an administrator views login or a signed-in page
- **THEN** shared controls use the same dark surface, rounded shapes, and amber primary action treatment

#### Scenario: Invalid or pending form
- **WHEN** an admin form is invalid or pending
- **THEN** errors and disabled actions remain visually distinguishable and the existing form behavior is preserved
