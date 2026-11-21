# Proposal

## Why

`RsTaskService` drains, runs and removes scheduled tasks every tick, and `MapRegionLoadScheduler` owns the deduplicated in-flight region loads behind a single-reader channel. Neither reports its workload, so operators cannot see scheduled-task pressure or region-load queueing. `MapRegionLoader` already emits load count and duration.

**Goal:** the existing `Hagalaz.Services.GameWorld` meter exposes scheduled-task workload and region-load queue pressure.

## What Changes

- `RsTaskService` publishes, after every `Tick()` (including a faulted one):
  - `hagalaz.gameworld.tasks.active` gauge (`{task}`): tasks retained after the tick; pending tasks not yet drained are excluded.
  - `hagalaz.gameworld.tasks.processed` counter (`{tick}`): task `Tick()` invocations, so a recurring task counts once per invocation.
  - `hagalaz.gameworld.tasks.tick.duration` histogram (`s`): the whole `RsTaskService.Tick()` call.
- `MapRegionLoadScheduler` publishes:
  - `hagalaz.gameworld.region.load.inflight` gauge (`{load}`): unique deduplicated requests, queued or executing; shutdown drops the abandoned ones.
  - `hagalaz.gameworld.region.load.queue.duration` histogram (`s`): accepted unique enqueue to dequeue, once per request. The enqueue timestamp travels with the channel item.
- Both take an optional `TimeProvider` (defaulting to the system one) so existing constructors stay valid.
- Gauge callbacks only read published integers; no instrument carries tags.

Reused: the existing meter and `GameWorldMetrics`. Region load count and duration are not emitted again.

## Non-goals

Replacing either scheduler or the channel, rate limiting region creation, per-task histograms, tags for task type or region.

## Stop conditions

Stop if the work needs a new lock, worker, queue or cache, or changes scheduler semantics.
