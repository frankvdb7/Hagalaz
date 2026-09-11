## Ownership model

Domain objects own semantic state and behavior. Application services own
lifecycle transitions between domain objects. Stores and schedulers own shared
in-process coordination, exact-instance claims, queues, deduplication, and
shutdown. Distributed stores retain distributed fencing and reconciliation.

Domain entities are not independently synchronized lifecycle actors. No new
entity lock or lifecycle framework is introduced.

## Creature destruction

`Creature.Destroy()` is a sequential terminal domain operation. It rejects a
sequential duplicate after `IsDestroyed` is published, then detaches from the
region, exits its area, invokes `OnDestroy`, disposes its scope, and propagates
the first failure after all structural cleanup steps have been attempted.

Competing destruction ownership is not solved in `Creature`; NPC lifecycle
ownership is claimed by `NpcService`/`NpcStore`, and character destruction is
owned by the character logout workflow.

## MapRegion residency and destruction

`MapRegionService` owns active/idle residency and exact-instance removal. The
background service removes an idle exact instance from residency before calling
`DestroyAsync`, so no later canonical operation can target that instance.

`MapRegion` retains `MapRegionState` because readiness/discarding has domain
meaning. It retains only a terminal `IsDestroyed` fact for cleanup checks. It
does not own a destruction enum, mutation gate, or concurrent destruction
claim. Region mutations are performed by the serialized game tick or the
initial loader before readiness; the high-level owner removes the region before
destruction.

## Map loading

`MapRegionService` depends on `IMapRegionLoadScheduler` and requests a load
when it publishes a new canonical region. `MapRegionLoadScheduler` owns the
private channel, in-flight completion map, deduplication, and shutdown. It no
longer depends on `MapRegionService` to validate residency; `MapRegionLoader`
remains authoritative for ready/discarded/canonical checks before applying
loaded data.

All scheduler-enqueued regions have an in-flight completion. Duplicate requests
reuse that completion. Synchronous map APIs remain synchronous, while
`EnsureLoadedAsync` remains available for callers that explicitly require
readiness.

## Failed load cleanup

`MapRegionLoader` marks a failed region discarded, attempts to unregister all
NPCs registered by the attempt, and exact-removes the region. Cleanup failures
are logged as secondary diagnostics. The original load exception or cancellation
is rethrown unchanged; no `AggregateException` or exception-flattening helper is
created for cleanup.

## NPC ownership

`NpcService.Unregister` and `UnregisterAsync` first attempt exact removal from
`NpcStore`. A successful removal is the lifecycle claim and its caller then
destroys the NPC. A false removal result means another owner already removed
the exact instance or it was absent, so the caller does not destroy it. Store
locks remain inside `NpcStore`; no per-NPC lock is added.

Registration rollback remains owned by `NpcService`, which removes the exact
registered instance and best-effort destroys it after a registration callback
failure.

## Pending abort processing

`GameSessionStore` keeps the pending-abort reservation and connection-ID
reservation. Processing is a nullable in-process boolean marker under the
existing writer lock. Begin, release, and complete require exact session
identity. Failed processing releases the marker for the lease cycle to retry;
there is no token, expiry, or second retry subsystem.

## Intentionally unchanged

Distributed claim IDs, session generations, stale sign-out rejection,
authorization revocation, Contacts generation checks, persistence/outbox
semantics, character logout completion fencing, and the public sync/async NPC
API split remain at their existing infrastructure/application owners.
