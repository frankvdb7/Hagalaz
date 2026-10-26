# cache-reference-tables Specification

## Purpose

Defines how reference tables represent sparse archive identifiers independently from the number of groups present.

## Requirements

### Requirement: Archive identifier capacity is distinct from group count

A reference table MUST preserve each archive's identifier independently of the number of groups it contains. `Count` MUST report the number of present groups. `Capacity` MUST report one greater than the greatest present archive identifier, or zero when no groups are present. Missing identifiers within that capacity MUST remain absent.

#### Scenario: A table contains sparse archive identifiers

- **WHEN** a reference table contains archive identifiers 1 and 100
- **THEN** its Count is 2, its Capacity is 101, and lookup by identifier 100 returns the corresponding entry

#### Scenario: A lookup targets a gap between sparse identifiers

- **WHEN** a caller looks up archive identifier 50 in that table
- **THEN** the lookup reports that the archive is absent

### Requirement: Reference-table encoding preserves sparse identifiers and group count

Reference-table encoding and decoding MUST preserve every present archive identifier and its associated entry while using the number of present groups as the serialized group count.

#### Scenario: A sparse table is encoded and decoded

- **WHEN** a reference table with archive identifiers 1 and 100 is encoded and decoded
- **THEN** the decoded table has Count 2 and Capacity 101, and both entries remain available under their original identifiers

### Requirement: Reference-table responses report both group count and ID capacity

The reference-table response MUST report the number of present groups and the exclusive upper bound of the archive-ID space as separate values, and MUST preserve each entry's archive identifier.

#### Scenario: A sparse table is returned by the reference-table endpoint

- **WHEN** the endpoint returns a reference table containing archive identifiers 1 and 100
- **THEN** the response reports Count 2, Capacity 101, and entries keyed by identifiers 1 and 100

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
