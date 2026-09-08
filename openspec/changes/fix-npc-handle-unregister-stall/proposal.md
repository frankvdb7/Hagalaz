## Why

The previous NPC death fix removed blocking waits from the standard death cleanup, but NPC handles still synchronously wait for asynchronous unregister operations. Custom death cleanup can call the handle from the game-loop thread, so the character remains unable to move while the NPC store operation is pending.

## What Changes

- Make handle-based NPC unregister scheduling non-blocking by using the NPC's existing tick-task mechanism.
- Preserve the existing NPC service as the owner of destruction and store removal.
- Add a deterministic regression test proving a pending handle unregister does not block its caller or game tick.

## Capabilities

### New Capabilities

- `npc-handle-lifecycle`: NPC handles must not synchronously block the game-loop thread during asynchronous unregister.

### Modified Capabilities

None.

## Impact

The affected boundary is `Hagalaz.Services.GameWorld` NPC handle cleanup and its GameWorld tests. Existing custom NPC cleanup paths will use the same non-blocking task scheduler already used by standard NPC death cleanup. No protocol, persistence schema, dependency, or generic scheduler change is required.

Acceptance criteria:

- Calling an NPC handle's unregister operation returns without waiting for the asynchronous NPC service operation.
- The queued operation still invokes the existing NPC service and completes after its asynchronous task completes.
- Focused GameWorld tests and strict OpenSpec validation pass.

Stop conditions: do not redesign the NPC service, change NPC death timing, add a second queue or worker, or modify unrelated synchronous calls outside NPC handle unregister.
