## MODIFIED Requirements

### Requirement: Concurrent reference-table initialization is coordinated per index

`ReferenceTableProvider` MUST decode reference-table data without memoizing it or exposing an invalidation operation. `CacheApi` MUST own one memoized table slot per ordinary cache index and MUST publish only successfully decoded tables. Concurrent cold lookups for one index MUST share one decode; different indexes MAY decode concurrently. A failed decode MUST leave the slot empty for a later retry.

#### Scenario: Direct provider calls decode independently

- **WHEN** a caller invokes `ReferenceTableProvider.ReadReferenceTable` more than once for an index
- **THEN** each call reads and decodes a new table and disposes temporary storage resources

#### Scenario: CacheApi serves a warm table

- **WHEN** a table for an index has already been successfully loaded by the same `CacheApi`
- **THEN** subsequent lookups return that memoized instance without another provider decode

#### Scenario: Concurrent cold reads share one initialization

- **WHEN** concurrent callers request one cold index through the same `CacheApi`
- **THEN** the provider decodes the table once and callers receive the published table instance

#### Scenario: A cold lookup fails

- **WHEN** provider decoding throws for an uncached index
- **THEN** the exception propagates and a later lookup retries decoding

#### Scenario: A local write refreshes reference-table state

- **WHEN** a supported local write changes an ordinary index's reference-table file
- **THEN** `CacheApi` clears its memoized slot before releasing write exclusion; previously returned table objects are not retroactively modified
