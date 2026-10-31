# Operation-specific type endpoint DTOs
## Why
Existing nested REST records and a shared item DTO do not make each operation contract explicit.
## What Changes
Dedicated named DTO files for type reads, payload requests and mutation results. Separate GetItemDefinitionDto from UpdateItemDefinitionDto. Preserve existing JSON and routes; collections reuse element DTOs and the existing search envelope shape. Binary images remain binary.
## Acceptance criteria
1. All type endpoint JSON payloads use dedicated operation-named DTOs; no nested request/response records or shared get/update item DTO remain.
2. Routes, payload JSON, full-field mapping, validation and persistence remain compatible; service tests pass.
## Non-goals
No new full-definition editing APIs for other types, no route changes, generic mapping framework or binary-image JSON wrappers.
## Capabilities
### New Capabilities
- `type-endpoint-dtos`: explicit operation contracts for type routes.
### Modified Capabilities
None.
## Impact
Cache service DTO files, type handlers, shared persistence helper return ownership and test references. Frontend wire shape unchanged.
