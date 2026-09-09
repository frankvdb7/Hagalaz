## Purpose

Keep failed or canceled map-region population unpublished and make the same
region instance safe to retry.

## ADDED Requirements

### Requirement: Unpublished region loads can be reset transactionally

The system MUST provide a reset operation for a region that has not committed
loading. The reset MUST remove unpublished NPCs through the NPC service,
destroy unpublished map objects and ground items without gameplay removal
notifications, clear collision, and clear pending and prepared map-part
updates. It MUST leave the region not loaded and not destroyed.

#### Scenario: Population fails after partial NPC and object creation

- **WHEN** a region load fails before `Load()` commits readiness
- **THEN** rollback MUST attempt every unpublished NPC and map-part population
  cleanup, remove the unpublished population and collision without sending
  gameplay removal or respawn updates, and report all cleanup failures after
  the attempts complete

#### Scenario: Population cancellation is observed

- **WHEN** a region load is canceled after population has started
- **THEN** rollback MUST run with non-cancelable cleanup and the original
  cancellation MUST be rethrown

#### Scenario: Rollback also fails

- **WHEN** population fails and reset fails
- **THEN** the loader MUST surface both the original and rollback failures

The unpublished-load reset operation is an internal GameWorld loader boundary;
it is not part of the general `IMapRegion` or `IMapRegionPart` contracts.

### Requirement: Reset has explicit lifecycle preconditions

The system MUST reject reset for a loaded region, a destroyed region, or a
region containing characters. A successful reset MUST be safe to call before a
load and MUST permit a later load attempt on the same instance.

#### Scenario: Reset is requested after load commit

- **WHEN** reset is called on a loaded region
- **THEN** reset MUST reject the request without changing the committed region

#### Scenario: Reset is requested with characters present

- **WHEN** reset is called on a region containing characters
- **THEN** reset MUST reject the request without removing those characters

#### Scenario: The same instance is retried

- **WHEN** a failed load is reset and the loader is invoked again for the same
  region instance
- **THEN** the second load MUST be able to commit normally

### Requirement: NPC registration is atomic during unpublished population

NPC registration during an unpublished region load MUST either publish the NPC
to the global store, region membership, initialization lifecycle, and owned
scope together, or leave none of those resources owned by the failed attempt.

#### Scenario: NPC initialization fails before region attachment

- **WHEN** `NpcService.RegisterAsync` fails before the NPC enters its region
- **THEN** the global NPC store MUST contain no entry for that NPC
- **AND** the owned NPC scope MUST be released

#### Scenario: NPC initialization fails after region attachment

- **WHEN** NPC initialization fails after region membership was added
- **THEN** registration rollback MUST remove both the regional and global entry
- **AND** lifecycle cleanup MUST run at most once

#### Scenario: NPC store insertion fails

- **WHEN** the global NPC store rejects a new NPC
- **THEN** registration MUST report failure to its caller
- **AND** the unowned NPC MUST be destroyed without invoking registration

#### Scenario: NPC destruction fails during unregistration

- **WHEN** an NPC is globally published and its destruction callback throws
- **THEN** unregistration MUST still attempt to remove that NPC from the global
  store
- **AND** a successful removal MUST leave no global entry for the destroyed NPC
- **AND** destruction and removal failures MUST both remain observable when both
  operations fail
