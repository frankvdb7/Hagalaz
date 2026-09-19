## Purpose

Ensures custom NPC cleanup can remove spawned NPCs through the synchronous
game-world lifecycle path without an async blocking bridge.

## ADDED Requirements

### Requirement: Handle-based NPC unregister is synchronous

The NPC handle unregister operation SHALL invoke the synchronous NPC service
and store path and SHALL return only after destruction and global-store removal
have been attempted.

#### Scenario: NPC cleanup is requested by a synchronous caller

- **WHEN** custom NPC cleanup requests unregister from a synchronous game-world
  callback
- **THEN** the handle invokes synchronous destruction and store removal
- **AND** it does not wait on an asynchronous operation or use a blocking async
  bridge

#### Scenario: Asynchronous NPC cleanup remains available

- **WHEN** delayed death or failed-respawn cleanup uses the asynchronous NPC
  service
- **THEN** the existing asynchronous destruction and store removal path remains
  available and unchanged
