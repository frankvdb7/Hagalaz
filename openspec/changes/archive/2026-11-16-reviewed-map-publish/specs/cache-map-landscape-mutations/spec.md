## MODIFIED Requirements

### Requirement: Save map editor edits to the cache

The editor SHALL present the exact before and after values of each stored placement change, grouped deterministically by region and kind, and require confirmation of the displayed changes before writing. It SHALL explain that requests commit independently and that the operation updates static cache data. Live spawns and the running world remain outside the operation.

#### Scenario: Review exact placement edits

- **GIVEN** changes to stored placements across one or more regions
- **WHEN** the administrator opens the review
- **THEN** it shows the object ID, local coordinates, source plane, shape and rotation for the applicable before and after placements, including additions and removals
- **AND** it discloses scale and underwater edits that will not be stored

#### Scenario: Cancelling confirmation

- **WHEN** the administrator cancels, or the reviewed batch changes before confirmation
- **THEN** the editor writes nothing and preserves the draft

#### Scenario: Scale edits are disclosed

- **GIVEN** an edit changes an object's scale
- **WHEN** the administrator opens the review
- **THEN** the review states that scale changes are not stored

#### Scenario: Underwater objects are left alone

- **GIVEN** edits to underwater objects
- **WHEN** the administrator publishes the batch
- **THEN** the review discloses that those edits are not stored and the write excludes underwater placements

#### Scenario: Successful save

- **WHEN** the administrator confirms and all reviewed writes succeed
- **THEN** the editor reloads the scene from the cache and clears the saved draft and its undo history

#### Scenario: Nothing to save

- **WHEN** there are no writable edits or the editor is already publishing
- **THEN** the save action is unavailable

### Requirement: Keep edits when a save fails

The editor SHALL track outcomes by region and kind and stop at the first failed request. It SHALL retain unsaved edits and their original version checks and reconcile confirmed saves against the cache before allowing further edits against those baselines.

#### Scenario: One region fails

- **GIVEN** an earlier terrain or landscape request succeeds
- **WHEN** a later request fails
- **THEN** the editor identifies the confirmed, failed and unattempted region/kind requests

#### Scenario: Retrying after a partial failure

- **WHEN** the administrator reviews and publishes again after partial success
- **THEN** the batch omits confirmed writes while retaining unsaved edits, including edits of a different kind in the same region

#### Scenario: Conflict or failed write verification

- **WHEN** the server reports a version conflict or cannot verify a write
- **THEN** the editor reports the failure, retains the affected edit and never treats it as confirmed or silently replaces its expected version

#### Scenario: Reload of a confirmed write fails

- **WHEN** a confirmed write cannot be reloaded
- **THEN** the editor reports the reload failure and allows read recovery without resending the confirmed write

## ADDED Requirements

### Requirement: Use region-specific keys when publishing protected landscapes

The editor SHALL use the supplied, verified XTEA keys for the target landscape during publish and reconciliation.

#### Scenario: Known keys for one region

- **GIVEN** keys for a protected landscape that has been read successfully
- **WHEN** the administrator publishes changes
- **THEN** the editor supplies those keys for that landscape and does not apply them to other regions

#### Scenario: Missing or unverifiable keys

- **WHEN** a protected landscape cannot be verified with the keys available for that region
- **THEN** the editor explains how to supply or correct its keys, preserves the edit and does not fall back to a write without keys
