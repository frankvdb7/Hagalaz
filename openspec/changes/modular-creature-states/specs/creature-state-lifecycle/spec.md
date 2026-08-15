## ADDED Requirements

### Requirement: States expose only the capabilities they use
The state model MUST keep `IState` free of timing, ticking, lifecycle, and persistence members. Timed lifetime, custom tick behavior, lifecycle callbacks, persistence, and reapplication policy MUST be opt-in capabilities.

#### Scenario: Passive state remains until explicitly removed
- **WHEN** a passive state is added and the creature processes any number of game ticks
- **THEN** the state remains active until its typed removal operation is called

#### Scenario: Timed state expires deterministically
- **WHEN** a timed state with one remaining tick is processed
- **THEN** its duration reaches expiry and the collection removes that exact state instance

#### Scenario: Passive state receives no tick work
- **WHEN** a creature processes a game tick with a passive state active
- **THEN** the passive state receives no timed or custom tick callback

### Requirement: The creature-owned state collection owns transitions
Each creature MUST own one state collection that stores at most one active instance per concrete state type, answers typed queries, applies the state-declared reapplication policy, and invokes lifecycle callbacks exactly once for actual transitions.

#### Scenario: Independent state types coexist
- **WHEN** two different state types are added to one creature
- **THEN** both remain queryable and removable independently

#### Scenario: Rejected duplicate has no false lifecycle transition
- **WHEN** a state declares keep-existing behavior and a duplicate is applied
- **THEN** the original remains active and neither an add nor remove callback is raised for the rejected instance

#### Scenario: Replacement transitions once
- **WHEN** a state declares replace behavior and a replacement is applied
- **THEN** the old instance receives one removal callback and the new instance receives one add callback

#### Scenario: Removal during processing is safe
- **WHEN** a state removes or replaces a state while lifecycle or tick processing is using a snapshot
- **THEN** the collection does not remove a newer replacement and each active instance is transitioned at most once

### Requirement: Reapplication policy is explicit
State reapplication MUST use the state-declared policy. Timed states MAY use keep-longest-duration behavior, but the collection MUST NOT compare duration for passive or until-removed states.

#### Scenario: Timed keep-longest preserves the longer duration
- **WHEN** a timed state is reapplied with a shorter remaining duration
- **THEN** the existing longer-lived instance remains active without lifecycle churn

#### Scenario: Timed keep-longest accepts a longer duration
- **WHEN** a timed state is reapplied with a longer remaining duration
- **THEN** the longer-lived instance replaces the old instance with the normal remove/add transition

### Requirement: Game-loop processing remains synchronous and allocation-conscious
State processing MUST be synchronous, MUST avoid reflection in ordinary ticks, and MUST process only states that implement timing or custom ticking capabilities.

#### Scenario: State processing handles a mixed collection
- **WHEN** a creature has passive, timed, and custom-tickable states
- **THEN** only the timed/custom-tickable states are processed and expired timed states are removed in the same game tick
