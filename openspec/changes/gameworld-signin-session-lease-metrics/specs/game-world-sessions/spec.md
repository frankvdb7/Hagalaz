## ADDED Requirements

### Requirement: Sign-in outcomes are observable

`AuthenticationService` MUST record `hagalaz.gameworld.signin` (`{signin}`) and `hagalaz.gameworld.signin.duration` (`s`) exactly once per world or lobby sign-in request, tagged `flow` (`world` or `lobby`) and `outcome` (`success`, `rejected`, `full`, `duplicate`, `failed`, `error` or `cancelled`). A request rejected by the sign-in rate limiter MUST NOT be recorded, and recording MUST NOT change sign-in results, exceptions or compensation.

#### Scenario: Rejected credentials
- **WHEN** a sign-in is refused for invalid credentials, a disabled account or a lockout
- **THEN** one `rejected` sample is recorded

#### Scenario: Sign-in throws
- **WHEN** a sign-in throws
- **THEN** one `error` sample is recorded and the exception propagates unchanged

#### Scenario: Rate limited
- **WHEN** the sign-in rate limiter rejects the request
- **THEN** no sign-in sample is recorded

### Requirement: Session lease renewal is observable

`GameSessionLeaseService` MUST count each claim-renew attempt in `hagalaz.gameworld.session.lease.renew` (`{renewal}`) with `outcome` `success`, `lost`, `error` or `cancelled`, distinguishing a refused renewal from a thrown one.

#### Scenario: Transient claim-store error
- **WHEN** renewal throws before the known lease deadline
- **THEN** one `error` sample is recorded and no `lost` sample

### Requirement: Deferred session cleanup is observable

`GameSessionStore` MUST expose `hagalaz.gameworld.session.cleanup.pending` (`{session}`) with `kind` `claim_release` or `abort`, updated at the store's add and remove transitions and never by walking its session collections at collection time.

#### Scenario: Abort retry
- **WHEN** a pending abort is released for retry
- **THEN** it remains counted until it is removed
