# Split type mutation endpoints
## Why
One endpoint class owns mutations for six unrelated definition types.
## What Changes
Move each type's mappings, handlers and request/response records to its own endpoint class. Keep shared persistence in the existing utility and map each class directly from TypeEndpoints.
## Acceptance criteria
1. Items, NPCs, objects, sprites, varp bits and config definitions each own their mutation endpoints.
2. Existing HTTP routes, JSON shapes, validation, authorization and cache persistence remain unchanged; cache-service regressions pass.
## Non-goals
No new endpoints, behavior changes, callbacks, frameworks or cache ownership changes.
## Capabilities
### New Capabilities
- `type-mutation-organization`: endpoint ownership by definition type.
### Modified Capabilities
None.
## Impact
Cache service endpoint source organization and compile-time references in tests only.
