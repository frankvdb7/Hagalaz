## 1. Non-blocking handle cleanup

- [x] 1.1 Replace the NPC handle's synchronous unregister wait with the existing asynchronous creature task scheduling; verify the handle returns while a pending service operation remains incomplete.

## 2. Regression coverage and validation

- [x] 2.1 Add a deterministic GameWorld test proving the queued handle unregister invokes the existing NPC service and completes after the pending operation resolves.
- [x] 2.2 Run the focused GameWorld tests, validate the OpenSpec change strictly, inspect the diff, and restart the affected GameWorld resource for manual confirmation.
