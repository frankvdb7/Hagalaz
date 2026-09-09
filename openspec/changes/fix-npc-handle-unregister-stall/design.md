## Context

See proposal.md for the observed remaining lifecycle mismatch. `NpcCombat` and the default script respawn path retain asynchronous service paths, while `NpcHandle.Unregister` needs a synchronous completion path when called by custom NPC encounters and wave controllers during synchronous game-loop callbacks.

## Goals / Non-Goals

**Goals:**

- Add synchronous registration/unregistration beside the existing asynchronous service APIs.
- Use the synchronous `AsyncReaderWriterLock` paths for in-memory NPC store mutation.
- Prove that handle cleanup invokes the synchronous service operation and preserves NPC service ownership.

**Non-Goals:**

- Changing the `INpcHandle` public synchronous method shape in this focused fix.
- Redesigning NPC cleanup ownership, store locking, or the game-loop scheduler.
- Auditing unrelated `.Result` or `.GetAwaiter().GetResult()` calls outside handle unregister.

## Decisions

`NpcHandle.Unregister` calls the synchronous `INpcService.Unregister` operation. `NpcService` remains the authoritative owner of destruction and store removal. `ICreature.OnRegistered` is synchronous because registration initialization is an in-memory lifecycle callback and must not require a blocking bridge on synchronous callers. `RegisterAsync` and `UnregisterAsync` remain available for asynchronous loaders and region cleanup, while `NpcStore` uses `AsyncReaderWriterLock.WriterLock()` for synchronous mutation and `WriterLockAsync()` for asynchronous mutation.

Removing the existing async methods is rejected because map loading and other asynchronous callers already use them. Starting a worker or adding a queue is rejected because the synchronous store operation is already protected by the existing lock. Hiding the synchronous operation behind a queued async callback is rejected because it violates the handle contract.

## Risks / Trade-offs

- [Risk] Registration initialization now runs inline in both service paths. → Mitigation: the service keeps transactional rollback around the callback, and asynchronous service/store APIs remain available for asynchronous callers.
- [Risk] A synchronous cleanup callback throws after destruction. → Mitigation: the service still attempts global-store removal and preserves both failures when necessary.

## Migration Plan

Deploy the GameWorld change with the focused regression test. No data migration or caller changes are required. Rolling back restores the previous blocking behavior.
