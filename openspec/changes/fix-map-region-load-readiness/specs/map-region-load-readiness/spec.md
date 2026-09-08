## Purpose

Expose map-region collision data only after the region has been populated successfully, so movement cannot traverse unloaded or partially loaded world geometry and failed loads can recover through the existing scheduler.

## ADDED Requirements

### Requirement: Region readiness is published after complete population

The system MUST keep a map region not ready for movement collision queries until terrain collision, static map objects, database-backed objects, ground items, and NPC population have completed successfully.

#### Scenario: Collision is queried while a region is loading

- **WHEN** movement or pathfinding queries a tile in a region whose population is still in progress
- **THEN** the query MUST fail closed so movement cannot enter or traverse the not-ready region

#### Scenario: Region population completes successfully

- **WHEN** all required region population steps complete without error
- **THEN** the region MUST become ready and movement collision queries MUST return the populated collision flags

#### Scenario: Static collision is the final population step

- **WHEN** static map objects are still being decoded or their collision flags are still being applied
- **THEN** the region MUST remain not ready even if earlier population steps have completed

### Requirement: Failed and canceled loads remain recoverable

The system MUST leave a region not ready when population fails or is canceled, and a later load request MUST be able to attempt loading that region again.

#### Scenario: Population fails before completion

- **WHEN** a region loader throws an unexpected exception during population
- **THEN** the region MUST remain not ready and the scheduler MUST release its in-flight admission so a later request can retry

#### Scenario: Population is canceled during shutdown

- **WHEN** host cancellation interrupts a region load before completion
- **THEN** the region MUST remain not ready and no successful-ready state MUST be published

#### Scenario: A later request retries a failed region

- **WHEN** a region previously failed to load and a later request is submitted while the scheduler is running
- **THEN** the loader MUST be invoked again for that region

### Requirement: In-flight loading remains single-owned

The system MUST continue to deduplicate concurrent requests for the same region and MUST use the existing region-load scheduler as the sole owner of asynchronous loading.

#### Scenario: Duplicate requests arrive during loading

- **WHEN** multiple map updates request the same region before its current load completes
- **THEN** exactly one load operation MUST be in flight for that region

#### Scenario: A ready region is requested again

- **WHEN** a map update requests a region whose complete population has already succeeded
- **THEN** the scheduler MUST NOT invoke the loader again
