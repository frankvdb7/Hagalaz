# Design

## Context

Production offers enter the trade container only through Inventory item transfer or MoneyPouch withdrawal. Those operations remove the offered quantity from its source before committing the offer. A later GameFrame money-pouch withdrawal or Take BoB action from the persistent Summoning Orb/Tab can otherwise use the newly free Inventory slot while the trade remains active, leaving too little capacity for a detach refund.

## Goals / Non-Goals

**Goals:** Keep cancellation on the same transactional refund path for normal cancellation and detach cleanup; prevent GameFrame pouch withdrawals and persistent Summoning Orb/Tab Take BoB actions from consuming Inventory capacity during a trade; remove the redundant forced recovery branch.

**Non-Goals:** Change how a recipient-capacity failure rejects a trade or add a second destination for returned items. Do not run unrelated script destruction callbacks before dehydration, change combat-stage duel settlement, or alter the equipment degradation timer's existing destruction-event persistence behavior.

## Decisions

- Use the existing refund transaction for every cancellation. Returning an offer restores the capacity used to fund it.
- Check `ICharacter.IsBusy()` when the GameFrame money-pouch amount is submitted, immediately before calling `MoveToInventory`. A trade marks its participants busy, so the deferred UI action cannot use capacity reserved by funded offers.
- Check `ICharacter.IsBusy()` in the Summoning Orb and Summoning Tab Take BoB handlers before calling `WithdrawAvailableToInventory`. Those persistent interfaces do not replace the trade interface or interrupt its session, so their direct familiar withdrawals must respect the same busy rule.
- Retain the `TryStageRefund` result guard. It protects transaction atomicity if an underlying domain mutation rejects; it is not a separate capacity preflight.
- Remove `forceConservation` and the Rewards/Bank selection helper because supported production offer sources already establish the return capacity.
- `CharacterLogoutService.DetachAsync` already calls `ICharacter.OnDetach()` before dehydration. That phase dispatches only `ICharacterScript.OnDetach()`; `ICharacterScript.OnDestroy()` remains part of actual character destruction. Trade and pre-combat duel scripts use the detach callback to refund their temporary offers or stakes.

## Risks / Trade-offs

- Other interactive Inventory-credit paths were checked: world actions interrupt the character, and incompatible root inventory widgets close the trade interface before their operations run. If another supported gameplay operation can consume Inventory capacity during trade, it must be prevented or included in this design.

## Migration Plan

No data migration is required. Existing active offers continue to use the normal refund operation during cancellation and detach.
