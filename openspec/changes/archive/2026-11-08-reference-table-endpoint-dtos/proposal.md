# Reference-table endpoint DTOs
## Why
ReferenceTableEndpoints still declares nested response records and exposes the cache flags enum through REST.
## What Changes
Dedicated GetReferenceTableDto, ReferenceTableEntryDto and ReferenceTableChildEntryDto under ReferenceTables/Dtos. Preserve numeric flags and all existing fields.
## Acceptance criteria
1. Table/entry/child JSON uses dedicated DTOs without cache model/enum properties.
2. Sparse counts/capacities, ordering, hashes, CRC/version, digests and child indexes remain compatible; service tests/build pass.
## Non-goals
No reference-table semantics, route, cache ownership or client changes. Admin has no existing reference-table consumer.
## Capabilities
### New Capabilities
- `reference-table-contracts`: explicit reference table REST DTOs.
### Modified Capabilities
None.
## Impact
Reference-table DTO/mapping and focused read-only endpoint regression tests.
