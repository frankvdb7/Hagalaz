# Spec Delta

## Purpose

Let administrators observe how cached CS2 callbacks change widget components in an interactive preview.

## ADDED Requirements

### Requirement: Run widget events in preview
Widget detail SHALL offer an interactive preview that executes cached revision-742 open, click, and manually selected callbacks in the browser using programs supplied by the Cache service.

#### Scenario: Start preview
- **WHEN** an administrator starts the interactive preview
- **THEN** it loads the widget's cached baseline and runs its open callbacks in client order

#### Scenario: Click a preview component
- **WHEN** an administrator clicks an eligible component in interactive preview
- **THEN** the matching cached click callback runs with that component as the event source
- **AND** the preview displays the callback's supported visual changes

#### Scenario: Run a manual trigger
- **WHEN** an administrator selects a cached callback and supplies any required trigger input
- **THEN** the preview runs the callback with the selected event context when its trigger filter matches
- **AND** a filtered-out trigger leaves the preview as it was and reports that it did not match

### Requirement: Keep script effects within the preview
The preview SHALL apply successful callback effects to temporary component and simulated variable state and provide a reset to the cached baseline.

#### Scenario: Continue and reset
- **WHEN** an administrator runs successive events
- **THEN** each event reads the state produced by the previous successful event
- **AND** resetting restores cached components and clears simulated variable changes

#### Scenario: Execution cannot complete
- **WHEN** a script is missing, requires unavailable client state, encounters an unsupported opcode, or exceeds its execution limit
- **THEN** the preview identifies the callback and reports the reason, with the script ID and instruction when available
- **AND** the event leaves the prior preview state intact

#### Scenario: Leave during execution
- **WHEN** an administrator resets the preview or navigates away while an event is running
- **THEN** a late result cannot overwrite the reset state or the newly opened widget
