## Why

The previous NPC cleanup changes removed blocking waits from unregister paths, but the post-death kill notification still executes a synchronous wait on the game-loop thread. An active Slayer task loads its definition through `.Result`, which can stop the game loop after the death animation, preventing movement and delaying queued loot.

## What Changes

- Make Slayer kill accounting and completion rewards use the character task scheduler for asynchronous service calls.
- Add deterministic regressions that keep the Slayer lookup pending and prove the synchronous kill boundary returns.

## Capabilities

### New Capabilities

- `npc-death-kill-path`: NPC death notifications must not synchronously block the game-loop thread on asynchronous Slayer service work.

### Modified Capabilities

None.

## Impact

The affected runtime boundary is post-death character notification and Slayer accounting. The existing creature task scheduler remains the only continuation mechanism; service ownership and loot ordering are preserved. No protocol, persistence schema, dependency, script-facing builder API, or generic event-bus redesign is included.

Acceptance criteria:

- A Slayer kill event returns without waiting for its asynchronous task-definition lookup.
- Slayer completion rewards perform both service lookups asynchronously without synchronous waits.
- Focused tests and strict OpenSpec validation pass.

Stop conditions: do not redesign the event bus, replace the existing creature task scheduler, add script-facing asynchronous builder APIs, change ordinary synchronous builder callers, or alter loot eligibility.
