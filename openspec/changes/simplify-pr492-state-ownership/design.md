## Ownership model

Domain objects own semantic state. `GameWorkerService`, `MapRegionService`,
stores, schedulers, and lease services own the transitions they already
sequence or claim. A caller asks the owner to perform a compound operation;
it does not check a state through one API and mutate it through another.

## Decisions

1. **MapRegionService owns residency reads and removal.** `Dimension` keeps
   ordinary active and idle dictionaries plus its existing residency lock,
   but exposes no live or implicitly-cloned dictionary properties. The service
   copies values while holding each dimension lock and returns explicit
   snapshots. `TryRemoveEmptyDimension` validates the global-dimension rule,
   exact canonical instance, and both empty stores in the same critical
   section. The background service uses service-owned snapshots.

2. **Admission claims local character ownership first.** Hydration creates a
   character, then `CharacterService.AddAsync` claims the exact local instance.
   The existing `CharacterStore` implementation only performs its locked
   duplicate check and collection insertion, so no persistence behavior occurs
   before revision initialization. Persistence revision initialization follows
   successful registration. Pre-registration failures destroy the unregistered
   object directly; post-registration failures remove the exact instance,
   forget owned persistence state, destroy it, and release the session.

3. **Creature event cleanup is terminal.** `UnregisterEventHandlers` detaches
   its handler inventory before calling the event manager, attempts every
   captured handler, and throws the first failure after the pass. A destroyed
   creature cannot resume cleanup or register new handlers.

4. **Lease renewal follows store-owned state.** `GameSessionStore.FindAll`
   returns active and pending-world sessions, while moving a session into
   pending claim cleanup removes it from those stores. The lease service keeps
   the pending-cleanup list for reconciliation but does not build a redundant
   membership set for the active loop. Successful renewal continues directly;
   failed renewal falls through to the existing abort/reconcile path.

5. **Cumulative PR ownership remains unchanged.** Creature destruction stays
   terminal, MapRegion teardown releases external resources after exact
   residency removal, map loading stays scheduler-owned, NpcStore exact
   removal remains the NPC destruction claim, pending abort processing stays
   store-owned and retryable, Contacts retains generation checks, and
   `MapRegionPart._updatesLock` remains because it protects its own buffers.

## Verification strategy

- Test explicit active/idle snapshot safety and exact empty-dimension removal.
- Test admission ordering and the absence of persistence probing on failed
  local registration.
- Test terminal event-handler cleanup attempts all handlers after one fails.
- Run the cumulative GameWorld tests, integration tests, Contacts tests, Raido
  tests, solution build, locked restore, strict OpenSpec validation, and diff
  checks.
