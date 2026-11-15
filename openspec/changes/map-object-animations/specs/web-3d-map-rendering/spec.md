## ADDED Requirements

### Requirement: Map object animation preview

The editor MUST preview the first declared nonnegative animation ID on each visible object's resolved definition when its sequence is supported. Playback MUST use discrete frame durations and the sequence's loop offset when valid.

#### Scenario: An animated placement is visible
- **GIVEN** a visible object with a supported cache sequence
- **WHEN** playback is enabled and its clip is available
- **THEN** the object animates at its placed position and orientation
- **AND** its selection identity and saved placement are preserved

#### Scenario: Animation is disabled or unavailable
- **WHEN** playback is disabled or the sequence cannot be loaded or posed
- **THEN** the object draws in its base pose
- **AND** other objects and terrain remain usable

#### Scenario: The selection is replaced
- **WHEN** a different region selection is loaded
- **THEN** old requests and playback no longer affect the map
