# Money pouch — revision 742

**Repository assessment:** Partial; specialized pouch mutation boundary exists. This is an implementation guide, not a claim of complete feature parity.

## Revision-specific behavior

Coin holding up to signed 32-bit count, withdraw/deposit, shop payments, coin loot, trade stakes, death safety and bank transfer.

## Existing Hagalaz entry points

- `Hagalaz.Game.Abstractions/Collections/IMoneyPouchContainer.cs`
- `Hagalaz.Game.Scripts/Characters/TradeExchange.cs`
- `Hagalaz.Game.Scripts/Minigames/DuelArena/DuelStakeExchange.cs`
- `Hagalaz.Game.Scripts/Widgets/Orbs/RunEnergyOrb.rsi.cs`
- `Hagalaz.Game.Abstractions.Tests/Collections/ItemContainerStorageTests.cs`

## Build approach

1. Use the existing money-pouch mutation boundary as single owner, including coin item ID 995. Integrate item-to-pouch conversion via checked item-container transactions, not an independent balance update followed by inventory removal.
2. Identify exact orb/widget operations and toggle semantics for 742. Validate bank/PIN, no-drop on death, allowable regions and overflow to inventory only when explicitly compatible.

## Acceptance and tests

- Exact cap is enforced; a full pouch does not silently lose change; inventory/pouch transfers conserve coins on failure; trade and stake cancellation refund; pouch remains safe across death and relog.
- Add focused tests near the cited code; check wire UI behavior with a revision 742 client/cache fixture before treating IDs or timings as exact.

## Evidence and open questions

- [Historical feature reference](https://wiki.darkan.org/Money_pouch)
- Read [implementation conventions](implementation-conventions.md) before modifying runtime state or persistence.
