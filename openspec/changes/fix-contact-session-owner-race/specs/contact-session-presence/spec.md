## ADDED Requirements

### Requirement: Contacts presence is owned by an exact game-session generation

The GameWorld session owner MUST admit every lobby or world lifecycle through
the existing distributed per-account session claim and assign it a
monotonically increasing `SessionGeneration` for the account. A generation
becomes authoritative only after that account-session lifecycle is successfully
admitted. Contacts MUST associate every presence with that generation and retain
its connection ID for exact sign-out validation.

Separate GameWorld instances MUST NOT establish a newer authoritative lobby or
world lifecycle while an older lifecycle still owns the account. After exact
ownership release, a newer lifecycle MAY acquire the next generation and
replace stale presence.

The lobby handshake MUST carry the exact opaque lobby claim into a world
handshake when world selection crosses GameWorld processes. A world lifecycle
MUST replace a lobby claim only when the presented claim is the current exact
owner. Missing, stale, or unrelated handoff identities MUST be rejected
without changing the current owner.

#### Scenario: active world blocks a lobby on another GameWorld instance

- GIVEN master 42 owns world generation 10 on GameWorld A
- WHEN GameWorld B attempts a lobby login
- THEN GameWorld B does not establish generation 11 as an authoritative lobby
  presence while generation 10 remains the active owner

#### Scenario: lobby succeeds after the world releases ownership

- GIVEN master 42 owns world generation 10 on GameWorld A
- WHEN GameWorld A releases the exact world owner and GameWorld B logs in to the lobby
- THEN GameWorld B is admitted with a generation greater than 10

#### Scenario: concurrent lobby owners are rejected globally

- GIVEN GameWorld A admits a lobby lifecycle for master 42
- WHEN GameWorld B attempts a second lobby lifecycle for master 42
- THEN GameWorld B is not admitted as an authoritative owner

#### Scenario: world sign-in arrives before stale lobby sign-out

- GIVEN master 42 has lobby generation 1 on `lobby-a`
- WHEN world generation 2 on `world-b` signs in and generation 1 signs out
- THEN master 42 remains present with generation 2 on `world-b`

#### Scenario: stale lobby sign-out arrives before world sign-in

- GIVEN master 42 has lobby generation 1 on `lobby-a`
- WHEN generation 1 signs out and world generation 2 on `world-b` signs in
- THEN master 42 is present with generation 2 on `world-b`

#### Scenario: same-connection lobby-to-world promotion

- GIVEN master 42 has lobby generation 1 on connection `shared`
- WHEN world generation 2 signs in on the same connection
- THEN master 42 is present as world generation 2

#### Scenario: different-connection lobby-to-world promotion

- GIVEN master 42 has an admitted lobby lifecycle on `lobby-a`
- WHEN world initialization completes on `world-b`
- THEN the exact lobby claim is transferred atomically to world generation 2 and
  master 42 is present on `world-b`

#### Scenario: cross-GameWorld promotion with the exact handoff claim

- GIVEN GameWorld A owns lobby claim `L`
- AND GameWorld B receives the same lobby lifecycle's exact handoff claim `L`
- WHEN world initialization commits on GameWorld B
- THEN GameWorld B atomically replaces `L` with world claim `W`
- AND no other claim is replaced

#### Scenario: stale cross-GameWorld handoff is rejected

- GIVEN lobby claim `L` was current
- AND the account is now owned by claim `W`
- WHEN a world login presents stale handoff claim `L`
- THEN world admission does not commit
- AND claim `W` remains current

#### Scenario: unrelated world login cannot steal a lobby claim

- GIVEN lobby claim `L` is current
- WHEN a world login presents no proof of `L`
- THEN world admission is rejected
- AND claim `L` remains current

#### Scenario: failed lobby admission remains reconcilable

- GIVEN a lobby claim is acquired
- AND local lobby admission fails
- AND exact claim release fails
- THEN the exact failed session remains represented for reconciliation
- AND reconciliation removes only that exact owner after it is released or proven stale

If exact release of a distributed claim fails after a session has been admitted,
the retained session MUST remain represented by a local reconciliation record
until the claim is released or the claim store proves that the retained owner is
no longer current. Ordinary local lifecycle removal, including duplicate
disconnect cleanup and failed world-sign-in cleanup, MUST NOT discard that sole
record. Abort reconciliation MUST NOT replace it with a pending-abort-only
record. Only exact reconciliation may remove it.

#### Scenario: retained claim cleanup survives local removal

- GIVEN a lobby or world session has been retained after exact claim release failed
- WHEN another local cleanup path removes that session before lease reconciliation
- THEN the retained claim-cleanup record remains available
- AND lease reconciliation later removes it only after exact release succeeds or proves the owner stale

#### Scenario: stale lease renewal cannot replace deferred claim cleanup

- GIVEN a lease cycle has snapshotted an active session before its exact claim release fails
- WHEN the release failure retains that session for cleanup and the stale lease renewal also fails
- THEN lost-session abort reconciliation does not replace the retained cleanup record
- AND the exact cleanup obligation remains available for the next reconciliation
- AND a later successful exact release removes the cleanup record

#### Scenario: stale world sign-out arrives after a newer world owner

- GIVEN master 42 has world generation 1 on `world-a`
- WHEN world generation 2 replaces it and generation 1 signs out
- THEN generation 2 remains present

#### Scenario: delayed world sign-out after lobby replacement

- GIVEN master 42 has world generation 1 on `world-a`
- WHEN lobby generation 2 on `lobby-b` replaces it before generation 1 signs out
- THEN generation 2 remains present after the delayed sign-out

#### Scenario: stale world sign-in

- GIVEN master 42 has world generation 2 on `world-b`
- WHEN world generation 1 on `world-a` signs in
- THEN generation 2 remains present

#### Scenario: stale lobby sign-in

- GIVEN master 42 has world generation 2 on `world-b`
- WHEN lobby generation 1 on `lobby-a` signs in
- THEN generation 2 remains present

#### Scenario: current owner signs out

- GIVEN master 42 has world generation 2 on `world-b`
- WHEN generation 2 on `world-b` signs out
- THEN master 42 is removed

### Requirement: Presence replacement does not duplicate notifications

The Contacts service MUST publish one sign-in notification for a new owner and
MUST NOT publish an intermediate sign-out notification for a replacement.
Repeated sign-in for the current generation MUST NOT publish another sign-in.

#### Scenario: duplicate sign-in for the current owner

- GIVEN master 42 already has world generation 2 on `world-b`
- WHEN generation 2 signs in again
- THEN Contacts publishes no additional sign-in or sign-out notification

Session generation remains the Contacts causal ordering mechanism; the exact
claim ID is the GameWorld ownership fact and is not used as a replacement for
generation ordering.
