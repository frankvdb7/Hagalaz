## 1. State capabilities

- [x] 1.1 Replace the coupled `IState` contract with marker and opt-in capability interfaces for timed lifetime, custom ticking, lifecycle callbacks, persistence, and reapplication policy.
- [x] 1.2 Add `State`/`TimedState` convenience behavior for until-removed and timed states without an indefinite-duration sentinel.

## 2. Creature state ownership

- [x] 2.1 Implement the creature-owned state collection with typed queries, explicit reapplication policies, exactly-once lifecycle transitions, and snapshot-safe removal.
- [x] 2.2 Route `Creature` state APIs, registration, and game-loop processing through the collection while preserving existing public ergonomics and pooled synchronous processing.

## 3. Existing state migration

- [x] 3.1 Migrate all current duration-using states to the timed capability and remove `int.MaxValue` state lifetime initializers from the affected gameplay call sites.
- [x] 3.2 Migrate lifecycle callback states and ensure equipment-derived, activity, and representative passive states remain until explicitly removed and runtime-only.

## 4. Registry and persistence

- [x] 4.1 Replace raw state-type lookup with narrow create/identity operations and make duplicate registrations fail during startup while unknown IDs return safely.
- [x] 4.2 Make character state persistence opt-in, preserve timed remaining duration, support persistent until-removed state records, and skip missing metadata/runtime-only/unknown records without exceptions.

## 5. Regression coverage and verification

- [x] 5.1 Add MSTest coverage for passive/timed/custom-tick lifecycle, reapplication policies, callback exactness, coexistence, and snapshot-safe mutation.
- [x] 5.2 Add representative bow, freeze/immunity, Staff of Light, activity, persistence, unknown-ID, and duplicate-ID regressions.
- [x] 5.3 Run focused tests, the GameWorld test project, affected builds, OpenSpec validation, and a clean diff review; record any unverified topology separately.
