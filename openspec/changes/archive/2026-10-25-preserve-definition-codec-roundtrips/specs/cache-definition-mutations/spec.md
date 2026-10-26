# Spec Delta

## Purpose

This capability defines how administrative edits change stored cache definitions while preserving unrelated encoded values.

## ADDED Requirements

### Requirement: NPC and object edits preserve other stored definition values

The Cache service MUST persist an NPC combat-level or object-name change without changing other decoded values or recognized opcode payloads in that definition.

#### Scenario: Editing an NPC with recognized payloads

- **WHEN** an administrator changes the combat level of an NPC definition containing recognized payloads, weighted model data, or single-byte non-ASCII text
- **THEN** the new combat level is stored and those other decoded values and payloads remain unchanged

#### Scenario: Editing an object with recognized payloads

- **WHEN** an administrator changes the name of an object definition containing recognized opcode payloads or weighted animation data
- **THEN** the new name is stored and those other decoded values and payloads remain unchanged
