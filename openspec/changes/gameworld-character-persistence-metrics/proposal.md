# Proposal

## Why

`CharacterDehydrationWorkerService` captures character snapshots and hands them to the persistence service every five minutes and once more on shutdown; `CharacterLogoutService` recovers logouts left pending. None of this is observable, so operators cannot tell whether snapshots are slow, flushes fail, or logouts are stuck in recovery. The Characters service already owns durable commit outcomes (`CharacterPersistenceMetrics`).

**Goal:** the existing `Hagalaz.Services.GameWorld` meter reports snapshot, flush hand-off and logout-recovery health, without claiming a database commit.

## What Changes

- `hagalaz.gameworld.character.snapshot.duration` histogram (`s`, tag `outcome`): the game-thread snapshot capture, excluding the wait for the scheduler.
- `hagalaz.gameworld.character.flush` counter (`{flush}`) and `hagalaz.gameworld.character.flush.duration` histogram (`s`), tags `mode=periodic|shutdown` and `outcome=success|failure|cancelled`: one sample per flush attempt, including the logout recovery pass. Any character that failed to hand off makes the attempt a failure; success means the hand-off contract held, not that the database acknowledged it.
- `hagalaz.gameworld.character.logout.recovery` counter (`{recovery}`, tag `outcome=completed|incomplete|failed|cancelled`): one sample per claimed recovery attempt. `completed` only when the logout was actually completed; a claim that leaves the logout pending (persistence not committed, session not removed, release refused) is `incomplete` and is retried.

No identifiers, hashes or revisions are tags. Behavior of the flush and recovery paths is unchanged.

## Non-goals

Reworking the persistence protocol or worker, re-emitting Characters service persistence counters, extra synchronous work in the game tick.
