## Purpose

Keep map-region readiness fail-closed until all required source data has been
prepared and the initial population has completed, while discarding failed
region instances so later requests can create clean replacements.

## ADDED Requirements

### Requirement: Region readiness is published after complete population

The system MUST keep a map region unavailable for movement collision queries
until terrain collision, static map objects, configured game objects, ground
items, and valid NPC population have completed successfully.

#### Scenario: Collision is queried while a region is loading

- **WHEN** movement or pathfinding queries a tile in a region whose population
  is still in progress
- **THEN** the query MUST fail closed so movement cannot enter or traverse the
  not-ready region

#### Scenario: Region population completes successfully

- **WHEN** all required source preparation and population steps complete without
  a fatal error
- **THEN** the region MUST publish readiness only after the final population
  step and movement queries MUST return the populated collision flags

### Requirement: Source preparation precedes region mutation

The system MUST complete all required database queries and static map decoding
before applying their collision, object, or item results to the region.
Static decode callbacks MUST stage local data and MUST NOT mutate the region
during decoding.

#### Scenario: Static decode fails after producing partial callbacks

- **WHEN** static decoding emits collision or object data and then fails
- **THEN** none of that staged data MUST be applied to the region
- **AND** the region MUST NOT publish readiness

### Requirement: Fatal load failures discard the failed instance

An unexpected database, cache, decode, map-apply, cancellation, or shutdown
failure MUST leave the region not ready. The loader MUST attempt to unregister
every NPC successfully registered by that attempt, preserve cleanup failures,
and exact-remove the failed region only if the active service entry is that
same instance. The failed instance MUST NOT be reset or reused.

#### Scenario: Required source preparation fails

- **WHEN** a required source query or map cache read/decode fails
- **THEN** no partially prepared source result is applied to the region
- **AND** the failed region instance MUST be removed/discarded

#### Scenario: Cancellation follows successful NPC registration

- **WHEN** cancellation occurs after one or more NPCs have been registered
  successfully but before readiness is published
- **THEN** all NPCs registered by that attempt MUST be unregistered
- **AND** the region MUST remain not ready and be exact-removed

#### Scenario: A stale failed instance is replaced

- **WHEN** a failed instance R1 is no longer current and a replacement R2 is
  active
- **THEN** cleanup for R1 MUST NOT remove R2

#### Scenario: A later request retries through a fresh instance

- **WHEN** a later request asks for the same region after R1 failed
- **THEN** the service MUST create or return a fresh instance R2
- **AND** R2 MUST NOT be the failed R1 instance

### Requirement: Cache absence is distinct from cache failure

The system MUST treat the cache API's documented `GetFileId == -1` result as a
missing map archive according to existing empty-data semantics. It MUST NOT
convert container-read, decryption, decompression, malformed-container, or
decoder failures into missing data.

#### Scenario: A named map archive is absent

- **WHEN** the cache reports `-1` for a terrain or object archive
- **THEN** that archive MUST follow the existing intentional empty-data path

#### Scenario: A named map archive cannot be decoded

- **WHEN** an existing archive fails to read or decode
- **THEN** the failure MUST propagate as fatal map-load input failure

### Requirement: Isolated NPC content failure does not invalidate valid map data

A non-cancellation failure constructing or registering one configured NPC MUST
be logged with region and NPC identity, cleaned up by the owning registration
service where applicable, and skipped so valid NPC entries can continue. This
exception boundary MUST NOT swallow cancellation or failures from database,
cache, decoder, map apply, or scheduler infrastructure.

#### Scenario: One NPC registration fails

- **WHEN** NPC A registers, NPC B fails at the isolated NPC content boundary,
  and NPC C is valid
- **THEN** A and C MUST remain populated, B MUST be absent, and the region MUST
  be eligible to publish readiness

### Requirement: In-flight loading remains single-owned

The existing `MapRegionLoadScheduler` MUST remain the sole asynchronous loader
and MUST coalesce duplicate requests for one active region instance.

#### Scenario: Duplicate requests arrive during loading

- **WHEN** multiple callers request the same active region before its current
  load completes
- **THEN** exactly one load operation MUST be in flight and all waiters MUST
  observe its completion or failure

#### Scenario: A ready region is requested again

- **WHEN** a region's complete population has committed readiness
- **THEN** the scheduler MUST NOT invoke the loader again
