## Purpose

Ensures custom NPC cleanup can remove spawned NPCs without blocking the single game-loop thread that also processes player movement and world updates.

## ADDED Requirements

### Requirement: Handle-based NPC unregister is non-blocking

The NPC handle unregister operation SHALL return without waiting for the asynchronous NPC removal operation to finish.

#### Scenario: NPC cleanup is pending

- **WHEN** custom NPC cleanup requests unregister while the NPC service removal operation is still pending
- **THEN** the handle call returns and the game loop remains available for subsequent ticks

#### Scenario: NPC removal completes later

- **WHEN** the scheduled unregister operation completes asynchronously
- **THEN** the existing NPC service performs destruction and store removal exactly as before
