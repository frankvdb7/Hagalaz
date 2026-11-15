# Tasks

## 1. Regression tests

- [x] 1.1 Add `DegradeTaskLifecycleTests` using a stubbed equipment container, a real `InMemoryEventBus` and manual task ticks; confirm they fail on the old implementation.

## 2. Fix

- [x] 2.1 Add `CharacterDetachingEvent` and send it from `Character.OnDetach()`.
- [x] 2.2 Update `DegradeTask` (departure check, detach save, direct cancel, destroy cleanup only).

## 3. Verification

- [x] 3.1 Run focused tests, `Hagalaz.Game.Scripts.Tests` and `Hagalaz.Services.GameWorld.Tests`, and `dotnet build Hagalaz.sln`.
- [x] 3.2 Run `openspec validate degrade-task-lifecycle --strict` and `git diff --check`.
