## Why

The previous NPC death fix removed blocking waits from the standard death cleanup, but NPC handles still defer removal through a queued asynchronous operation. Custom death cleanup can call the handle from the game-loop thread, so ownership is not released when the synchronous handle call returns.

## What Changes

- Add synchronous NPC registration and unregistration paths for synchronous game-world callers while retaining the existing asynchronous service APIs.
- Make the creature `OnRegistered` lifecycle callback synchronous so neither registration path needs a blocking async bridge.
- Make handle-based NPC unregister complete through the synchronous store path.
- Preserve the existing NPC service as the owner of destruction and store removal.
- Add a deterministic regression test proving a pending handle unregister does not block its caller or game tick.

## Capabilities

### New Capabilities

- `npc-handle-lifecycle`: NPC handles must not synchronously block the game-loop thread during asynchronous unregister.

### Modified Capabilities

None.

## Impact

The affected boundary is `Hagalaz.Services.GameWorld` NPC lifecycle cleanup and its GameWorld tests. Existing asynchronous loading and region cleanup remain unchanged, while synchronous handles and delayed death cleanup use the synchronous service path. No protocol, persistence schema, dependency, or generic scheduler change is required.

Acceptance criteria:

- Calling an NPC handle's unregister operation returns only after the NPC service has attempted destruction and global-store removal.
- Existing asynchronous NPC registration and unregistration callers retain their APIs and behavior.
- Focused GameWorld tests and strict OpenSpec validation pass.

Stop conditions: do not redesign the NPC service, change NPC death timing, add a second queue or worker, or modify unrelated synchronous calls outside NPC handle unregister.
