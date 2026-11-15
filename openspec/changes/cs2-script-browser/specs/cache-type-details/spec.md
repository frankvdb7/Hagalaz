# Spec Delta

## ADDED Requirements

### Requirement: Browse CS2 scripts
Cache Studio SHALL list cached CS2 scripts in the existing CS2 scripts tab with numeric ID, name when present, and instruction count. The tab SHALL support pagination and numeric ID lookup.

#### Scenario: Open a script from the list
- **WHEN** an administrator selects a script on any page of the table
- **THEN** the script's detail route opens with the selected ID

#### Scenario: Look up a script by ID
- **WHEN** an administrator opens a script using numeric lookup
- **THEN** the same detail route opens with that ID

### Requirement: Inspect a CS2 script
The guarded CS2 script detail page SHALL show read-only metadata and revision-742 disassembly for the requested ID. The disassembly SHALL show instructions in cache order with their indices, numeric opcodes, and decoded operands, plus switch table keys and offsets.

#### Scenario: Open a script directly
- **WHEN** an authorized administrator opens a valid script URL directly
- **THEN** the page shows its ID, name when present, argument and local counts by type, instruction count, and switch table count
- **AND** the disassembly preserves exact operand values, including signed 64-bit constants

#### Scenario: Script is missing
- **WHEN** the requested script is absent from the cache
- **THEN** the page shows that the script could not be found

#### Scenario: Read or decode fails
- **WHEN** the cache read fails or the script cannot be decoded
- **THEN** the page shows an error and lets the administrator retry the selected ID
- **AND** the page displays no partial or stale disassembly
