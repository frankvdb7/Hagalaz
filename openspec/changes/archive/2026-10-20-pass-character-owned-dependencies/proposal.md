## Why

`Character` still stores a per-character service scope and lets itself and its owned components resolve ordinary dependencies through `IServiceProvider`. That hides the real construction contract, couples domain components to the DI container, and makes focused tests assemble a provider instead of passing the dependency under test. Issue #408 asks for this graph to use explicit constructor dependencies while preserving the existing per-character scope lifetime and gameplay behavior.

## What Changes

- Keep per-character scope creation and disposal in `CharacterFactory`, and activate `Character` from that scope with typed constructor dependencies.
- **BREAKING**: Make `Character` and its partial methods use explicit fields for the services they directly require.
- **BREAKING**: Pass typed dependencies into character-owned containers, appearance, rendering, statistics, prayers, combat, magic, music, slayer, widgets, and related components instead of resolving them through the owner.
- Add narrow typed activation paths for familiar scripts and character-NPC scripts that must be selected by runtime type.
- Pass the explicit services needed by character-created `Movement` and `CharacterCombat` instances through their constructors without redesigning the general creature hierarchy.
- Update focused component and character construction tests to provide fakes directly, and verify scope activation/lifetime behavior.
- Finish with a character-graph scan for ordinary service-provider lookups and strict OpenSpec/build/test validation.

## Capabilities

### New Capabilities

- `explicit-character-dependencies`: Character composition and character-owned components expose their required typed services through constructors while retaining scoped lifetime ownership at the factory boundary.

### Modified Capabilities

- None.

## Impact

- Production code under `Hagalaz.Services.GameWorld/Factories` and `Model/Creatures/Characters`, plus the shared constructor boundaries required by character-owned combat and movement.
- Character factory activation and many internal constructor signatures change. Persisted data, network messages, and gameplay rules do not change.
- Focused GameWorld tests that currently mock `ICharacter.ServiceProvider` or construct a scope only for hidden lookups must be updated.
- No new package, generic resolver, service bag, or second lifetime owner is introduced.

## Acceptance Criteria

- `CharacterFactory` remains the owner of the per-character scope and activates `Character` from that scope.
- `Character` and its partials do not resolve normal dependencies from `ServiceProvider`.
- Character-owned components declare their ordinary service requirements in constructors.
- Runtime familiar and character-NPC type activation goes through narrow typed activators owned by the composition boundary.
- No optional constructor parameters are added.
- Focused unit tests can instantiate migrated components with direct fakes, and existing character behavior tests pass.
- The final character composition scan finds no ordinary service-provider lookup in the covered graph.

## Non-goals and Stop Conditions

- Do not refactor every `Creature`/`Npc` subtype or remove every `IServiceProvider` use in GameWorld.
- Do not introduce a generic resolver/context facade or alter gameplay, persistence, protocol, or service lifetime policy.
- Stop and record follow-up work if a dependency belongs only to an unrelated creature/NPC graph and cannot be changed without broadening this issue.
