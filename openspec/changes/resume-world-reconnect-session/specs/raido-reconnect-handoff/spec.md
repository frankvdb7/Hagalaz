# Raido reconnect integration boundary

## ADDED Requirements

### Requirement: GameWorld uses the existing Raido reconnect lifecycle

GameWorld MUST attach a raw replacement `ConnectionContext` to the existing
detached logical Raido connection. It MUST NOT create a temporary candidate
logical context, transfer a physical connection between logical contexts, add a
second reconnect registry or state machine, or add response-aware transport
writes. Existing #477/#488 Raido reconnect timing, attach locking, and single
winner behavior remain authoritative.

#### Scenario: Concurrent reconnects have one existing-lifecycle winner

- GIVEN two valid raw reconnect requests for one detached logical connection
- WHEN both pass GameWorld validation
- THEN the existing session claim and Raido reconnect window allow at most one
  raw connection to attach
- AND a rejected request cannot alter the target logical connection or session

### Requirement: Existing logical identity is preserved

The target MUST retain its stable logical connection ID, features, items,
handlers, and GameWorld state. The replacement physical connection ID MUST NOT
be rewritten as the logical ID.

#### Scenario: Raw reconnect attaches to the target

- GIVEN an exact existing GameWorld session and detached Raido logical target
- WHEN the raw replacement connection is accepted
- THEN the target logical connection resumes with the same identity
- AND no candidate logical connection exists
