## Purpose

Ensure that a character is not exposed to the game world until the complete set of regions visible at the initial location has finished loading.

## ADDED Requirements

### Requirement: Initial world entry waits for all visible regions

The system MUST rebuild the character's initial viewport and wait for every visible region to report complete readiness before character registration continues and before the startup map/entity update is sent.

#### Scenario: All visible regions load successfully

- **WHEN** world sign-in begins for a hydrated character
- **THEN** the system MUST request all regions in the initial viewport through the existing map-region scheduler
- **AND** MUST wait until every requested region has completed clipping, static and custom object, ground-item, and NPC population
- **AND** MUST then continue character registration and startup map delivery
- **AND** retained region references MUST be refreshed to their canonical
  instances before scheduling or waiting

#### Scenario: Entry remains pending while a visible region loads

- **WHEN** at least one initial visible region is still loading
- **THEN** character registration MUST remain pending
- **AND** no startup map or entity update MUST be sent for that character

### Requirement: Initial region-load failure aborts entry cleanly

The system MUST treat a failure to complete any initial visible-region load as a terminal world-entry failure for that attempt.

#### Scenario: A visible region fails to become ready

- **WHEN** a scheduled initial visible-region load fails or completes without publishing readiness
- **THEN** world entry MUST stop without retrying the failed region
- **AND** the normal disconnect/sign-out cleanup owner MUST run
- **AND** the underlying client session MUST be disconnected cleanly

#### Scenario: Entry failure does not publish world presence

- **WHEN** initial region loading fails
- **THEN** the system MUST NOT publish successful world sign-in or contact initialization messages

### Requirement: Existing asynchronous map ownership is preserved

The system MUST use the existing single-reader map-region scheduler as the sole owner of asynchronous region loading and MUST NOT add an additional worker, queue, retry loop, or script-facing asynchronous map-update API.

#### Scenario: Regions are already ready

- **WHEN** an initial visible region is already loaded
- **THEN** the entry barrier MUST treat it as complete without invoking another load

#### Scenario: Normal map updates continue to use the synchronous API

- **WHEN** a character performs a later viewport update after entry
- **THEN** the existing synchronous map-update contract MUST remain unchanged

#### Scenario: Startup map delivery reuses the entry viewport

- **WHEN** world entry has already rebuilt the initial viewport for the
  readiness barrier
- **THEN** the startup map update MUST reuse that visible-region set
- **AND** it MUST NOT rebuild the viewport a second time before sending the
  initial map
