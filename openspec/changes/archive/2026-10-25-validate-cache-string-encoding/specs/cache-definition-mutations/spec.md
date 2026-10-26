# Spec Delta

## ADDED Requirements

### Requirement: Object-name edits use representable cache strings

The Cache service MUST accept object names containing only non-null Latin-1 characters and MUST reject invalid names before modifying the archive or reference table.

#### Scenario: Persisting high-byte cache text

- **WHEN** an administrator changes an object name to a Latin-1 value containing high-byte characters
- **THEN** the new name and other existing text properties retain their characters after persistence and readback

#### Scenario: Rejecting invalid cache text

- **WHEN** an object-name request contains an embedded null or a character outside Latin-1
- **THEN** the service returns HTTP 400 and leaves the archive and reference table unchanged
