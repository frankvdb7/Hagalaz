# Store mutation endpoint DTOs
## Why
StoreMutationEndpoints still uses nested requests and a shared result record.
## What Changes
Dedicated WriteRawFileDto, CopyRawFileDto and per-operation result DTOs in Mutations/Dtos. Preserve all existing response fields, including null source IDs on writes.
## Acceptance criteria
1. Each raw-store operation uses dedicated request/result DTOs in its matching namespace.
2. Routes, JSON, base64 validation, ID validation, raw byte persistence and cache invalidation remain compatible; service regressions/build pass.
## Non-goals
No write ownership, atomicity, authorization, route or cache behavior changes. Admin has no consumer for these routes; no unused client API is added.
## Capabilities
### New Capabilities
- `store-mutation-contracts`: explicit raw-store REST DTOs.
### Modified Capabilities
None.
## Impact
Store mutation contracts, existing coherence-test references and focused HTTP regression coverage.
