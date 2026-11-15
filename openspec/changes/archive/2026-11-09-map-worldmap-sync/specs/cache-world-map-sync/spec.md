# Spec Delta

## Purpose

Defines how a region's entry in the main world map is regenerated from its stored terrain and objects, and how the map editor keeps it current after saving edits.

## ADDED Requirements

### Requirement: Regenerate a region's world map entry

The Cache service SHALL regenerate a region's `main` world map entry from its stored terrain and object files, so that the base layer carries plane 0 floors and the included objects.

#### Scenario: Floors follow the terrain

- **GIVEN** a region whose stored floors changed
- **WHEN** its world map entry is refreshed
- **THEN** each tile's base layer holds that region's plane 0 underlay, overlay, shape and rotation

#### Scenario: Walls are included

- **GIVEN** a plane 0 wall of a straight, end or diagonal shape
- **WHEN** its region is refreshed
- **THEN** the object appears on its tile with its shape and rotation

#### Scenario: Blocks follow the area's coordinate mapping

- **GIVEN** a region whose block the main area shows at different coordinates
- **WHEN** it is refreshed
- **THEN** the block at its display coordinates is regenerated from that region's real map files

#### Scenario: A region without objects

- **GIVEN** an open-water region with terrain and no landscape file
- **WHEN** it is refreshed
- **THEN** its entry is regenerated from terrain alone

#### Scenario: Excluded objects stay off the map

- **GIVEN** objects the verified inclusion rules exclude
- **WHEN** their region is refreshed
- **THEN** they do not appear in the entry

### Requirement: Change nothing else in the world map

A refresh MUST NOT change any region block other than the requested ones, and MUST NOT write when the regenerated entry matches what is stored.

#### Scenario: Other regions are preserved

- **WHEN** one region is refreshed
- **THEN** every other block in the area file is byte-identical

#### Scenario: No difference

- **GIVEN** a region whose stored entry already matches its map files
- **WHEN** it is refreshed
- **THEN** the service writes nothing

### Requirement: Refuse refreshes that cannot be trusted

The Cache service MUST leave a region's world map entry unchanged and report the region when `main` does not contain it or its map files cannot be read.

#### Scenario: Region not on the world map

- **WHEN** a refresh names a region that `main` does not contain
- **THEN** the region is reported as skipped and nothing is written for it

#### Scenario: Unreadable map files

- **WHEN** a region's terrain or objects cannot be decoded
- **THEN** the region is reported as skipped and nothing is written for it

#### Scenario: Invalid request

- **WHEN** a refresh names no regions or a region ID outside 0 to 65535
- **THEN** the service responds with a validation error and writes nothing

### Requirement: Refresh the world map after saving

The map editor SHALL refresh the world map once for all regions it saved, after those saves succeed, and SHALL report the outcome separately from the map save.

#### Scenario: Successful save

- **WHEN** every edited region saves
- **THEN** the world map is refreshed once for those regions

#### Scenario: A map save fails

- **WHEN** a terrain or object save fails
- **THEN** no world map refresh is requested

#### Scenario: The refresh fails

- **GIVEN** map saves that all succeeded
- **WHEN** the world map refresh fails
- **THEN** the saved edits stay saved and the editor reports that only the world map was not updated

#### Scenario: Other areas are disclosed

- **WHEN** the administrator is asked to confirm a save
- **THEN** the confirmation states that world map areas other than the main map are not updated

### Requirement: Rebuild the world map on request

The map editor SHALL offer rebuilding the world map for the loaded regions, only after confirmation, so a failed refresh can be retried without editing again.

#### Scenario: Retrying a failed refresh

- **WHEN** the administrator confirms a rebuild
- **THEN** the loaded regions are refreshed and any skipped regions are reported

#### Scenario: Cancelling

- **WHEN** the administrator cancels the confirmation
- **THEN** no refresh is requested
