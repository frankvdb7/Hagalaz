# cache-definition-mutations Specification

## Purpose
This capability defines how administrative edits change stored cache definitions while preserving unrelated encoded values.

## Requirements

### Requirement: Object-name edits preserve stored definition state

The Cache service MUST persist an object-name change without changing other fields in the stored object definition.

#### Scenario: Editing a gateway object with omitted fields

- **WHEN** an administrator changes the name of a stored gateway object whose interaction and support-item values are omitted
- **THEN** the new name is stored while the interaction, support-item, clipping, and solidity fields retain their original encoded state

#### Scenario: Replacement encoding is invalid

- **WHEN** the updated definition cannot be decoded after encoding
- **THEN** the service MUST NOT replace the stored archive member

### Requirement: NPC and object edits preserve other stored definition values

The Cache service MUST persist an NPC combat-level or object-name change without changing other decoded values or recognized opcode payloads in that definition.

#### Scenario: Editing an NPC with recognized payloads

- **WHEN** an administrator changes the combat level of an NPC definition containing recognized payloads, weighted model data, or single-byte non-ASCII text
- **THEN** the new combat level is stored and those other decoded values and payloads remain unchanged

#### Scenario: Editing an object with recognized payloads

- **WHEN** an administrator changes the name of an object definition containing recognized opcode payloads or weighted animation data
- **THEN** the new name is stored and those other decoded values and payloads remain unchanged
