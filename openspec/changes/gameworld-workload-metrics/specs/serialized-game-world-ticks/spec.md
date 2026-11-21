## ADDED Requirements

### Requirement: Scheduled task workload is observable

`RsTaskService` MUST publish gauge `hagalaz.gameworld.tasks.active` (`{task}`) with the tasks it retains after each tick, counter `hagalaz.gameworld.tasks.processed` (`{tick}`) with the task `Tick()` invocations, and histogram `hagalaz.gameworld.tasks.tick.duration` (`s`) with the duration of each `RsTaskService.Tick()` call, without tags.

#### Scenario: Recurring task

- **WHEN** a task needing three ticks is ticked three times
- **THEN** `tasks.processed` rises by three in total and the task is no longer active once the service removes it

#### Scenario: Faulting task

- **WHEN** a task throws while ticking
- **THEN** its invocation is still counted and the tick duration is recorded

#### Scenario: No work

- **WHEN** no task is scheduled
- **THEN** `tasks.active` is zero

### Requirement: Region load queue pressure is observable

`MapRegionLoadScheduler` MUST publish gauge `hagalaz.gameworld.region.load.inflight` (`{load}`) with its unique queued or executing requests, and histogram `hagalaz.gameworld.region.load.queue.duration` (`s`) once per accepted unique request from enqueue to dequeue.

#### Scenario: Deduplicated waiters

- **WHEN** several callers request the same region while it is in flight
- **THEN** one in-flight load and one queue sample exist

#### Scenario: Load ends or scheduler stops

- **WHEN** a load succeeds, faults or is cancelled, or the scheduler stops with requests outstanding
- **THEN** the in-flight count returns to its prior value
