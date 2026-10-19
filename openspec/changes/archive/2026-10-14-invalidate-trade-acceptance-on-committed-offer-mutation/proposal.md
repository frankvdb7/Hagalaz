# Proposal

## Why

Trade acceptance currently follows offer publication. A publisher can throw after storage has committed, leaving the visible offer contents changed while previously recorded acceptance still appears valid. Acceptance must track committed offer contents rather than successful notification delivery.

## What Changes

- Make offer acceptance revisions reflect committed storage mutations, including mutations whose later publication fails.
- Preserve existing trade-session serialization, transaction ownership, and publication behavior.
- Add a regression that injects an Inventory publication failure after an offer mutation and proves stale acceptance cannot complete the trade.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `trading-completion`: acceptance must be invalidated by a committed offer mutation independently of publication success.
- `item-container-storage`: trade acceptance revisions must reflect committed storage changes rather than publication success.

## Impact

The change is limited to the item-container change tracker, trade-offer revision source, the trade regression suite, and the trading/item-container specifications. It reuses the existing storage mutation revision, transaction lifecycle, and shared trade gate. No dependencies or transaction abstractions are added.

## Non-goals

- Redesigning transaction commit or publication ordering.
- Retrying, compensating, or suppressing publisher failures.
- Changing the trade UI protocol or adding generic result/error handling.

## Acceptance Criteria

- A committed offer mutation advances the revision used to validate acceptance even when another participant's publisher throws first.
- A subsequent final acceptance cannot complete the trade against the stale acceptance snapshot.
- A rolled-back mutation does not appear as a committed revision change.
- Existing offer publication and trade cleanup behavior remains intact.

## Stop Conditions

- Stop if the existing storage mutation revision cannot be read safely after transaction completion without adding broader mutation or transaction APIs.
- Stop if the regression requires changing transaction publication guarantees or the trade session state machine.
