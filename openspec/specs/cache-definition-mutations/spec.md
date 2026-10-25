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
