## ADDED Requirements

### Requirement: States expose only the capabilities they use
The state model MUST keep `IState` free of timing, ticking, lifecycle, and persistence members. Timed lifetime, removal callbacks, persistence, and longer-duration reapplication MUST be opt-in capabilities. Custom ticking and add callbacks are not part of this change.

#### Scenario: Passive state remains until explicitly removed
- **WHEN** a passive state is added and the creature processes any number of game ticks
- **THEN** the state remains active until its typed removal operation is called

#### Scenario: Timed state expires deterministically
- **WHEN** a timed state with one remaining tick is processed
- **THEN** its duration reaches expiry and the collection removes that exact state instance

#### Scenario: Passive state receives no timed work
- **WHEN** a creature processes a game tick with a passive state active
- **THEN** the passive state remains active and receives no duration decrement

#### Scenario: Resting uses the queried concrete state
- **WHEN** the run-energy orb starts resting
- **THEN** it adds a concrete `RestingState`, the typed resting query is true, and movement removes that same state and invokes its cleanup callback once

### Requirement: The creature-owned state collection owns transitions
Each creature MUST own one concrete state collection that stores at most one active instance per concrete state type, answers typed queries, keeps duplicate applications by default, and invokes removal callbacks exactly once for actual removals.

#### Scenario: Independent state types coexist
- **WHEN** two different state types are added to one creature
- **THEN** both remain queryable and removable independently

#### Scenario: Rejected duplicate has no false lifecycle transition
- **WHEN** a duplicate state is applied without `IKeepLongestDurationState`
- **THEN** the original remains active and no removal callback is raised for the rejected instance

#### Scenario: Longer timed reapplication transitions once
- **WHEN** a timed state implementing `IKeepLongestDurationState` is reapplied with a longer duration
- **THEN** the old instance receives one removal callback and the longer instance becomes active

#### Scenario: Removal during processing is safe
- **WHEN** a removal callback removes or replaces a state while timed processing is using a snapshot
- **THEN** the collection does not remove a newer replacement and each active instance is transitioned at most once

### Requirement: Reapplication behavior is minimal
State reapplication MUST keep the active instance by default. Timed states implementing `IKeepLongestDurationState` MAY replace it only when the new duration is longer; the collection MUST NOT compare duration for passive or other timed states.

#### Scenario: Timed keep-longest preserves the longer duration
- **WHEN** a timed state is reapplied with a shorter remaining duration
- **THEN** the existing longer-lived instance remains active without lifecycle churn

#### Scenario: Timed keep-longest accepts a longer duration
- **WHEN** a timed state implementing `IKeepLongestDurationState` is reapplied with a longer remaining duration
- **THEN** the longer-lived instance replaces the old instance and the old instance receives one removal callback

### Requirement: Game-loop processing remains synchronous and allocation-conscious
State processing MUST be synchronous, MUST avoid reflection in ordinary ticks, and MUST process only states that implement timed lifetime.

#### Scenario: State processing handles a mixed collection
- **WHEN** a creature has passive and timed states
- **THEN** only the timed states are processed and expired timed states are removed in the same game tick
