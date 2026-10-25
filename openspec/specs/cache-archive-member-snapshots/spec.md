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

`CacheApi` MUST own reference-table memoization and archive snapshot retention. Supported same-instance writes MUST hold the existing write exclusion until affected cached state is cleared, including when the operation throws. High-level writes clear their index table memo and exact archive snapshot; raw writes and copies apply destination-based invalidation.

#### Scenario: A high-level archive write completes

- **WHEN** a local high-level write changes an archive
- **THEN** later member reads observe the post-write archive and later table lookups decode the current reference table while unrelated index state remains available

#### Scenario: A raw ordinary archive write or copy completes

- **WHEN** a local raw write or copy changes an ordinary archive
- **THEN** later member reads observe the destination bytes while the source snapshot and reference-table memo remain available

#### Scenario: A local write throws

- **WHEN** a high-level write or raw reference-table write/copy throws after cached state exists
- **THEN** the original write failure propagates and the affected table memo and snapshots are invalidated before another read proceeds

#### Scenario: A raw ordinary archive write or copy throws

- **WHEN** a raw write or copy to an ordinary archive throws after its destination snapshot was retained
- **THEN** the original write failure propagates and the destination snapshot is invalidated while the reference-table memo remains available

#### Scenario: A raw reference-table file changes by write or copy

- **WHEN** a raw write or copy replaces `(255, dataIndex)` through `CacheApi`
- **THEN** later type reads use the refreshed logical-to-physical mapping and reconstruct snapshots for that data index

#### Scenario: A reference-table lookup overlaps a raw replacement

- **WHEN** a reference-table lookup is already loading while a raw write or copy replaces its file
- **THEN** the mutation waits for the lookup, invalidates its memoized result before reads resume, and later lookups load the replacement table

#### Scenario: A mutation occurs outside the owning cache API instance

- **WHEN** cache files are changed by an external process or by direct `IFileStore` use outside the owning cache API instance
- **THEN** the running instance does not claim automatic snapshot or reference-table coherence and callers must follow the existing restart requirement

#### Scenario: A caller retains a table during mutation

- **WHEN** a local write clears a memoized reference table
- **THEN** a previously returned mutable table object remains the same object and is not retroactively updated

### Requirement: Multi-operation reads are not atomic with local writes

`ReadArchive`, `ReadContainer`, and composed range or inspection workflows MAY overlap same-instance local writes. The cache MUST NOT promise a before-or-after atomic result for these higher-level operations.

#### Scenario: A higher-level read overlaps a local mutation

- **WHEN** a `ReadArchive`, `ReadContainer`, or range/inspection workflow overlaps a local high-level write, raw write, or raw copy
- **THEN** only individual member reads and reference-table lookups have the documented write serialization; no guarantee covers the complete higher-level read and decode sequence

### Requirement: Mutable archive reads remain detached from snapshots

Reading a complete archive MUST continue to return independently owned mutable data and MUST NOT expose or mutate retained single-member snapshot storage.

#### Scenario: Two mutable archive reads are changed independently

- **WHEN** a caller reads an archive twice and mutates one result
- **THEN** the other result and later single-member reads remain unchanged

### Requirement: Generic and sprite reads require a parent archive

Generic `TypeProvider<T, TD>` and `SpriteTypeProvider` single-definition reads MUST fail with `FileNotFoundException` when the parent archive has no reference-table entry. Generic `TypeProvider<T, TD>.GetRange` MUST preserve the same missing-parent failure.

#### Scenario: A generic individual and range request target a missing parent archive

- **WHEN** `TypeProvider.Get` and `TypeProvider.GetRange` request a definition whose parent archive is absent from the reference table
- **THEN** both requests report `FileNotFoundException` and neither returns a factory fallback

#### Scenario: A sprite request targets a missing parent archive

- **WHEN** `SpriteTypeProvider.Get` requests a definition whose parent archive is absent from the reference table
- **THEN** it reports `FileNotFoundException` and does not return a factory fallback

### Requirement: Missing child mappings use provider fallbacks

Generic and sprite providers MUST return factory fallbacks when a parent archive exists but the requested child mapping is absent. Item, NPC, and object providers MUST retain factory fallbacks when either the parent archive or child mapping is absent.

#### Scenario: A child is absent from an existing archive

- **WHEN** a generic, sprite, item, NPC, or object provider requests a definition whose parent archive exists but whose child mapping is absent
- **THEN** it returns a factory fallback without reading a physical member

#### Scenario: A specialized provider's parent archive is absent

- **WHEN** an item, NPC, or object provider requests a definition whose parent archive is absent from the reference table
- **THEN** the provider returns its existing factory fallback

### Requirement: Mapped definitions decode through the cache API

Providers MUST decode mapped physical members through `ICacheAPI.Read` and propagate missing-file or decode failures. Existing custom-provider delegation and post-decode behavior MUST remain intact. A null-terminated string that reaches the end of its input without a terminator MUST fail with `EndOfStreamException`.

#### Scenario: A provider decodes a present definition

- **WHEN** a provider reads a present definition
- **THEN** it decodes the mapped physical member into a fresh definition and invokes existing post-decode behavior once

#### Scenario: A mapped archive file is missing or corrupt

- **WHEN** a provider requests a mapped definition but its archive file is missing or its data cannot be decoded
- **THEN** the cache's file-not-found or decoding failure propagates and the provider does not use a factory fallback

#### Scenario: A mapped definition contains an unterminated string

- **WHEN** a definition codec reads a null-terminated string whose terminator is missing
- **THEN** decoding fails with `EndOfStreamException` after consuming the available bytes

### Requirement: Archive-batched provider ranges release decoded archives
Generic and NPC provider range reads MUST release each decoded archive after its adjacent definition batch has been processed, including when archive loading, definition decoding, or the range post-decode hook throws.

#### Scenario: A range crosses archive boundaries
- **WHEN** a generic or NPC range reads definitions from more than one archive
- **THEN** each prior archive is disposed before the next archive is loaded and the final archive is disposed when the range completes

#### Scenario: A range operation fails
- **WHEN** loading a later archive, decoding a member, or running the range hook throws
- **THEN** any archive currently owned by the range operation is disposed and the original exception propagates

### Requirement: Rewritten archive versions advance from reference-table metadata

High-level writes to an existing archive MUST advance that archive's reference-table entry version by one from its pre-write value. Version advancement MUST remain correct when the physical container has no version trailer.

#### Scenario: Consecutive archive-member replacements

- **WHEN** two high-level writes replace members of an archive whose reference-table entry starts at version 5
- **THEN** the entry version is 7, the final member bytes are readable, and the physical container keeps the existing trailer-free encoding
