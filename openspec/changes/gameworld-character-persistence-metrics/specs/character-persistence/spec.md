## ADDED Requirements

### Requirement: Character snapshot and flush health is observable

`CharacterDehydrationWorkerService` MUST record `hagalaz.gameworld.character.snapshot.duration` (`s`, tag `outcome`) per snapshot capture, and `hagalaz.gameworld.character.flush` (`{flush}`) plus `hagalaz.gameworld.character.flush.duration` (`s`) once per flush attempt, tagged `mode` (`periodic` or `shutdown`) and `outcome` (`success`, `failure` or `cancelled`). Success MUST NOT imply a database commit, and no instrument carries identifier, hash or revision tags.

#### Scenario: Successful periodic flush

- **WHEN** every character is handed off during a periodic flush
- **THEN** one `periodic`/`success` flush sample and one successful snapshot sample are recorded

#### Scenario: Partial hand-off failure

- **WHEN** one character fails to hand off, in either mode
- **THEN** the flush is recorded as `failure`

#### Scenario: Cancelled shutdown flush

- **WHEN** the shutdown deadline cancels the flush
- **THEN** one `shutdown`/`cancelled` flush sample is recorded

### Requirement: Logout recovery outcomes are observable

`CharacterLogoutService` MUST count each claimed recovery attempt in `hagalaz.gameworld.character.logout.recovery` (`{recovery}`) with `outcome` `completed`, `incomplete`, `failed` or `cancelled`. `completed` MUST only be recorded when the pending logout was completed.

#### Scenario: Recovery leaves the logout pending

- **WHEN** persistence is not committed or the session cannot be removed
- **THEN** the attempt is `incomplete` and not `completed`

#### Scenario: Nothing to recover

- **WHEN** no pending logout is recoverable
- **THEN** no sample is recorded
