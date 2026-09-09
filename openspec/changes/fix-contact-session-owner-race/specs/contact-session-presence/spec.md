## ADDED Requirements

### Requirement: Contacts presence is owned by an exact game connection

The Contacts service MUST associate every lobby or world presence with the
connection ID that established it and MUST require that same ID for sign-out.

#### Scenario: world sign-in arrives before stale lobby sign-out

- GIVEN master 42 has lobby owner `lobby-a`
- WHEN world owner `world-b` signs in and `lobby-a` signs out
- THEN master 42 remains present with owner `world-b`

#### Scenario: stale lobby sign-out arrives before world sign-in

- GIVEN master 42 has lobby owner `lobby-a`
- WHEN `lobby-a` signs out and world owner `world-b` signs in
- THEN master 42 is present with owner `world-b`

#### Scenario: stale world sign-out arrives after a newer world owner

- GIVEN master 42 has world owner `world-a`
- WHEN world owner `world-b` replaces it and `world-a` signs out
- THEN owner `world-b` remains present

#### Scenario: current owner signs out

- GIVEN master 42 has world owner `world-b`
- WHEN `world-b` signs out
- THEN master 42 is removed

### Requirement: Presence replacement does not duplicate notifications

The Contacts service MUST publish one sign-in notification for a new owner and
MUST NOT publish an intermediate sign-out notification for a replacement.
Repeated sign-in for the current owner MUST NOT publish another sign-in.

#### Scenario: duplicate sign-in for the current owner

- GIVEN master 42 already has world owner `world-b`
- WHEN `world-b` signs in again
- THEN Contacts publishes no additional sign-in or sign-out notification
