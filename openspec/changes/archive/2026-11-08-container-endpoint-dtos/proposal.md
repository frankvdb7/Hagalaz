# Container endpoint DTOs
## Why
Container endpoints keep nested REST records and expose a cache compression enum in their contracts.
## What Changes
Dedicated GetContainerDto, DecryptContainerDto, WriteContainerDto and WriteContainerResultDto files in the matching namespace. Map compression to its existing byte JSON value.
## Acceptance criteria
1. All container JSON requests/results use dedicated DTOs with no cache model or enum types.
2. Routes, JSON fields, defaults, validation, binary/base64 and read/write semantics remain unchanged; targeted service regressions/build pass.
## Non-goals
No new client API, compression policy, route or cache ownership changes. Admin has no existing container consumer.
## Capabilities
### New Capabilities
- `container-contracts`: explicit container REST DTOs.
### Modified Capabilities
None.
## Impact
Container endpoint contracts and focused HTTP tests only.
