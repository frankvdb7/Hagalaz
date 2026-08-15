## Why

The current creature state abstraction forces passive equipment markers, timed combat effects, activity locks, and progression flags through the same duration, tick, callback, and persistence contract. This causes passive states such as `BowEquippedState` to expire on the first game tick and makes character persistence serialize runtime-only state by accident. The change establishes explicit state capabilities while preserving the existing typed state API and character hydration pipeline.

## What Changes

- **BREAKING** Reduce `IState` to the marker contract and add opt-in capabilities for timed lifetime, custom ticking, lifecycle callbacks, and persistence.
- Extract state storage, reapplication, lifecycle dispatch, and expiration from `Creature` into a focused per-creature state collection owned by the creature.
- Represent passive states as `IState` implementations without `ITimedState` and remove `int.MaxValue` lifetime sentinels from the migrated call sites.
- Make reapplication policy explicit, preserving longest-duration behavior only for timed states and keeping passive duplicate applications without false callbacks.
- Make state dehydration opt-in through a persistent-state capability; restore only known persistent states and skip unknown or runtime-only records safely.
- Explicitly classify the durable states carried by the existing character snapshot: `DefaultSkulledState`, the three God Wars/Saradomin rope markers, and `LodestoneActivatedState`. Equipment, prayer, combat/session, activity, and NPC-derived markers remain runtime-only.
- Require every persistent state discovered at startup to declare `StateMetaDataAttribute` with a stable identifier; fail registration clearly when that invariant is violated.
- Replace the public raw `state id -> Type` lookup with a narrow registry/factory contract that creates states and resolves persistent identifiers at the activation boundary.
- Fail state registration on duplicate persistent identifiers and cover representative equipment, freeze, callback, activity, and persistence behavior with deterministic MSTest regressions.

## Capabilities

### New Capabilities

- `creature-state-lifecycle`: Explicit state lifetime, ticking, reapplication, storage, and lifecycle behavior for creatures.
- `creature-state-persistence`: Opt-in state identity, activation, hydration, and dehydration behavior.

### Modified Capabilities

- None.

## Impact

- Affected projects: `Hagalaz.Game.Abstractions`, `Hagalaz.Services.GameWorld`, `Hagalaz.Game.Scripts`, and `Hagalaz.Services.GameWorld.Tests`.
- Existing `ICreature.HasState<T>()`, `AddState`, `RemoveState<T>()`, and `GetStates()` ergonomics remain available; state implementations that currently set `TicksLeft` migrate to the timed capability.
- The existing DI scanning, startup service, and character hydration/dehydration pipeline are reused. No new package, worker, queue, persistence table, or global state registry is introduced.
- The broad catalog cleanup and moving domain-specific progression/equipment concepts into separate owning systems remain outside this focused implementation; only the representative vertical slices needed to prove the new contract are migrated here.
