# Proposal

## Why

`GameWorkerService` is the single owner of the serialized game tick, but it emits no tick metrics. Its only signal is a log warning for a completed major-pipeline overrun, and that stopwatch starts after task scheduling. Operators cannot see tick duration, per-phase cost, failed or cancelled ticks, or cadence drift.

**Goal:** the existing GameWorld worker emits bounded tick duration, outcome, overrun, phase, and start-to-start interval metrics on the existing `Hagalaz.Services.GameWorld` meter.

## What Changes

- Add five instruments to the existing meter:
  - `hagalaz.gameworld.tick.duration` (`s`): the whole attempted tick, from before the task scheduler tick to after the pipeline completes or faults.
  - `hagalaz.gameworld.tick` (`{tick}`): one count per attempted tick, tagged `outcome=success|failure|cancelled`.
  - `hagalaz.gameworld.tick.overrun` (`{tick}`): attempted ticks whose end-to-end duration exceeds `TickTimeSpan`, including ticks that throw.
  - `hagalaz.gameworld.tick.phase.duration` (`s`): tagged `phase=scheduler|major_update|client_prepare|client_update|client_reset|housekeeping`, recorded only for phases entered.
  - `hagalaz.gameworld.tick.interval` (`s`): monotonic time between successive tick start timestamps.
- `GameWorkerService` takes the registered `TimeProvider` for monotonic timestamps.

Reused: the existing meter, `GameWorldMetrics`, and `GameWorkerService`. The existing overrun log and serial phase order are unchanged.

## Non-goals

World population, region-loader queues, per-region or per-NPC histograms, changes to tick scheduling or worker lifecycle (#392, #393), dashboards.

## Acceptance criteria

- Exactly one tick count and one whole-tick duration per attempted tick, on success, failure and worker cancellation.
- Phase tags and order are asserted with a controllable time provider, with no sleeps.
- Overrun is recorded above `TickTimeSpan` and not at or below it.
- Interval measures actual starts, not the configured delay.
- No region, player, or other unbounded tags.
- No instrument enabled means no timestamps are taken.

## Stop conditions

Stop if the work needs a new lock, worker, queue, or telemetry abstraction, or a change to tick behavior.
