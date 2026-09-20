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

2. **Admission acquires character ownership before persistence state.**
   Hydration creates a character, `CharacterService.AddAsync` claims the exact
   local instance, and only then does `InitializeRevision` seed persistence
   state for that admission. A failed registration destroys only the
   unregistered object and cannot change or release another character's
   persistence state. Later failures remove and destroy the exact registered
   instance before releasing the session reservation; revision initialization
   is released only when this admission acquired it.

3. **Map-region callers state intent explicitly.** `GetOrCreateMapRegion`
   returns canonical active ownership for mutations and general gameplay.
   `FindMapRegion` performs an exact existing-region lookup without creating or
   resuming an idle region; read-only and teardown paths use it.

4. **Creature cleanup is terminal.** `UnregisterEventHandlers` detaches its
   handler inventory before calling the event manager. Creature, Character, and
   NPC destruction continues across independent cleanup failures and reports
   them in one `AggregateException`. A destroyed creature cannot resume cleanup
   or register new handlers.

5. **Lease renewal follows store-owned state.** `GameSessionStore.FindAll`
   returns active and pending-world sessions, while moving a session into
   pending claim cleanup removes it from those stores. The lease service keeps
   the pending-cleanup list for reconciliation but does not build a redundant
   membership set for the active loop.

6. **Stores expose explicit collection boundaries.** NPC enumeration does not
   create a hidden snapshot. Character callers that need a stable set request
   `GetSnapshotAsync`; direct lookups hold a reader lock only for the lookup,
   never while yielding to caller code.

7. **Logout workflow is separate from persistence.** A
   `CharacterLogoutState` owned by `CharacterLogoutService` stores plain
   pending records in one dictionary behind one owner gate. Exact character
   identity prevents silent takeover and duplicate forced snapshots.
   `CharacterPersistenceState` retains persistence serialization, revision,
   pending receipt matching, and acknowledgement state; the persistence
   consumer acknowledges it directly and never completes logout.

8. **Lifecycle visibility has one owner.** Map loading/scheduling owns the
   ready/discarded transitions; `MapRegion` exposes only that existing
   visibility state and does not maintain a separate destruction flag.

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

11. **Persistence submission owns local pending cleanup.** Persistence records
    the exact receipt before publication, releases that receipt if publish or
    outbox submission fails, and never rolls back the consumed revision. The
    first terminal acknowledgement wins; a conflict clears the persistence
    submission entry without marking its fingerprint persisted. Transport or
    outbox submission failures may retry the same detached revision. Final
    logout retains its detached snapshot and logout ownership on `Conflict`
    without replacing the snapshot, allocating a revision, or republishing.

12. **Entity lifecycle is an ownership decision, not entity state.** Creature,
    Character, NPC, MapRegion, GameObject, and GroundItem do not expose an
    `IsDestroyed` flag for callers. The owning store, residency service, or
    region-part collection establishes exact ownership before terminal
    cleanup; cleanup methods do not arbitrate a second lifecycle state.

13. **Creature owns cancellation for its queued work.** Each concrete
    `Creature` privately owns one task `CancellationTokenSource`. Its
    `QueueTask` methods supply that token to `ICreatureTaskService`, which wraps
    tasks for cancellation-aware scheduling and delegates execution to the one
    shared generic scheduler. `Creature.Destroy()` cancels the token as part of
    deterministic terminal cleanup; no task registry or lifecycle flag is
    needed. Exact owner removal remains the lifecycle transition, and failed
    removal does not call `Destroy()` or cancel the Creature's task authority.

14. **Persistence consumes detached models.** `CharacterDehydrationService`
    synchronously creates the existing detached `CharacterModel`. Both final
    logout and periodic persistence use that service through the GameWorker;
    `CharacterPersistenceService` only publishes/awaits detached models and
    never reads a live Character.

15. **Logout retains the handoff data.** The existing gate-backed pending
    logout record retains the exact Character during terminal preparation, the
    captured snapshot after removal, and the persistence receipt after
    submission. Transport or outbox retries may reuse those data fields and
    revision; a persistence `Conflict` is terminal for that logout attempt and
    does not allocate a new revision or republish automatically.

16. **Async results use the creature queue boundary.** Commands capture
    immutable inputs before their asynchronous work and submit Character-
    targeted continuations through `character.QueueTask(...)`. A destroyed
    Creature's cancelled token causes queued continuations to be dropped; a
    replacement with the same master ID cannot inherit a stale task.

17. **Connection lifetime does not define input ordering.** Raido message and
    disconnect callbacks may use independent scopes and overlap. CharacterStore
    membership plus the creature task/logout boundary claim the shared
    boundary, so admitted gameplay is queued before the terminal turn and
   later input is rejected.

18. **Creature lifetime cancellation is scheduler-owned.** Destroying a
    Creature cancels its private token, but does not register a callback that
    invokes an arbitrary task's `Cancel` method synchronously. The existing
    Creature task wrapper remains scheduled until the shared `RsTaskService`
    ticks it; that tick performs the task cancellation on the GameWorker and
    the normal scheduler cleanup path removes and disposes it. Token-aware
    asynchronous operations are constructed directly with the Creature token.

19. **Persistence ordering is assigned at capture.** Periodic selection,
    dehydration, and `CharacterPersistenceState.NextRevision` run in one
    scheduled GameWorker operation. Final logout does the same synchronously
    before exact removal and destruction. `CharacterPersistenceService` consumes
    the model's revision and rejects older detached models rather than deriving
    a revision from publication timing.

20. **Admission compensation is store-owned.** After a character is registered,
    rollback schedules one GameWorker task that performs exact synchronous
    `ICharacterStore.Remove`; only a successful removal permits `Destroy`, with
    no separate asynchronous remove/destroy gap.

21. **Async callers capture before await.** Region-change music work captures
    region identity and dimension first. `teletome` captures the issuing
    character's destination and display name, while `teleto` captures the
    target's location and display name after lookup. Later queued work uses only
    those values and the intended target/issuer queue boundary.

22. **Final logout does not retry conflicts.** A `Conflict` acknowledgement for
    a final logout receipt leaves the detached snapshot, terminal handoff, and
    logout ownership retained for reconciliation. `AuthenticationService` does
    not allocate a new revision, republish, reread, or resurrect the Character;
    only an explicit later persistence attempt may submit a new snapshot.

23. **Async gameplay completes after its final await.** Casket and summoning
    operations perform external lookups before consuming inventory, spawning a
    familiar, or changing statistics. After the final await they check
    cancellation and current ownership, then complete the related gameplay
    mutation without another await. Nested async services receive the
    Creature-owned cancellation token.

24. **Breaking shared contracts use coordinated deployment.** The Contacts
    session-generation/connection-identity messages and the Authorization
    exact-`AuthorizationId` revocation request are intentionally breaking
    across old and new versions. `Hagalaz.Services.GameWorld`,
    `Hagalaz.Services.Contacts`, and `Hagalaz.Services.Authorization` therefore
    form one contract-compatible release set on the shared RabbitMQ topology;
    old and new versions must not overlap for this rollout. No legacy fallback
    or compatibility adapter is added: missing authorization identity must not
    fall back to broad revocation, and missing contact identity must not weaken
    ownership fencing. Future breaking shared-contract changes require either
    the same coordinated release treatment or an explicit compatibility and
    versioning plan.

## Verification strategy

- Test explicit active/idle lookup safety, concurrent dimension allocation,
  active ownership for mutation, teardown non-resurrection, and exact
  empty-dimension removal.
- Test admission ordering, monotonic revision preservation, and the absence
  of persistence rollback on failed local registration.
- Test terminal event-handler cleanup attempts all handlers after one fails.
- Test logout state ownership, duplicate and conflicting character claims,
  exact persistence acknowledgement, explicit character lookups, NPC
  compensation, and both NPC API families.
- Test GameWorker ordering for gameplay before final snapshot, cancellation
  after exact Creature destruction, detached transport/outbox retry, periodic snapshot
  capture, replacement-instance result rejection, and overlapping connection
  admission without relying on a creature lifecycle flag.
- Run the cumulative GameWorld tests, integration tests, Contacts tests, Raido
  tests, solution build, locked restore, strict OpenSpec validation, and diff
  checks.
