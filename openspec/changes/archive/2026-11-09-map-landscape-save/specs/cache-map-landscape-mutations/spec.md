# Spec Delta

## Purpose

Defines how administrative edits replace a region's stored landscape placements in the cache, and how the map editor saves local edits safely.

## ADDED Requirements

### Requirement: Replace a region's landscape placements

The Cache service SHALL replace a region's stored landscape placements with a submitted list, so that reading the landscape afterwards returns exactly those placements.

#### Scenario: Saving edited placements

- **GIVEN** a region with a readable landscape archive
- **WHEN** an administrator submits a list of placements for that region
- **THEN** a later read of that landscape returns the submitted placements

#### Scenario: Object ID gaps of 32,767 or more

- **GIVEN** submitted placements whose consecutive object IDs differ by 32,767 or more
- **WHEN** they are saved
- **THEN** a later read returns the same object IDs

#### Scenario: Removing every placement

- **WHEN** an administrator submits an empty placement list
- **THEN** a later read of that landscape returns no placements

#### Scenario: Other archives are untouched

- **WHEN** a region's landscape is saved
- **THEN** its terrain archive and every other region's archives remain unchanged

### Requirement: Reject invalid landscape saves without writing

The Cache service MUST reject a landscape save and leave the cache unchanged when the region ID or any placement is out of range.

#### Scenario: Placement out of range

- **WHEN** a submitted placement has an unknown object ID, a shape above 22, a rotation above 3, a coordinate above 63 or a plane above 3
- **THEN** the service responds with a validation error and writes nothing

#### Scenario: Invalid region ID

- **WHEN** the region ID is outside 0 to 65535
- **THEN** the service responds with a validation error and writes nothing

### Requirement: Only replace landscapes that can be read

The Cache service MUST NOT overwrite a region's landscape unless its stored archive exists and decodes without keys, because unread placements would otherwise be lost.

#### Scenario: Region has no landscape archive

- **WHEN** a save targets a region with no landscape archive
- **THEN** the service responds with not found and writes nothing

#### Scenario: Stored landscape cannot be decoded

- **GIVEN** a region whose landscape is encrypted or corrupt
- **WHEN** a save targets that region
- **THEN** the service responds that the landscape cannot be replaced and writes nothing

### Requirement: Save map editor edits to the cache

The map editor SHALL offer saving local edits, only after the administrator confirms, and SHALL submit each edited region's resulting placements, then reload from the cache once all succeed.

#### Scenario: Nothing to save

- **WHEN** there are no local edits, or a save is in progress
- **THEN** the save action is unavailable

#### Scenario: Cancelling confirmation

- **WHEN** the administrator cancels the confirmation
- **THEN** nothing is written and the local edits remain

#### Scenario: Successful save

- **WHEN** the administrator confirms and every edited region saves
- **THEN** only edited regions were submitted, local edits and undo history are cleared, and the scene is reloaded from the cache

#### Scenario: Scale edits are disclosed

- **GIVEN** local edits that change an object's scale
- **WHEN** the administrator is asked to confirm
- **THEN** the confirmation states that scale changes are not stored

#### Scenario: Underwater objects are left alone

- **GIVEN** a region with underwater objects, and local edits to some of them
- **WHEN** the administrator saves
- **THEN** underwater objects are not submitted as surface placements, and the confirmation states that underwater object changes are not stored

### Requirement: Keep edits when a save fails

The map editor MUST keep local edits and report the failure when any region fails to save, and retrying MUST be safe for regions already saved.

#### Scenario: One region fails

- **GIVEN** edits in two regions
- **WHEN** the first region saves and the second fails
- **THEN** the editor reports which region failed and that the first was saved, stops submitting, and keeps the local edits

#### Scenario: Retrying after a partial failure

- **WHEN** the administrator saves again after a partial failure
- **THEN** already-saved regions receive the same placements again and their stored placements do not change
