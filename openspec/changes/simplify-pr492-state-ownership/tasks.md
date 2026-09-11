## 1. Planning and consolidation

- [x] 1.1 Read the cumulative PR ownership requirements and choose this change
      as the single authoritative lifecycle/state simplification record.
- [x] 1.2 Validate the updated proposal, design, delta specification, and task
      list with strict OpenSpec validation.

## 2. Production simplification

- [x] 2.1 Replace `IDimension` dictionary properties and `CanDestroy` with
      explicit `MapRegionService` snapshots and atomic empty-dimension removal.
- [x] 2.2 Reorder world admission to claim local character ownership before
      revision initialization and remove failed-registration persistence probes.
- [x] 2.3 Make creature event-handler cleanup terminal while preserving first
      failure and attempting all handlers.
- [x] 2.4 Remove impossible pending-cleanup filtering, claim-loss state, and
      lease forwarding ceremony without changing reconciliation behavior.
- [x] 2.5 Preserve the previously implemented scheduler, loader, creature,
      MapRegion, NPC, abort, Contacts, and update-buffer ownership boundaries.

## 3. Regression coverage

- [x] 3.1 Add explicit residency snapshot and atomic dimension-removal tests.
- [x] 3.2 Add admission ordering and failed-registration persistence tests.
- [x] 3.3 Add terminal event-cleanup failure/remainder tests.
- [x] 3.4 Preserve cumulative lifecycle, lease, abort, and update-buffer tests.

## 4. Validation and cleanup

- [x] 4.1 Run focused and full GameWorld tests, Contacts tests, and integration
      tests where Docker infrastructure permits.
- [x] 4.2 Run Raido tests, locked restore, solution build, strict OpenSpec
      validation, and final diff/symbol audits.
- [x] 4.3 Remove the duplicate `simplify-pr492-lifecycle-ownership` change
      artifacts after the consolidated record validates.
