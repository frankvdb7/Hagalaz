## Why

PR #492 currently distributes lifecycle coordination across domain objects,
application services, and infrastructure bridges. Creature and MapRegion each
contain synchronization intended to defend against competing infrastructure
owners, while map-load requests and abort processing expose extra coordination
types that duplicate their actual owners.

## What Changes

- Remove per-creature destruction synchronization; the owning lifecycle service
  is responsible for ensuring one destruction owner.
- Keep MapRegion domain state simple and move residency/destruction ownership to
  MapRegionService and its background lifecycle.
- Make MapRegionLoadScheduler own its channel, in-flight deduplication,
  completion, and shutdown; MapRegionService requests loads through the
  existing scheduler contract.
- Preserve the primary MapRegionLoader failure and log best-effort cleanup
  failures instead of building aggregate exception graphs.
- Make NpcStore removal the ownership claim before NpcService destroys an NPC.
- Simplify pending-abort processing in GameSessionStore to a writer-locked
  in-process processing flag with exact session identity checks.
- Update tests and OpenSpec artifacts to describe high-level lifecycle ownership
  and remove obsolete mechanisms.

## Non-goals

- No changes to distributed session claims, generation fencing, authorization,
  outbox semantics, or periodic reconciliation ownership.
- No generic lifecycle/workflow framework, entity lock registry, retry worker,
  or replacement synchronization abstraction.
- No change to public sync/async NPC APIs or script-facing API boundaries.
- No speculative scheduler parallelism; loading remains a single scheduler
  worker unless a separate measured requirement is established.

## Acceptance Criteria

- Creature contains no destruction lock, atomic destruction gate, semaphore, or
  destruction state machine; sequential duplicate destruction remains defensive.
- MapRegion no longer coordinates destruction through a low-level destruction
  enum or mutation lock; exact residency removal remains owned by
  MapRegionService, and a terminal `IsDestroyed` domain fact is preserved.
- MapRegionService submits initial loads directly to IMapRegionLoadScheduler;
  the scheduler owns its private queue and all in-flight/shutdown mechanics.
- A loader failure rethrows its original exception or cancellation after
  best-effort NPC cleanup and exact region removal, with secondary failures only
  logged.
- Concurrent NPC unregister callers cannot both own destruction because only
  the exact store removal winner destroys the NPC.
- Pending abort processing is retryable, exact-session fenced, and represented
  without a processing token.
- Existing distributed ownership guarantees remain unchanged.

## Impact

- GameWorld domain lifecycle objects and region partials.
- MapRegionService, MapRegionLoadScheduler, MapRegionLoader, NpcService, and
  GameSessionStore.
- Direct lifecycle, scheduler, loader, residency, logout, and session tests.
- Related active OpenSpec design/specification artifacts.
