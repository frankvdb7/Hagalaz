# Spec Delta

## MODIFIED Requirements

### Requirement: Committed trade operations reach a terminal state
Trade completion and refund MUST perform checked domain mutations under their existing session gate and one disposable transaction. The session MUST own state changes through intent-specific transitions: completion enters `Completing` from `Active`, successful completion enters `Completed`, failed completion returns to `Active`, and cancellation enters `Cancelled` from `Active` only after refund mutations succeed. Immediately before Commit, the owner MUST transition to `Completed` or `Cancelled` without an intervening external action. Commit MUST make storage irreversible and release physical locks while retaining transaction bindings. The trade owner MUST explicitly publish each affected domain in its established order before disposing the committed scope. Existing finally cleanup MUST run exactly once even when publication throws. A transaction-owned completion failure MUST propagate as `ItemContainerPostCommitCompletionException`, and a publication failure MUST propagate as `ItemContainerPostCommitPublicationException`; each preserves the exact original callback exception as `InnerException`. Such failures MUST NOT make committed value eligible for another completion or refund. Publications explicitly invoked by the trade owner after Commit retain their existing exception behavior. Unsuccessful attempts MUST dispose before another operation starts.

#### Scenario: Completion publication fails after commit
- **WHEN** recipient credit and escrow clearing commit but an explicit domain publisher throws
- **THEN** the recipients retain the exchanged value, escrow stays cleared, the trade completes and cleans up once, remaining required publishers follow the established attempt policy, and the publication exception propagates

#### Scenario: Refund publication fails after commit
- **WHEN** an owner refund commits but an explicit domain publisher throws
- **THEN** returned value remains committed, the trade is cancelled and cleaned up once, remaining required publishers follow the established attempt policy, and the publication exception propagates

#### Scenario: Transaction-owned publication fails after commit
- **WHEN** a transaction-owned publisher throws after the trade's storage changes commit
- **THEN** the trade remains terminal and is cleaned up exactly once, and `ItemContainerPostCommitPublicationException` preserves the exact publisher exception as `InnerException`

#### Scenario: Trade-owned publication fails after Commit returns
- **WHEN** an explicit trade-owned publication throws after transaction Commit has returned
- **THEN** the trade remains terminal and is cleaned up once, and the publisher's exception follows its existing direct propagation behavior
