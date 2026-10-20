# Proposal

## Why

`ComponentHub.OnComponentUseOnCharacter` and `OnComponentUseOnNpc` repeat the same queued widget lookup and generational creature-handle resolution. Keeping those steps in both handlers risks their deferred interaction behavior diverging.

## What Changes

- Extract the shared queued component-use operation into a private method on the existing `ComponentHub`.
- Add direct public-handler coverage for a valid NPC target while retaining existing character success and stale-handle tests.

## Scope and Acceptance Criteria

- Only the character/NPC component-use handlers in `ComponentHub` and their focused public-handler tests change.
- Both handlers keep their existing type-specific target lookup before queuing and pass the captured `EntityHandle<ICreature>` to the shared operation.
- At task execution, the operation still requires the widget to be open and resolves the captured handle through the existing `IEntityService` before invoking `OnComponentUsedOnCreature`.
- A closed widget or stale handle still produces no component-use invocation.
- Character and NPC valid-target tests verify the same component data reaches the widget.
- If consolidation requires a new class, interface, static helper, state owner, or behavior change, stop and propose a separate change.

## Non-Goals

- Do not refactor the ground-item/game-object component-use handlers or other exact clones in this change.
- Do not change lookup order, task scheduling, widget semantics, message fields, or entity-handle ownership.
- Do not add public APIs, new abstractions, dependencies, or static helpers.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

None. This is an implementation-only refactor; existing component-use behavior remains the contract.

## Impact

- `Hagalaz.Services.GameWorld/Hubs/ComponentHub.cs`
- `Hagalaz.Services.GameWorld.Tests/CreatureInteractionHubTests.cs`

No API signature, dependency, registration, or runtime topology changes are required.
