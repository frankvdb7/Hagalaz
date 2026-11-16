## ADDED Requirements

### Requirement: A duel completes its result once

A duel SHALL have one result shared by both participants. Once a terminal entry (victory stage, kill, participant destruction or forfeit) completes it, later terminal entries from either participant SHALL NOT respawn the winner, open end screens or settle stakes again. A duel without stakes SHALL complete once like any other.

#### Scenario: Both destruction callbacks run

- **WHEN** a participant is destroyed and both participants' destroy callbacks run, in either order
- **THEN** the winner is respawned once, the winner's end screen opens once and the destroyed participant is not respawned by the duel

#### Scenario: Repeated terminal entries

- **WHEN** the victory stage, `OnKilledBy()`, a destruction callback or a forfeit repeats after the result completed
- **THEN** no respawn, end screen or stake settlement runs again

### Requirement: A rejected payout does not complete the result

If stake settlement is rejected for insufficient capacity, the duel result SHALL stay incomplete with the escrow intact, and a later terminal entry SHALL complete it once capacity exists. A typed post-commit failure SHALL count as a completed payout and SHALL NOT be retried.

#### Scenario: Insufficient capacity then space

- **WHEN** the winner cannot receive the stakes and later makes space
- **THEN** the first entry changes nothing and the next entry settles and completes once

#### Scenario: Post-commit publication failure

- **WHEN** settlement commits but publication fails with a typed post-commit exception
- **THEN** the victory completes and later entries do not repeat it
