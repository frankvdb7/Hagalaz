# Spec Delta

## Purpose

Let Cache Studio administrators trace cached widget events to CS2 scripts and inspect script behavior through readable source and widget references.

## ADDED Requirements

### Requirement: Inspect cached widget callbacks
The widget inspector SHALL display the selected component's cached callback events, their ordered typed arguments, and the trigger filters associated with each event. Numeric script targets SHALL link to the corresponding CS2 detail page.

#### Scenario: Follow a callback
- **WHEN** an administrator selects a component with a callback containing a numeric script ID
- **THEN** the inspector displays the event, target, arguments, and any associated filters
- **AND** following the target opens the corresponding CS2 script without running the callback

#### Scenario: Callback metadata is empty or unreadable
- **WHEN** a selected component has no callbacks or its callback metadata cannot be read
- **THEN** the inspector distinguishes those states and keeps the component preview available

### Requirement: Read CS2 pseudocode
The CS2 detail page SHALL show read-only RuneScript-style pseudocode reconstructed from the selected revision-742 script and keep the original disassembly available.

#### Scenario: Inspect supported instructions
- **WHEN** the instructions have verified semantics and their operands can be resolved
- **THEN** the source shows meaningful operation names and reconstructs their expressions and control flow where supported

#### Scenario: Inspect a partially reconstructed script
- **WHEN** a region cannot be reconstructed reliably
- **THEN** the source marks that region as partial and shows the original instruction indices, opcodes, and operands for it

### Requirement: Navigate verified widget references
The CS2 source view SHALL link statically resolved operands with verified widget semantics to the addressed widget and component. The widget page SHALL select that component when the link opens.

#### Scenario: Follow a widget operand
- **WHEN** an administrator follows a verified widget reference from the source
- **THEN** the target widget opens with the referenced component selected and highlighted

#### Scenario: Destination is missing
- **WHEN** a linked script, widget, or component is absent from the cache
- **THEN** the destination identifies the missing ID and allows navigation back to the source

#### Scenario: Operand is unresolved
- **WHEN** the operand is dynamic or its widget semantics are unverified
- **THEN** the source shows the operand without a widget link
