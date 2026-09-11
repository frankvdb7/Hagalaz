## Planning

- [x] Record ownership boundaries, non-goals, invariants, and acceptance criteria.

## Implementation

- [x] Remove the Creature destruction gate and update lifecycle tests.
- [x] Move MapRegion destruction ownership to MapRegionService and remove the
      low-level destruction synchronization/state machinery.
- [x] Move all map-load request infrastructure into MapRegionLoadScheduler and
      remove the request sink/queue bridge and scheduler residency dependency.
- [x] Simplify MapRegionLoader failure cleanup to preserve the primary failure.
- [x] Make NpcStore removal the exact lifecycle claim before NPC destruction and
      update registration/unregistration tests.
- [x] Replace pending-abort processing tokens with a store-owned boolean marker.
- [x] Audit CharacterLogoutService and preserve its existing high-level
      completion fencing without adding character-level synchronization.

## Specifications and verification

- [x] Update related active OpenSpec artifacts so deleted mechanisms are not
      described as current behavior.
- [x] Add or revise ownership-focused regression tests.
- [ ] Run focused GameWorld lifecycle/scheduler/session tests.
- [ ] Run GameWorld integration tests where infrastructure permits.
- [ ] Run Raido tests, solution build, strict OpenSpec validation, and diff
      checks.
