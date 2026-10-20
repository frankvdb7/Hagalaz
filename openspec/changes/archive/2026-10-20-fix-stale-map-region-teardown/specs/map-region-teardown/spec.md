## Purpose

Keep stale map-region teardown and existing-object callbacks harmless without
changing legitimate region creation behavior.

## ADDED Requirements

### Requirement: Existing-state operations do not resurrect regions

Ground-item removal, game-object removal, object collision teardown, and
existing-region update operations MUST use the currently canonical region only.
They MUST NOT create a missing region, resume an idle region, or schedule a
load solely to process stale work. A missing target region MUST be treated as
an intentional no-op.

#### Scenario: Removed region receives stale teardown

- **WHEN** a region is permanently removed
- **AND** stale ground-item or game-object teardown runs
- **THEN** no replacement region MUST be created or loaded

#### Scenario: Suspended region receives stale teardown

- **WHEN** a canonical region is suspended and retains existing state
- **AND** stale teardown runs for state in that region
- **THEN** the retained state MAY be removed in place
- **AND** the region MUST remain idle

#### Scenario: Removed dynamic dimension receives stale work

- **WHEN** the dimension containing a stale object or item has been removed
- **AND** delayed teardown or existing-object mutation runs
- **THEN** the operation MUST be treated as a missing owner
- **AND** it MUST NOT throw, recreate the dimension, recreate a region, or schedule loading

### Requirement: Stale game-object work cannot affect replacement state

Collision teardown, collision mutation, and existing-object update records MUST
apply only when the exact game-object instance is still owned by the canonical
region. A stale instance MUST NOT mutate collision or queued client state for
a replacement at the same coordinates.

#### Scenario: Delayed callback targets a replacement object

- **WHEN** object A is removed with its region and replacement object B becomes
  canonical at the same location
- **AND** delayed work for A executes
- **THEN** B's collision and queued update state MUST remain unchanged

### Requirement: Legitimate creation remains create-capable

Operations that intentionally introduce world state MAY continue to use the
create-capable lookup and MUST retain their existing region creation and load
request behavior.

#### Scenario: New region is required for creation

- **WHEN** a legitimate creation operation targets an absent region
- **THEN** one canonical region MAY be created
- **AND** its normal load request MUST still be scheduled
