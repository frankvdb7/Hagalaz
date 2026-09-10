## Purpose

Keep map-region readiness fail-closed until all required source data has been
prepared and the initial population has completed, while discarding failed
region instances so later requests can create clean replacements.

## ADDED Requirements

### Requirement: Concurrent region creation has one canonical active instance

When multiple callers create the same absent region concurrently, the map
service MUST publish one canonical active instance and return that same
instance to every caller. A losing construction MUST NOT replace or remove
the instance that won the active-dictionary insertion.

#### Scenario: Concurrent callers create one absent region

- **WHEN** multiple callers request creation of the same absent region
- **THEN** every caller MUST receive the exact same canonical region instance
- **AND** the active dimension MUST contain exactly one region for that ID

### Requirement: Region lifecycle has one explicit source of truth

Every map-region instance MUST expose exactly one initial-load lifecycle state:
`Initializing`, `Ready`, or `Discarded`. New instances MUST begin
`Initializing`. The only legal transitions are `Initializing` to `Ready` after
complete successful population, or `Initializing` to `Discarded` after fatal
failure or cancellation. `Ready` and `Discarded` are terminal for this
lifecycle.

#### Scenario: A new region is created

- **WHEN** the map service creates a new region instance
- **THEN** its state MUST be `Initializing`

#### Scenario: A load completes successfully

- **WHEN** all required population completes successfully
- **THEN** the loader MUST publish `Ready` as its final readiness action

#### Scenario: A load fails or is canceled

- **WHEN** initial loading terminates with a fatal failure or cancellation
- **THEN** that exact instance MUST become `Discarded`
- **AND** it MUST NOT transition back to `Initializing` or to `Ready`

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

### Requirement: Failed instances cannot be scheduled or consumed

The load scheduler MUST schedule an `Initializing` region only while that exact
instance remains the canonical region for its ID and dimension. `Ready` regions
are already loaded, and `Discarded` or stale instances MUST be rejected.
Viewport and map-update processing MUST resolve retained stale references to
the canonical current instance where possible. Dynamic/standard map identity
is selected from canonical region metadata independently of readiness. Only
`Ready` regions may contribute normal creatures, full region updates, or world
ticks. An `Initializing` or `Discarded` region MUST never be treated as ready
world state.

#### Scenario: A discarded region is submitted again

- **WHEN** stale code calls `RequestLoad(R1)` after R1 is discarded
- **THEN** the loader MUST NOT be invoked for R1

#### Scenario: An initializing stale region is submitted

- **WHEN** R1 remains `Initializing` but the service currently owns R2 for the
  same region and dimension
- **THEN** the scheduler MUST reject R1

#### Scenario: A viewport retains a failed region

- **WHEN** a viewport retains discarded R1 and the service owns replacement R2
- **THEN** viewport/map-update processing MUST resolve R2 as the current region
- **AND** MUST NOT read creatures or submit R1 for loading

#### Scenario: The world worker enumerates regions

- **WHEN** a worker tick sees initializing, ready, and discarded regions
- **THEN** only the ready region MUST receive normal region tick/update calls

#### Scenario: Collision is queried for a non-ready region

- **WHEN** collision is queried for an initializing or discarded region
- **THEN** the query MUST return the existing fail-closed `FloorBlock` result

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

### Requirement: NPC construction and registration failures are fatal

Any non-cancellation failure constructing or registering a configured NPC MUST
be fatal to the region load. The loader MUST NOT publish a partially populated
region as ready. This exception boundary MUST NOT swallow cancellation or
failures from database, cache, decoder, map apply, or scheduler infrastructure.

#### Scenario: NPC construction fails after an earlier NPC registered

- **WHEN** NPC A registers and NPC B fails during construction
- **THEN** the region load MUST fail and the region MUST become `Discarded`
- **AND** NPC A MUST be cleaned up before the load attempt completes
- **AND** a later configured NPC MUST NOT be constructed or registered

#### Scenario: NPC registration infrastructure fails

- **WHEN** `INpcService.RegisterAsync` fails for an NPC
- **THEN** the region load MUST fail and the region MUST become `Discarded`
- **AND** successfully registered NPCs from that attempt MUST be cleaned up

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
