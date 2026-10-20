## Why

Killing an NPC can stop movement and world updates while the client remains connected. The NPC death cleanup path synchronously waits for asynchronous removal on the single game-loop thread, so an incomplete store operation can stall every subsequent game tick.

## What Changes

- Schedule NPC removal through the existing asynchronous creature-task mechanism without blocking the game-loop thread, including the fallback when a respawning NPC can no longer spawn.
- Preserve the existing death animation, loot, visibility, respawn, and permanent-removal timing.
- Add a regression test proving delayed permanent removal remains non-blocking while the removal operation is pending.

## Capabilities

### New Capabilities

- `npc-death-lifecycle`: NPC death cleanup must not block world updates.

### Modified Capabilities

None.

## Impact

The affected boundary is the GameWorld NPC combat/death lifecycle and its existing tick-task scheduler. No protocol, persistence schema, dependency, or client change is required.

Acceptance criteria:

- A pending asynchronous NPC removal does not prevent the death task from completing its tick.
- A pending asynchronous removal from the respawn fallback does not block the world update thread.
- The removal operation is still started after the configured death-render delay and completes through the existing NPC service.
- Existing respawning NPC behavior remains unchanged.
- Focused GameWorld tests and OpenSpec validation pass.

Stop conditions: do not change generic scheduler semantics, combat rules, loot generation, or the unrelated MySQL inbox-cleanup error in this change.
