## ADDED Requirements

### Requirement: Hydration messages stay with the owning process
GameWorld SHALL send hydration requests to a temporary endpoint unique to its process identity and receive saga replies on that endpoint.

#### Scenario: Two worlds hydrate concurrently
- **WHEN** two GameWorld processes request character hydration
- **THEN** each receives its response using its own in-memory saga repository

#### Scenario: A world restarts
- **WHEN** a replacement GameWorld starts with a new process identity
- **THEN** its hydration requests use a new temporary endpoint without requiring the previous repository
