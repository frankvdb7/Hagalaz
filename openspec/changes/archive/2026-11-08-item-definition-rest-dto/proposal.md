# Item definition REST DTO
## Why
Item definition GET/PUT currently expose the cache model directly.
## What Changes
Introduce a full explicit ItemDefinitionDto and direct mapping. Preserve existing JSON field names and full-definition editing. Apply DTO fields onto the decoded persisted item so opaque cache metadata remains internal.
## Acceptance criteria
1. Item definition GET/PUT use DTOs with all stored fields and existing derived response fields; no cache implementation is a REST contract.
2. Existing save validation, model fields, extra data and opaque opcode preservation remain intact; targeted codec and API tests pass.
## Non-goals
No new API behavior, partial field updates, generic mapper framework, callback or unrelated endpoint redesign. Other type endpoints already expose explicit response/request records.
## Capabilities
### New Capabilities
- `item-definition-contract`: explicit REST boundary for full item editing.
### Modified Capabilities
None.
## Impact
Item definition endpoint, DTO mapping, cache metadata visibility and regression tests. Existing frontend JSON remains compatible.
