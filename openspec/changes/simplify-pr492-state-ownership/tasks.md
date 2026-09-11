## 1. Planning and consolidation

- [x] 1.1 Read the cumulative PR ownership requirements and choose this change
      as the single authoritative lifecycle/state simplification record.
- [x] 1.2 Validate the updated proposal, design, delta specification, and task
      list with strict OpenSpec validation.

## 2. Service-owned map residency

- [x] 2.1 Replace `Dimension.ResidencySyncRoot` with one private
      `MapRegionService` residency gate covering the dimension registry and
      active/idle ownership transitions.
- [x] 2.2 Make dimension creation, removal, publication, suspension, resume,
      and enumeration use the same ownership gate without holding it during
      region construction or loading.
- [x] 2.3 Audit `resume: false` callers and make every mutating path claim
      canonical active ownership before mutation.
- [x] 2.4 Remove duplicate `GetMapRegion` ownership checks and add deterministic
      race/identity tests for dimensions, regions, and idle destruction.

## 3. Store and workflow ownership

- [x] 3.1 Remove unused `INpcStore.FindAllAsync`; if any real caller requires
      a point-in-time set, expose an explicitly named snapshot operation.
- [x] 3.2 Replace character streaming enumeration at actual callers with
      explicit snapshots or direct lookups; do not hold a reader lock while
      yielding to caller code.
- [x] 3.3 Move pending logout workflow state out of
      `CharacterPersistenceState` and into `CharacterLogoutService`, retaining
      only justified persistence serialization and acknowledgement state.
- [x] 3.4 Remove the test-only dehydration request helper and adapt tests to
      the production contract.

## 4. Lifecycle and NPC ownership

- [x] 4.1 Simplify `MapRegion` ready/discarded transitions to visibility-only
      state writes when the scheduler/loader is the sole lifecycle owner.
- [x] 4.2 Make sync and async NPC registration compensation retain store
      ownership when exact removal fails; preserve the original registration
      exception.
- [x] 4.3 Preserve both NPC API families and the existing store synchronization
      mechanism without adding lower-level locks or adapters.

## 5. Regression coverage

- [x] 5.1 Add deterministic admission tests for revision ordering and
      duplicate-admission preservation.
- [x] 5.2 Add deterministic map-region ownership/mutation race tests.
- [x] 5.3 Add store, logout, lifecycle, and NPC compensation tests that prove
      behavior rather than implementation primitives.

## 6. Validation and cleanup

- [x] 6.1 Run focused tests for admission, persistence, logout, dehydration,
      map-region service/background/scheduler/loader, NPC store/service, and
      the full GameWorld test project.
- [x] 6.2 Run integration, Contacts, and Raido tests, locked restore, serial
      solution build, strict OpenSpec validation, and final diff/concurrency
      audits.
- [x] 6.3 Preserve the single authoritative change record and remove obsolete
      mechanism-specific spec text.
