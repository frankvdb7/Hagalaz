## MODIFIED Requirements

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
