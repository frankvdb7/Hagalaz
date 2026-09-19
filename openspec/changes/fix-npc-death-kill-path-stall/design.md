## Context

`NpcCombat` notifies the killer after the NPC death animation by invoking `ICharacter.OnTargetKilled`. Character kill-event handlers run synchronously. The Slayer handler currently blocks on an asynchronous service result. Because this callback runs on the single world game-loop thread, a pending database operation prevents movement, task ticks, cleanup, and loot processing.

## Goals / Non-Goals

**Goals:**

- Keep asynchronous service calls off synchronous kill-event execution.
- Reuse each creature's existing task scheduler as the continuation owner.
- Preserve Slayer accounting and reward behavior.
- Add deterministic tests at the blocking boundary and completion path.

**Non-Goals:**

- Redesigning synchronous event dispatch for all event types.
- Changing the public synchronous `Spawn()` behavior for existing callers.
- Changing loot eligibility or death animation timing.

## Decisions

The Slayer event handler will only enqueue an async character task and return. The task awaits the task-definition lookup, updates kill state, and awaits completion reward lookups. This follows the existing `AssignNewTask` scheduling pattern and keeps character-owned state on the character loop.

No script-facing builder API is added. The existing synchronous NPC builder contract remains unchanged; the TzHaar spawn path is outside this focused change.

## Risks / Trade-offs

- Slayer progress and completion rewards become deferred to subsequent character task ticks. This is required to avoid blocking the game loop and preserves ordering within the character-owned queue.

## Migration Plan

Deploy the GameWorld and script changes together. No data migration is required. Rolling back restores the synchronous kill-path behavior.
