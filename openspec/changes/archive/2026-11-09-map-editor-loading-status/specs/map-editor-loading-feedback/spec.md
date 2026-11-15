# Spec Delta

## Purpose

Defines how the admin map editor presents loading, writing and read-failure states.

## ADDED Requirements

### Requirement: Loading stays inside the viewport

The map editor SHALL present loading state inside the viewport without changing the size or position of the workspace panels.

#### Scenario: First region is loading

- **WHEN** a selection is loading and no region has arrived
- **THEN** the viewport shows a centered loading state naming what is loading

#### Scenario: Scene assets are loading

- **WHEN** a region is visible and its objects or textures are still loading
- **THEN** a compact indicator shows the current stage and its counts
- **AND** the indicator does not block interaction with the viewport

#### Scenario: A load finishes quickly

- **WHEN** an incremental load finishes in under 250 ms
- **THEN** no indicator becomes visible

### Requirement: Progress moves forward within a load

The map editor MUST NOT show progress moving backward during one load, including when newly discovered work increases the total.

#### Scenario: Newly discovered work

- **WHEN** appearances reveal more textures to load
- **THEN** the progress value stays at or above its previous value

#### Scenario: A new load starts

- **WHEN** a new load replaces or follows a previous load
- **THEN** progress restarts from the new load's counts

### Requirement: Cache writes show a pending state

The map editor SHALL show a pending state while a save or world map refresh is writing to the cache.

#### Scenario: Saving changes

- **WHEN** a region save is in progress
- **THEN** the viewport states that changes are being saved

#### Scenario: Refreshing the world map

- **WHEN** a world map refresh is in progress
- **THEN** the viewport states that the world map is being updated

### Requirement: Read failures remain visible

The map editor SHALL keep failed reads visible after loading ends, with their messages and a retry action.

#### Scenario: Some assets fail

- **WHEN** a load ends with failed reads and a scene is visible
- **THEN** the viewport states how many items could not be loaded
- **AND** the user can read the messages, retry or dismiss the summary

#### Scenario: Nothing can be drawn

- **WHEN** a load ends with no region or no renderable content
- **THEN** the viewport states why and offers a retry

#### Scenario: Retrying with a draft

- **WHEN** the user retries while the draft has unsaved edits
- **THEN** the editor asks before discarding them
- **AND** declining keeps the draft and starts no load

### Requirement: Assistive technology hears state changes

The map editor MUST announce changes of loading state without announcing each counter update.

#### Scenario: Counts change during a load

- **WHEN** the settled counts change but the load continues
- **THEN** the announced status text does not change
