## Purpose

Keep failed or canceled map-region population unpublished and make the same
region instance safe to retry.

## ADDED Requirements

### Requirement: Unpublished region loads can be reset transactionally

The system MUST provide a reset operation for a region that has not committed
loading. The reset MUST remove unpublished NPCs through the NPC service,
destroy unpublished map objects and ground items without gameplay removal
notifications, clear collision, and clear pending and prepared map-part
updates. It MUST leave the region not loaded and not destroyed.

#### Scenario: Population fails after partial NPC and object creation

- **WHEN** a region load fails before `Load()` commits readiness
- **THEN** rollback MUST remove the unpublished population and collision without
  sending gameplay removal or respawn updates

#### Scenario: Population cancellation is observed

- **WHEN** a region load is canceled after population has started
- **THEN** rollback MUST run with non-cancelable cleanup and the original
  cancellation MUST be rethrown

#### Scenario: Rollback also fails

- **WHEN** population fails and reset fails
- **THEN** the loader MUST surface both the original and rollback failures

### Requirement: Reset has explicit lifecycle preconditions

The system MUST reject reset for a loaded region, a destroyed region, or a
region containing characters. A successful reset MUST be safe to call before a
load and MUST permit a later load attempt on the same instance.

#### Scenario: Reset is requested after load commit

- **WHEN** reset is called on a loaded region
- **THEN** reset MUST reject the request without changing the committed region

#### Scenario: Reset is requested with characters present

- **WHEN** reset is called on a region containing characters
- **THEN** reset MUST reject the request without removing those characters

#### Scenario: The same instance is retried

- **WHEN** a failed load is reset and the loader is invoked again for the same
  region instance
- **THEN** the second load MUST be able to commit normally
