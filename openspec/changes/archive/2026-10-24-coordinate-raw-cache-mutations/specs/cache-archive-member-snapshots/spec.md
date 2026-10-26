# Spec Delta

## MODIFIED Requirements

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
