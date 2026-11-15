# Typed admin type endpoint contracts
## Why
Admin services still use unknown payloads and a shared partial item interface after backend DTO separation.
## What Changes
Add local TypeScript DTOs matching every backend type DTO, wire resources and mutation requests/results to them, and preserve types through shared store features. Keep UI summary abstractions separate from wire contracts.
## Acceptance criteria
1. Admin type services use explicit matching read/request/result DTOs, including distinct item read/update contracts and accurate nullability/JSON representations.
2. Definition/range/search stores retain resource types; editor submits only stored item fields; existing routes and UI behavior remain compatible.
3. Focused contract regressions, admin suite and production build pass.
## Non-goals
No runtime code generator, new dependency, API behavior or UI redesign. Generic JSON presentation may still accept unknown data.
## Capabilities
### New Capabilities
- `admin-type-contracts`: typed local DTOs for cache type APIs.
### Modified Capabilities
None.
## Impact
Admin DTOs, services, shared store features, typed consumers and tests only.
