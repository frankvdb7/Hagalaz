# item-forge-editing Specification

## Purpose
Allow administrators to inspect, edit supported item cache fields and duplicate definitions through a responsive Item Forge grounded in the selected visual reference.

## Requirements

### Requirement: Full definition saves preserve cache semantics
Protected item editing SHALL load and save the complete stored ItemType through the codec, validate input and retain legacy unmodeled payloads. Derived status SHALL remain computed from its source fields.

#### Scenario: Save
- **WHEN** an administrator confirms valid edits
- **THEN** the complete selected definition is encoded and unmodeled legacy payloads remain intact

#### Scenario: Invalid input
- **WHEN** input has invalid IDs, display name or base value
- **THEN** the update is rejected without changing the cache

### Requirement: Duplication creates a separate exact definition
Duplication SHALL copy persisted source bytes to a new free ID while holding the existing cache write boundary and retaining correct reference mappings and checksums.

#### Scenario: Duplicate
- **WHEN** an administrator confirms duplication
- **THEN** a new definition is created without modifying or overwriting existing definitions

### Requirement: Item Forge follows the selected reference
Item Forge SHALL provide an item header, supported editor, five functional tabs, model preview, technical details and quick actions, with responsive stacking and accessible controls.

#### Scenario: Editing and navigation
- **WHEN** an administrator changes a field and navigates away
- **THEN** the existing unsaved-change flow protects the draft

#### Scenario: Failure and retry
- **WHEN** saving or duplication fails
- **THEN** the draft remains available and the UI presents the failure and retry path
