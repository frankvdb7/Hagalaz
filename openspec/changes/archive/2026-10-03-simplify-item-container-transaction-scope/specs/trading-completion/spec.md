## MODIFIED Requirements

### Requirement: Committed trade operations reach a terminal state
Trade completion, refund, and recovery MUST perform checked domain mutations under their existing session gate and one disposable transaction. After successful mutation and immediately before Commit, the owner MUST assign Completed or Cancelled without an intervening external action. Commit MUST make storage irreversible before automatic publication. Existing finally cleanup MUST run exactly once even when post-commit publication throws. Such failures MUST NOT make committed value eligible for another completion or refund, and the original publication exception MUST propagate without a transaction result or public committed-state query. Unsuccessful attempts MUST dispose before refund or recovery starts.

#### Scenario: Completion publication fails after commit
- **WHEN** recipient credit and escrow clearing commit but a participant publication throws
- **THEN** the recipients retain the exchanged value, escrow stays cleared, the trade completes and cleans up once, and the publication exception propagates

#### Scenario: Refund or recovery publication fails after commit
- **WHEN** a refund or forced recovery commits but a participant publication throws
- **THEN** returned or recovered value remains committed, the trade is cancelled and cleaned up once, and the publication exception propagates
