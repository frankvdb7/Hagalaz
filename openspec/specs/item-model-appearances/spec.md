# item-model-appearances Specification

## Purpose
Allow administrators to inspect an item's inventory, equipped and head model parts and their source model IDs within the existing preview.

## Requirements

### Requirement: Item appearances expose source models
The protected cache API SHALL list inventory, male/female equipped and male/female head appearances with model IDs and availability, using the same selection rules as assembly.

#### Scenario: Available and absent appearances
- **WHEN** an administrator loads appearance metadata
- **THEN** all five choices identify their selected model IDs or unavailable state

### Requirement: Appearance assembly follows item context
The model API SHALL assemble selected equipped/head parts with applicable recolors and retextures, normalize model units and apply worn offsets only to equipped appearances. Inventory SHALL retain its existing default behavior.

#### Scenario: Equipped parts
- **WHEN** an available equipped appearance is requested
- **THEN** its parts are assembled with gender-specific wear offsets and without inventory scale

#### Scenario: Head parts
- **WHEN** a head appearance is requested
- **THEN** its parts are assembled without worn offsets or inventory scale

#### Scenario: Invalid or absent choice
- **WHEN** the appearance key is invalid or its primary model is absent
- **THEN** the API returns 400 for an invalid key and 404 for an absent appearance

### Requirement: Item previews support appearance selection
Item Forge SHALL show appearance choices and source IDs, allow selection and retry, and reset to inventory when the item changes.

#### Scenario: Selection and navigation
- **WHEN** an administrator switches appearance and then opens another item
- **THEN** the selected geometry and IDs update and the next item starts with inventory selected

#### Scenario: Asset failure
- **WHEN** metadata or selected geometry fails to load
- **THEN** the preview displays the failure and offers retry through its existing request owner
