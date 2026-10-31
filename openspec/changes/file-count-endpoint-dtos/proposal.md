# File-count endpoint DTOs
## Why
FileCountEndpoints still shares a nested response record between two operations.
## What Changes
Extract GetFileCountDto and GetSubFileCountDto into FileCounts/Dtos, preserving the count JSON field and typed results.
## Acceptance criteria
1. Each count operation uses its own dedicated DTO in its folder namespace.
2. Existing file/member counts, zero results and negative-ID validation remain unchanged; service tests/build pass.
## Non-goals
No counting semantics, route, cache ownership or client changes. Admin does not consume these routes.
## Capabilities
### New Capabilities
- `file-count-contracts`: explicit file-count REST DTOs.
### Modified Capabilities
None.
## Impact
File-count response contracts and focused HTTP regressions only.
