## ADDED Requirements

### Requirement: Guarded shared portal navigation
The admin SHALL provide a shared responsive Material shell for Overview, Cache Studio, Characters, Worlds, Economy, Infrastructure, Audit Logs and Settings, preserving authentication and existing cache URLs.

#### Scenario: Unauthorized navigation
- **WHEN** an unauthenticated visitor opens a portal route
- **THEN** the existing guard redirects to login and no protected page renders

#### Scenario: Mobile navigation
- **WHEN** an authorized administrator selects a destination in the mobile drawer
- **THEN** the route changes and the drawer closes without obscuring the selected page

#### Scenario: Navigation from a scrolled page
- **WHEN** the shared shell activates a different top-level feature page
- **THEN** its content begins at the top and its skip link targets that page's main content

### Requirement: Truthful operations and data
The portal SHALL expose only verified API-backed operations and clearly explain unavailable integrations.

#### Scenario: Metrics are unavailable
- **WHEN** the overview renders without a verified telemetry contract
- **THEN** it states that metrics are not connected and displays no invented counts or charts

#### Scenario: Existing cache workflow
- **WHEN** an administrator searches, inspects or updates a supported definition or sprite
- **THEN** the existing services and store behavior continue to own requests, loading, errors and confirmed results

#### Scenario: Failed cache request
- **WHEN** a cache request fails
- **THEN** an accessible error appears, pending state clears and a valid retry remains available
