## ADDED Requirements

### Requirement: Single-tile traversal uses the direction-specific destination mask
`SmartPathFinder` SHALL evaluate each single-tile direction with its matching collision destination mask.

#### Scenario: Southeast traversal has a southeast-only blocker
- **WHEN** the southeast destination has `WallAllowRangeSouthEast` while its cardinal side cells are clear
- **THEN** the pathfinder SHALL not enqueue the direct southeast expansion

#### Scenario: Other single-tile directions retain their mask mapping
- **WHEN** a cardinal or diagonal destination has its matching traversal blocker, or a distinct non-matching directional blocker
- **THEN** only the matching blocker SHALL prevent the direct expansion

### Requirement: Two-tile traversal validates its incoming footprint
`SmartPathFinder` SHALL check every newly occupied size-two edge or corner tile with its direction-specific collision mask.

#### Scenario: West footprint has a lower and upper incoming cell
- **WHEN** a size-two mover expands west
- **THEN** the pathfinder SHALL inspect `(x - 1, y)` with the southwest mask and `(x - 1, y + 1)` with the northwest mask

#### Scenario: North footprint has distinct top-edge masks
- **WHEN** a size-two mover expands north
- **THEN** the pathfinder SHALL inspect the left top cell with the northwest mask and the right top cell with the northeast mask

#### Scenario: Each direction-specific size-two blocker is present
- **WHEN** any newly occupied edge or corner cell for a size-two cardinal or diagonal expansion has its matching directional blocker
- **THEN** the pathfinder SHALL not enqueue that direct expansion

### Requirement: Variable-size traversal queues its validated anchor
`SmartPathFinder` SHALL enqueue the adjacent anchor that matches the collision geometry it validated for a variable-size diagonal expansion.

#### Scenario: Southwest anchor is validated
- **WHEN** a size-three or size-four mover has only a clear southwest incoming footprint
- **THEN** the reconstructed direct route SHALL end at the southwest adjacent anchor

#### Scenario: Southeast anchor is validated
- **WHEN** a size-three or size-four mover has only a clear southeast incoming footprint
- **THEN** the reconstructed direct route SHALL end at the southeast adjacent anchor

### Requirement: Variable-size traversal remains within the local route graph
`SmartPathFinder` SHALL only enqueue a positive-X or positive-Y variable-size expansion when the resulting full footprint remains inside the 104x104 local route graph.

#### Scenario: Positive edge expansion would exceed the graph
- **WHEN** a size-three or size-four mover at the final valid east, north, north-east, north-west, or south-east anchor requests the next expansion beyond the graph
- **THEN** the pathfinder SHALL return no route and SHALL not read clipping data outside the local graph
