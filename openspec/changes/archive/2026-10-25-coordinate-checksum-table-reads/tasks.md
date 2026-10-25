# Tasks

- [x] 1. Add a concurrency regression that blocks a checksum reference-table read while a local archive mutation attempts to persist.
- [x] 2. Hold the existing cache read lock throughout `CreateChecksumTable`.
- [x] 3. Verify checksum generation, cache integration tests, and OpenSpec validation.
