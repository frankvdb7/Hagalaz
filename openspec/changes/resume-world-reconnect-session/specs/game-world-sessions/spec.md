## MODIFIED Requirements

### Requirement: One active world session per account

The system MUST allow at most one active world session for an account in the distributed deployment. A reconnect by the authenticated owner of a retained world session MUST reuse that session instead of creating or promoting another session.

#### Scenario: A second world sign-in races with the first

- **GIVEN** two game-world instances attempt to sign in the same account
- **WHEN** both attempt to acquire ownership
- **THEN** exactly one session owns the distributed claim
- **AND** the losing attempt does not hydrate or promote a world session

#### Scenario: The retained owner reconnects with the characterized flag

- **GIVEN** an account has one retained world session and its logical connection is inside the reconnect window
- **AND** the authenticated owner sends opcode 16 with reconnect flag 1
- **WHEN** the reconnect is accepted
- **THEN** the existing session remains the only active world session
- **AND** no second session claim is created
- **AND** the existing character instance is reused
