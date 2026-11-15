## ADDED Requirements

### Requirement: World population and region residency are observable

The GameWorld service MUST expose gauges `hagalaz.gameworld.characters.registered` (`{character}`), `hagalaz.gameworld.npcs.registered` (`{npc}`), `hagalaz.gameworld.regions.ready` (`{region}`) and `hagalaz.gameworld.regions.idle` (`{region}`), published by the state owners without tags.

#### Scenario: Owner rejects a transition

- **WHEN** a store or the region service rejects an add, remove, suspend or idle removal
- **THEN** no gauge changes

#### Scenario: Registered characters

- **WHEN** a character is registered and later removed
- **THEN** `characters.registered` rises by one and falls by one, and a duplicate removal does not decrement again

#### Scenario: Idle regions

- **WHEN** a region is suspended, resumed or removed from the idle store
- **THEN** `regions.idle` follows the idle store

### Requirement: Region workload is sampled per tick

The worker MUST record histogram `hagalaz.gameworld.tick.regions` (`{region}`) once per tick, and a gauge callback MUST NOT scan a store or region collection.

#### Scenario: Tick workload

- **WHEN** the worker ticks
- **THEN** `tick.regions` records the ready regions it processed, not idle or unready regions
