## 1. Implementation

- [x] 1.1 Replace the permanent NPC death cleanup and non-spawning respawn fallback waits with the existing asynchronous creature task, preserving death timing and NPC service ownership.

## 2. Regression coverage

- [x] 2.1 Add deterministic GameWorld and script regression tests proving pending NPC removal does not block the delayed death task or the non-spawning respawn fallback, and that asynchronous removal starts on a later task tick.
- [x] 2.2 Run the focused GameWorld tests, validate the OpenSpec change, inspect the diff, and restart the affected Aspire world resources for manual confirmation.
