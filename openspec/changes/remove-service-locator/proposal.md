## Why

The GameWorld process still initializes a static root service locator, and two script paths resolve dependencies through that global state. This hides ownership and lifetime boundaries and leaves the deprecated migration adapter as an available architectural escape hatch.

## What Changes

- **BREAKING**: Delete `ServiceLocator` and remove its global provider initialization.
- Replace the remaining area-script and spellbook locator usages with constructor-injected typed dependencies.
- Preserve `IAreaScript.Initialize(IArea)` and the existing `SpellBookTab` lifecycle; do not add a new initialization or resolver abstraction.
- Remove stale commented locator references and project/package references that exist only for the deleted implementation.
- Add focused regression coverage for injected world options and the updated `SpellBookTab` activation path.

## Capabilities

### New Capabilities

- `explicit-dependency-wiring`: Production consumers of the removed global locator receive their required typed dependencies through existing DI construction paths.

### Modified Capabilities

- None.

## Impact

- Affected production code includes `AreaScript`, its concrete area scripts, `SpellBookTab`, and GameWorld startup.
- The `Hagalaz.DependencyInjection.Extensions` project and its sole implementation are removed from the solution and dependent project references.
- Constructor signatures for DI-created area scripts and `SpellBookTab` change; no persisted data or wire protocol changes are expected.
- Existing unrelated `IServiceProvider` composition-boundary usage remains out of scope.

## Acceptance Criteria

- No production or test source contains `ServiceLocator`, `ServiceLocator.Current`, or `SetLocatorProvider` references, including stale comments.
- Every former live locator consumer receives its required typed dependency explicitly.
- `SpellBookTab` retains one-time static teleport loading, does not call `OnOpen` from initialization, and does not introduce recursive or repeated spellbook initialization.
- Area respawn behavior continues to use the configured `WorldOptions` spawn coordinates.
- Focused tests, strict OpenSpec validation, and the solution build pass.

## Non-Goals and Stop Conditions

- Do not migrate unrelated `IServiceProvider` usage or introduce a generic service resolver/context facade.
- Do not redesign spellbook behavior, event registration, area construction contracts, or DI lifetime policy.
- Stop and record follow-up work if removing the project requires changes to an unrelated subsystem or external plugin compatibility mechanism.
