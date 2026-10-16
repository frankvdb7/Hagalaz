# Design

## Context

Production offers enter the trade container only through Inventory item transfer or MoneyPouch withdrawal. Those operations remove the offered quantity from its source before committing the offer. A later GameFrame money-pouch withdrawal can otherwise use the newly free Inventory slot while the trade remains active, leaving too little capacity for a detach refund.

## Goals / Non-Goals

**Goals:** Keep cancellation on the same transactional refund path for normal cancellation and detach cleanup; prevent the GameFrame pouch-withdrawal path from consuming Inventory capacity during a trade; remove the redundant forced recovery branch.

**Non-Goals:** Change how a recipient-capacity failure rejects a trade or add a second destination for returned items.

## Decisions

- Use the existing refund transaction for every cancellation. Returning an offer restores the capacity used to fund it.
- Check `ICharacter.IsBusy()` when the GameFrame money-pouch amount is submitted, immediately before calling `MoveToInventory`. A trade marks its participants busy, so the deferred UI action cannot use capacity reserved by funded offers.
- Retain the `TryStageRefund` result guard. It protects transaction atomicity if an underlying domain mutation rejects; it is not a separate capacity preflight.
- Remove `forceConservation` and the Rewards/Bank selection helper because supported production offer sources already establish the return capacity.

## Risks / Trade-offs

- If another supported gameplay operation can consume Inventory capacity while a trade is active, it must be prevented or included in the cancellation-capacity design before the capacity guarantee can be relied on.

## Migration Plan

No data migration is required. Existing active offers continue to use the normal refund operation during cancellation and detach.
