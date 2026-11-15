# Raw-file endpoint DTOs
## Why
RawFileEndpoints has two binary download routes with individually bound route parameters rather than operation DTOs.
## What Changes
Bind each route through GetRawFileDto or GetRawSubFileDto with explicit route names. Keep binary responses; do not wrap file bytes in JSON.
## Acceptance criteria
1. Each raw download uses its dedicated route-parameter DTO in RawFiles/Dtos.
2. Existing URLs, bytes, octet-stream content type, filenames and missing-file behavior remain compatible; service tests/build pass.
## Non-goals
No JSON download wrappers, decoding/validation semantics, new client consumers, cache ownership or route changes.
## Capabilities
### New Capabilities
- `raw-file-contracts`: explicit raw download parameter DTOs.
### Modified Capabilities
None.
## Impact
Raw-file binding and focused binary HTTP regression tests. Admin currently has no consumer for these routes.
