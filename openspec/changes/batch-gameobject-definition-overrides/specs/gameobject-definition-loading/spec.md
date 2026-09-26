# GameObject definition loading

## ADDED Requirements

### Requirement: Resolve only requested GameObject definitions in a bulk read

GameWorld MUST resolve a supplied set of GameObject definition IDs using one bulk database read on a cold batch, rather than a database request per ID. The repository query MUST filter to requested IDs and project only fields consumed by GameWorld. The service MUST deduplicate IDs, return definitions keyed by requested ID, retain archive fallback when no database override exists, and keep cache details internal. Empty ID sets MUST NOT query the database. Cancellation MUST reach the bulk read.

#### Scenario: A populated region resolves definitions

- **WHEN** a current map region contains static map objects or database-spawned game objects
- **THEN** GameWorld requests the definitions for that region's object IDs in bulk before constructing those objects
- **AND** each object is built with the definition matching its ID

#### Scenario: Duplicate IDs are resolved once

- **WHEN** multiple placements in the same region use the same object ID
- **THEN** the service sends that ID only once in the bulk request

#### Scenario: The requested batch is cached

- **WHEN** GameWorld resolves the same set of definition IDs again
- **THEN** the existing HybridCache reuses the batch result

#### Scenario: An empty region loads no definitions

- **WHEN** a current map region contains no static or database-spawned game objects
- **THEN** GameWorld does not query the GameObject override table

#### Scenario: An object has no database override

- **WHEN** GameWorld resolves an object ID absent from the requested database override rows
- **THEN** GameWorld returns the revision-cache archive definition without changing its examine or loot-table fields

#### Scenario: Region loading is canceled during definition resolution

- **WHEN** the region-load cancellation token is canceled before or during the bulk query
- **THEN** the query observes cancellation and region loading does not continue constructing objects
