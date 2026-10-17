# Item economy and ownership: failure-complete specification

## Core authoritative owners

- Inventory/bank/rewards and specialized pouch state live in existing `Hagalaz.Game.Abstractions/Collections/` item container mutations.
- `ItemContainerTransaction` coordinates cross-container consistency; never mutate slot storage and balance separately.
- `Hagalaz.Game.Scripts/Characters/TradeExchange.cs` and `TradingCharacterScript.cs` coordinate direct player offers.
- `Hagalaz.Game.Scripts/Minigames/DuelArena/DuelStakeExchange.cs` owns stake movement.
- `Hagalaz.Services.GameWorld/Services/CharacterLogoutService.cs` and `docs/character-persistence.md` define snapshot handoff and acknowledgement; a temporary offer not in the snapshot is **not durable**.

## Item identity and capacity model

A stack operation must consider ID, quantity, stackability, item attributes/charges, noted vs unnoted IDs, exact source instance and slot. Recipient capacity checks must run in the **same atomic operation** as transfer; a UI free-slot label is not authoritative. Two mutually competing transactions must not both consume the same source slot. Coin-pouch cap is `int.MaxValue`, but a pouch doesn't automatically turn 64-bit sums into safe inventory stacks.

### Ownership ledger

| Phase | Original owner | Temporary holder | Recipient | Expected durable conservation |
|---|---|---|---|---|
| Offer added | Player A | A's trade offer | none | A can recover full value |
| Offer edited | Player A | adjusted offer | none | both acceptance states reset |
| Both confirm | A and B | both offers | pending | one indivisible terminal decision |
| Exchange committed | B receives A items, A receives B | cleared offers | owners now swapped | both destination item/pouch states saved |
| Cancel/interrupt | each original owner | offers drained | each original owner | no pre-snapshot loss |
| Unexpected teardown | original owners | must not be discarded | original owners | durable recovery before removing character |

A possible defect in current `TradeExchange`: `CanReceive` previews pouch overflow into inventory but `Receive` calls pouch addition for full coin count. Validate and commit using identical rules; do not address this by a second compensation queue. Forced teardown into bank/rewards is also capacity-limited and may execute after a character snapshot. **Do not mark fixed without an actual recovery test through logout and save.**

## Scenario matrix for regression

- Either recipient has exactly N, N−1, N+1 free item slots; coin pouch at 0, cap−1 and cap.
- Stackable and non-stackable mixed offers; duplicate identical-looking item instances; tradeables/nontradeables and damaged/noted items.
- Concurrent accept vs offer removal, accept vs disconnect, simultaneous confirmation, repeated terminal callback.
- Both inventory and bank/reward containers full while forced logout; crash after detached snapshot but before cleanup.
- Money-pouch toggle while receiving coins; existing coins in inventory and pouch; bank PIN restrictions; death protection.
- Cancel duel while in countdown, opponent forfeits, both disconnect, winner's inventory full; ensure no double winner settlement.
- Shop stock concurrent buyers, item sellback, price checker close/double callback.
- Marketplace standing orders require **separate durable escrow** and idempotent matching/claim. They cannot use ephemeral trade offers.

## Reconciliation data requirements

The game item cache (index 19) is the primary source of note IDs, stack and equipment presentation where encoded. Hagalaz `ItemDefinition` provides `Id`, `Tradeable`, `TradePrice`, alchemy values and `Weight`. Confirm feature-specific item restrictions and current runtime item definition providers. Persist a price history only if the actual 742 marketplace requires it; do not invent a universal economy platform.

## Completion gate

End-to-end positive and rejected operations at inventory, bank, pouch, trade, duel, NPC shop, market order, item drop/reclaim and save/reload. Asset conservation must hold across both **volatile** and **persistent** state. Do not make a feature's cleanup rely on unknown spare bank capacity.
