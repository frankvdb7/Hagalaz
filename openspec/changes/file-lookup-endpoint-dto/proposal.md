# File lookup endpoint DTO
## Why
FileLookupEndpoints still declares a nested response record.
## What Changes
Extract GetFileIdDto into FileLookup/Dtos, preserving the typed result and fileName query.
## Acceptance criteria
1. File lookup returns a dedicated DTO in its matching namespace with unchanged JSON.
2. Named lookup, missing-name sentinel and invalid-ID/query behavior remain compatible; service tests/build pass.
## Non-goals
No lookup/hash algorithm, route, query semantics or client API changes. Admin has no file-lookup consumer.
## Capabilities
### New Capabilities
- `file-lookup-contract`: explicit file lookup REST DTO.
### Modified Capabilities
None.
## Impact
File-lookup response contract and focused read-only HTTP regressions.
