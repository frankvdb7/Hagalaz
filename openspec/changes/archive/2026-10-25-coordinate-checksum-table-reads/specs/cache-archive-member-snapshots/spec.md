# Spec Delta

## ADDED Requirements

### Requirement: Checksum generation coordinates with local cache writes

The Cache API MUST prevent local writes from interleaving with a checksum-table calculation performed by the same cache instance.

#### Scenario: Checksum generation overlaps an archive mutation

- **WHEN** a local archive mutation begins while the Cache API is calculating the checksum table
- **THEN** the mutation waits until checksum generation finishes, and the checksum uses a consistent set of reference-table bytes
