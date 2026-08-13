## 1. Synchronous recurring task implementation

- [x] 1.1 Change MiningTask, FishingTask, and WoodcuttingTask reward callbacks to synchronous `Func<bool>` callbacks with no `async void` tick methods.
- [x] 1.2 Preload Mining, Fishing, and Woodcutting loot/count inputs in their existing asynchronous setup flows and keep recurring callbacks limited to in-memory game-loop mutations.

## 2. Regression coverage

- [x] 2.1 Update existing Woodcutting service coverage for the synchronous callback contract and setup-time data loading.
- [x] 2.2 Add deterministic recurring-task tests covering synchronous callback completion and non-overlapping consecutive ticks for Mining, Fishing, and Woodcutting.
- [x] 2.3 Preserve scheduler lifecycle coverage for completed-task removal and delayed respawn ordering, then run focused tests and a project build.
