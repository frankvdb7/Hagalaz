# Design

## Context

Production offers enter the trade container only through Inventory item transfer or MoneyPouch withdrawal. Those operations remove the offered quantity from its source before committing the offer. A failed recipient settlement rolls back and preserves both offers.

## Goals / Non-Goals

**Goals:** Keep cancellation on the same transactional refund path for normal cancellation and detach cleanup; remove the redundant forced recovery branch.

**Non-Goals:** Change how a recipient-capacity failure rejects a trade or add a second destination for returned items.

## Decisions

- Use the existing refund transaction for every cancellation. Returning an offer restores the capacity used to fund it.
- Retain the `TryStageRefund` result guard. It protects transaction atomicity if an underlying domain mutation rejects; it is not a separate capacity preflight.
- Remove `forceConservation` and the Rewards/Bank selection helper because supported production offer sources already establish the return capacity.

## Risks / Trade-offs

- If a future production path inserts items into an offer without removing them from the owner's Inventory or MoneyPouch, the capacity invariant no longer holds. Such a path must update this design before being introduced.

## Migration Plan

No data migration is required. Existing active offers continue to use the normal refund operation during cancellation and detach.
