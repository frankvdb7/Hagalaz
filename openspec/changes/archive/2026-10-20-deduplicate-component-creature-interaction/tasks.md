# Tasks

## 1. Characterize public handler behavior

- [x] 1.1 Add a valid-target NPC test for `OnComponentUseOnNpc` that checks the exact widget callback arguments.
- [x] 1.2 Add a closed-widget test that verifies the queued operation does not invoke the widget.
- [x] 1.3 Preserve the existing valid-target character and stale-handle tests for both handlers.

## 2. Consolidate the shared queued operation

- [x] 2.1 Extract the duplicate widget lookup, handle resolution, and widget callback into a private `ComponentHub` method while leaving type-specific lookups in their public handlers.
- [x] 2.2 Run the focused `CreatureInteractionHubTests` and verify valid calls and stale-handle rejection remain correct.

## 3. Verify the scoped refactor

- [x] 3.1 Rerun `npx jscpd --mode strict --kind exact --reporters ai Hagalaz.Services.GameWorld/Hubs/ComponentHub.cs` and confirm this handler clone is gone while the unrelated component-use clone remains.
- [x] 3.2 Run strict OpenSpec validation and `git diff --check`.
