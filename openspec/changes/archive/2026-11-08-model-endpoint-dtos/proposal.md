# Model endpoint DTOs
## Why
ModelEndpoints exposes nested records, anonymous animation objects and cache decoder particle/frame/skeleton records.
## What Changes
Dedicated model, appearance, animation and particle DTOs with explicit mapping. Matching local admin DTOs replace component-local wire types and shared-renderer types used as HTTP contracts.
## Acceptance criteria
1. All ModelEndpoints JSON results use dedicated DTOs, including nested particle/animation data, with matching folder namespaces and no decoder types.
2. Existing routes, geometry, animation, appearance, particle data and warnings/unsupported responses remain unchanged.
3. Admin model resources use matching local DTOs, shared renderer remains independent, and backend/admin/viewer tests and admin build pass.
## Non-goals
No texture endpoint changes, rendering/animation algorithm changes, route changes or new dependencies.
## Capabilities
### New Capabilities
- `model-contracts`: explicit model asset API DTOs.
### Modified Capabilities
None.
## Impact
Model endpoint/DTO mapping, admin model HTTP contracts and focused regressions.
