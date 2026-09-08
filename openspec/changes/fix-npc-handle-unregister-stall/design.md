## Context

See proposal.md for the observed remaining stall. `NpcCombat` and the default script respawn path already enqueue asynchronous unregister work, but `NpcHandle.Unregister` still calls the same service with `.Wait()`. Handle cleanup is used by custom NPC encounters and wave controllers during synchronous game-loop callbacks.

## Goals / Non-Goals

**Goals:**

- Remove the remaining synchronous wait from handle-based NPC unregister.
- Reuse the NPC's existing asynchronous task scheduler and NPC service ownership.
- Prove both caller non-blocking behavior and eventual service invocation.

**Non-Goals:**

- Changing the `INpcHandle` public synchronous method shape in this focused fix.
- Redesigning NPC cleanup ownership, store locking, or the game-loop scheduler.
- Auditing unrelated `.Result` or `.GetAwaiter().GetResult()` calls outside handle unregister.

## Decisions

`NpcHandle.Unregister` will enqueue `_npcService.UnregisterAsync(Npc)` through the existing `ICreature.QueueTask(Func<Task>)` extension. The NPC task scheduler becomes the single progression owner: the handle only schedules work, and `NpcService` remains the authoritative owner of destruction and store removal.

Changing the interface to `Task UnregisterAsync` is rejected because it would expand the synchronous custom-script API change across all callers without being necessary to remove the block. Starting a worker or adding a queue is rejected because the NPC already owns a tick scheduler designed for this operation. Keeping `.Wait()` is rejected because a pending store lock can stall all world updates.

## Risks / Trade-offs

- [Risk] Removal becomes deferred until the NPC's next task tick. → Mitigation: this matches the existing standard NPC death cleanup and preserves the same service operation and ownership.
- [Risk] A caller may inspect the NPC immediately after scheduling and see it briefly present. → Mitigation: callers already use the handle for cleanup, and eventual removal remains guaranteed by the queued task.

## Migration Plan

Deploy the GameWorld change with the focused regression test. No data migration or caller changes are required. Rolling back restores the previous blocking behavior.
