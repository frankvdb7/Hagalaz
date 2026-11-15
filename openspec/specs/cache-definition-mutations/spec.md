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

### Requirement: Object-name edits use representable cache strings

The Cache service MUST accept object names containing only non-null Latin-1 characters and MUST reject invalid names before modifying the archive or reference table.

#### Scenario: Persisting high-byte cache text

- **WHEN** an administrator changes an object name to a Latin-1 value containing high-byte characters
- **THEN** the new name and other existing text properties retain their characters after persistence and readback

#### Scenario: Rejecting invalid cache text

- **WHEN** an object-name request contains an embedded null or a character outside Latin-1
- **THEN** the service returns HTTP 400 and leaves the archive and reference table unchanged

### Requirement: Definition edits load the stored entry without derived state

Every administrative edit of an item, NPC, object, config definition or varp bit definition MUST start from the stored entry as decoded, without values derived after decoding, so that only the requested change is persisted.

#### Scenario: Editing an item with derived note data

- **WHEN** an administrator saves an item definition whose decoded form gains derived note, lend or bought values when loaded for the game
- **THEN** the stored entry contains only the submitted changes and none of those derived values

#### Scenario: Editing a config definition

- **WHEN** an administrator updates a config definition's default value and value type
- **THEN** the stored entry differs from its previous canonical encoding only in those two fields

### Requirement: Definition edits are verified before writing

The Cache service MUST write an edited or replacement definition only when its encoding decodes and re-encodes to identical bytes. Otherwise it MUST leave the stored entry unchanged and report a server error.

#### Scenario: Stable encoding

- **WHEN** an edited NPC definition encodes, decodes and re-encodes to identical bytes
- **THEN** the new encoding is stored

#### Scenario: Unstable encoding

- **WHEN** an edited definition's re-encoding differs from its first encoding
- **THEN** the archive member and reference table remain unchanged and the request fails with HTTP 500

#### Scenario: Sprite image replacement

- **WHEN** an administrator replaces a sprite's image
- **THEN** the replacement is verified by the same rule before it is stored

### Requirement: Definition edits that change nothing are not written

The Cache service MUST NOT write a definition entry when the edited encoding equals the canonical encoding of the stored entry, and MUST still report the edit as successful.

#### Scenario: Saving an unchanged object name

- **WHEN** an administrator sets an object's name to its current name
- **THEN** the request succeeds and the archive member and reference table version are unchanged

#### Scenario: Stored bytes differ from canonical encoding

- **WHEN** a stored item entry's bytes differ from their canonical encoding and an administrator saves the item without changes
- **THEN** no write occurs

### Requirement: Missing definitions are reported as not found

Every definition mutation route MUST respond with HTTP 404 and leave the cache unchanged when the addressed definition has no stored entry.

#### Scenario: Editing a missing NPC

- **WHEN** an administrator sets the combat level of an NPC ID with no stored entry
- **THEN** the service returns HTTP 404 and writes nothing

#### Scenario: Replacing the image of a missing sprite

- **WHEN** an administrator replaces the image of a sprite ID with no stored entry
- **THEN** the service returns HTTP 404 and writes nothing

### Requirement: Unreadable definitions are reported as unprocessable

Every definition edit route that loads the stored entry MUST respond with HTTP 422 and leave the cache unchanged when the stored entry cannot be decoded, whatever decoding error occurs.

#### Scenario: Editing an undecodable item

- **WHEN** an administrator saves an item whose stored entry fails to decode
- **THEN** the service returns HTTP 422 and writes nothing
