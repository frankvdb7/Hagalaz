# cache-archive-member-snapshots Specification

## Purpose

Defines how the cache serves individual archive members efficiently while preserving independent stream ownership, bounded retention, and local write freshness.

## Requirements

### Requirement: Single-member reads return independent read-only member streams

The cache MUST serve a requested physical archive member from decoded member bytes and MUST return a fresh, seekable, read-only stream positioned at the start of that member. It MUST preserve existing missing-index, missing-archive, missing-member, and corrupt-data failures.

#### Scenario: Repeated reads of one member

- **WHEN** a caller reads the same physical member more than once
- **THEN** each call returns an independently positioned stream that cannot modify or expose shared member storage

#### Scenario: A decoded archive has multiple members

- **WHEN** callers request different physical members from the same archive
- **THEN** each stream contains only the requested member's bytes

#### Scenario: The requested member is absent

- **WHEN** the requested index, archive, or physical member is missing
- **THEN** the cache reports the established file-not-found failure

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

The cache MUST exclude single-member reads for the complete local write operation and invalidate the exact affected archive snapshot before releasing that exclusion, including when the write fails. This guarantee applies to writes through the same cache API instance only.

#### Scenario: A local write completes

- **WHEN** a local write changes an archive
- **THEN** later member reads reconstruct or retrieve the post-write archive while unrelated retained snapshots remain available

#### Scenario: A local write throws

- **WHEN** the writer throws after a member snapshot was retained
- **THEN** the original write failure propagates and the affected snapshot is invalidated before another member read proceeds

#### Scenario: An external or raw mutation occurs

- **WHEN** cache files are changed outside the owning cache API instance
- **THEN** the running instance does not claim automatic snapshot or reference-table coherence

### Requirement: Mutable archive reads remain detached from snapshots

Reading a complete archive MUST continue to return independently owned mutable data and MUST NOT expose or mutate retained single-member snapshot storage.

#### Scenario: Two mutable archive reads are changed independently

- **WHEN** a caller reads an archive twice and mutates one result
- **THEN** the other result and later single-member reads remain unchanged

### Requirement: Type providers decode each requested member independently

Single-definition provider reads MUST resolve the logical child through the reference table, request its physical member, and create a fresh decoded definition. Existing custom-provider delegation, factory fallback, and post-decode behavior MUST remain intact.

#### Scenario: A provider decodes a present definition

- **WHEN** a generic or specialized provider reads a present definition
- **THEN** it decodes the mapped physical member into a fresh definition and invokes the existing post-decode behavior once

#### Scenario: A provider decodes a missing definition

- **WHEN** the requested logical child has no reference-table entry
- **THEN** the provider uses its existing factory fallback without returning a cached definition instance
