# Organize definition endpoints
## Why
Read routes still live in TypeEndpoints while mutations use separate mutation files.
## What Changes
One named definition endpoint file per type, owning all its reads, mutations and mappings. Keep TypeEndpoints as the route-group composition root. Reuse shared query and persistence helpers.
## Acceptance criteria
1. ItemDefinitionEndpoints, NpcDefinitionsEndpoints and equivalent endpoint classes own all routes for their corresponding types; old mutation endpoint classes are removed.
2. All existing URLs, authorization, DTOs, validation, image disposal and persistence behavior remain unchanged; service regressions/build pass.
## Non-goals
No route/DTO changes, new operations, callback, framework, or renderer changes.
## Capabilities
### New Capabilities
- `definition-endpoint-ownership`: cohesive per-type endpoint ownership.
### Modified Capabilities
None.
## Impact
Cache service type endpoint organization only.
