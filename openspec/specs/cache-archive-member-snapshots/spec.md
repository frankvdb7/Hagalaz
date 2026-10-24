# cache-archive-member-snapshots Specification

## Purpose

Defines how the cache serves individual archive members efficiently while preserving independent stream ownership, bounded retention, and local write freshness.

## Requirements

### Requirement: Single-member reads return independent read-only member streams

Direct cache API reads MUST serve a requested physical archive member from decoded member bytes and MUST return a fresh, seekable, read-only stream positioned at the start of that member. They MUST preserve the established missing-index, missing-archive, missing-physical-member, and corrupt-data failures.

#### Scenario: Repeated reads of one member

- **WHEN** a caller reads the same physical member more than once
- **THEN** each call returns an independently positioned stream that cannot modify or expose shared member storage

#### Scenario: A decoded archive has multiple members

- **WHEN** callers request different physical members from the same archive
- **THEN** each stream contains only the requested member's bytes

#### Scenario: A direct physical-member read targets missing data

- **WHEN** a caller directly requests a physical member whose index, archive, or physical member is missing
- **THEN** the cache reports the established file-not-found failure

#### Scenario: A direct physical-member read targets corrupt archive data

- **WHEN** a caller directly requests a physical member from corrupt archive data
- **THEN** the cache propagates the established container or archive decoding failure

### Requirement: Snapshot retention is bounded and configurable

The cache MUST bound retained snapshot weight by a configured non-negative byte capacity, evict least-recently-used entries to admit fitting snapshots, and serve but not retain snapshots that exceed the capacity. A zero capacity MUST disable completed retention.

#### Scenario: A retained snapshot is accessed

- **WHEN** a retained snapshot is read
- **THEN** it becomes the most recently used entry

#### Scenario: A snapshot fits the remaining budget

- **WHEN** a snapshot's accounted weight is no greater than the configured capacity
- **THEN** it may be retained and the least-recently-used entries are evicted as needed to keep the total within capacity

#### Scenario: Capacity is zero, negative, or the snapshot is oversized

- **WHEN** capacity is zero or the snapshot exceeds the configured capacity
- **THEN** the read is served without permanent retention; a negative capacity is rejected during cache construction

### Requirement: Concurrent cold reads share archive reconstruction

Concurrent callers attached to a cold archive load MUST share that reconstruction. A failed reconstruction MUST NOT be retained or prevent a later independent request from retrying.

#### Scenario: Concurrent reads request different members of one cold archive

- **WHEN** both reads attach to the same active archive load
- **THEN** the archive is reconstructed once and each caller receives its requested member

#### Scenario: Concurrent reads request different cold archives

- **WHEN** distinct archives are cold
- **THEN** their reconstructions can proceed independently

#### Scenario: A cold reconstruction fails

- **WHEN** archive loading or decoding fails
- **THEN** attached callers observe that failure and a later independent read may start a fresh reconstruction

### Requirement: Local writes invalidate affected snapshots before reads resume

The cache MUST exclude single-member reads and Cache API reference-table lookups for the complete local `CacheApi.Write`, raw write, or raw copy operation and invalidate affected snapshots before releasing that exclusion, including when the write fails. A raw reference-table mutation MUST also refresh the memoized table and invalidate snapshots for its corresponding data index. This guarantee applies only to mutations through the same cache API instance.

#### Scenario: A local archive write completes

- **WHEN** a local high-level write or raw write/copy changes an archive
- **THEN** later member reads observe the post-write archive while unrelated retained snapshots remain available

#### Scenario: A local write throws

- **WHEN** a local high-level write or raw write/copy throws after a member snapshot was retained
- **THEN** the original write failure propagates and the affected snapshot is invalidated before another member read proceeds

#### Scenario: A raw reference-table file changes

- **WHEN** a raw write or copy replaces the reference-table file for a data index
- **THEN** later type reads use the refreshed logical-to-physical mapping and reconstruct snapshots for that data index

#### Scenario: A reference-table lookup overlaps a raw replacement

- **WHEN** a reference-table lookup is already loading while a raw write or copy replaces its file
- **THEN** the mutation waits for the lookup, invalidates its memoized result before reads resume, and later lookups load the replacement table

#### Scenario: A mutation occurs outside the owning cache API instance

- **WHEN** cache files are changed by an external process or by direct `IFileStore` use outside the owning cache API instance
- **THEN** the running instance does not claim automatic snapshot or reference-table coherence

### Requirement: Mutable archive reads remain detached from snapshots

Reading a complete archive MUST continue to return independently owned mutable data and MUST NOT expose or mutate retained single-member snapshot storage.

#### Scenario: Two mutable archive reads are changed independently

- **WHEN** a caller reads an archive twice and mutates one result
- **THEN** the other result and later single-member reads remain unchanged

### Requirement: Type providers decode each requested member independently

Providers MUST resolve requested children through the reference table and return factory fallbacks when the parent archive or child mapping is absent. For mapped entries, they MUST decode the mapped physical member into a fresh definition and propagate read or decode failures. Existing custom-provider delegation and post-decode behavior MUST remain intact.

#### Scenario: A provider decodes a present definition

- **WHEN** a generic or specialized provider reads a present definition
- **THEN** it decodes the mapped physical member into a fresh definition and invokes the existing post-decode behavior once

#### Scenario: The parent archive has no reference-table entry

- **WHEN** a provider requests a definition whose parent archive is absent from the reference table
- **THEN** it returns a factory fallback without attempting to read an archive file

#### Scenario: A child is absent from an existing archive

- **WHEN** a provider requests a definition whose parent archive exists but whose logical child has no reference-table entry
- **THEN** it returns a factory fallback without attempting to read an archive file

#### Scenario: A mapped archive file is missing

- **WHEN** a provider requests a mapped definition but its archive file is missing from storage
- **THEN** the cache's file-not-found failure propagates and the provider does not use the factory fallback

#### Scenario: A mapped archive is corrupt

- **WHEN** a provider requests a mapped definition but its archive data cannot be decoded
- **THEN** the cache's decoding failure propagates and the provider does not use the factory fallback
