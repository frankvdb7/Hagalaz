# Archive endpoint DTOs
## Why
ArchiveEndpoints still declares nested response records.
## What Changes
Extract GetArchiveDto and ArchiveEntryDto into Archives/Dtos and map the existing read response to them.
## Acceptance criteria
1. Archive read JSON uses dedicated DTOs with matching folder namespaces, unchanged fields and no cache types.
2. Single/multi-member and empty-member payloads retain member order, base64 data and lengths; missing archive response remains compatible; service tests/build pass.
## Non-goals
No routes, archive semantics, new client consumers or cache changes.
## Capabilities
### New Capabilities
- `archive-contracts`: explicit archive REST DTOs.
### Modified Capabilities
None.
## Impact
Archive endpoint, DTO files and focused read-only HTTP tests.
