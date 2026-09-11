## Ownership model

Domain objects own semantic state. `GameWorkerService`, `MapRegionService`,
stores, schedulers, and lease services own the transitions they already
sequence or claim. A caller asks the owner to perform a compound operation;
it does not check a state through one API and mutate it through another.

## Decisions

1. **MapRegionService owns all map residency.** `Dimension` is a data holder
   with identity and active/idle collections; it owns no synchronization
   primitive. `MapRegionService` uses one private gate for the dimension
   registry and every active/idle transition. Region construction and loading
   stay outside that gate. Enumeration returns explicit snapshots made under
   the same gate, and empty-dimension removal validates global-dimension,
   exact-instance, and emptiness invariants atomically.

2. **Admission initializes revision before publication.** Hydration creates a
   character, `InitializeRevision` monotonically seeds its persistence state,
   and `CharacterService.AddAsync` then claims the exact local instance. A
   failed registration destroys only the unregistered object and does not
   forget the monotonic revision. Later failures remove and destroy the exact
   registered instance before releasing the session reservation; revision
   initialization is not rolled back.

3. **Mutating region operations require active ownership.** `resume: false`
   is retained only for read-only access. Collision, object, item, dynamic
   region, and teardown paths use the service operation that resumes or
   creates canonical active ownership before mutating a region.

4. **Creature event cleanup is terminal.** `UnregisterEventHandlers` detaches
   its handler inventory before calling the event manager, attempts every
   captured handler, and throws the first failure after the pass. A destroyed
   creature cannot resume cleanup or register new handlers.

5. **Lease renewal follows store-owned state.** `GameSessionStore.FindAll`
   returns active and pending-world sessions, while moving a session into
   pending claim cleanup removes it from those stores. The lease service keeps
   the pending-cleanup list for reconciliation but does not build a redundant
   membership set for the active loop.

6. **Stores expose explicit collection boundaries.** NPC enumeration does not
   create a hidden snapshot. Character callers that need a stable set request
   `GetSnapshotAsync`; direct lookups hold a reader lock only for the lookup,
   never while yielding to caller code.

7. **Logout workflow is separate from persistence.** A keyed
   `CharacterLogoutState` owned by `CharacterLogoutService` tracks pending,
   removed, and completing logout workflow state. `CharacterPersistenceState`
   retains only persistence serialization, revision, and acknowledgement state.

8. **Lifecycle visibility has one owner.** Map loading/scheduling owns the
   ready/discarded transitions; `MapRegion` performs visibility-only volatile
   state writes and does not arbitrate lifecycle transitions with CAS.

9. **NPC compensation preserves ownership.** If `OnRegistered` fails, exact
   removal is attempted. Destruction happens only when removal succeeds; if
   removal fails, the original registration exception is preserved and the
   store-owned NPC is not destroyed. Both sync and async registration APIs and
   the existing store lock remain.

10. **Cumulative PR ownership remains unchanged.** Creature destruction stays
    terminal, map teardown releases external resources after exact residency
    removal, map loading stays scheduler-owned, pending abort processing stays
    store-owned and retryable, Contacts retains generation checks, and
    `MapRegionPart._updatesLock` remains because it protects its own buffers.

## Verification strategy

- Test explicit active/idle snapshot safety, concurrent dimension allocation,
  active ownership for mutation, and exact empty-dimension removal.
- Test admission ordering, monotonic revision preservation, and the absence
  of persistence rollback on failed local registration.
- Test terminal event-handler cleanup attempts all handlers after one fails.
- Test logout state ownership, explicit character snapshots, NPC compensation,
  and both NPC API families.
- Run the cumulative GameWorld tests, integration tests, Contacts tests, Raido
  tests, solution build, locked restore, strict OpenSpec validation, and diff
  checks.
