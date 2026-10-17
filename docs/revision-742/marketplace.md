# Exchange marketplace — revision 742

**Repository assessment:** No dedicated order-book feature located in audited tree. This is an implementation guide, not a claim of complete feature parity.

## Revision-specific behavior

Standing buy/sell offers, price guidance, collection box, partial fills, cancellation and retained orders. This existed by the 742 era, but cannot be inferred from an NPC shop.

## Existing Hagalaz entry points

- `Hagalaz.Game.Scripts/Areas/GrandExchange/GameObjects/UnderwallTunnel.rsobj.cs`
- `Hagalaz.Services.GameWorld/Services/ShopService.cs`
- `Hagalaz.Game.Abstractions/Collections/ItemContainerTransaction.cs`

## Build approach

1. First confirm 742 exchange screens, tradeable rules, currency limits and offer-slot counts from revision data. Reuse item and money-pouch container operations to escrow offered assets.
2. Unlike synchronous NPC shops, offers need durable ownership and matching after disconnect/restart. Design one authoritative persisted offer/escrow record with transactional delivery and idempotent collect/cancel; do not reuse temporary trade offer containers or add in-memory-only recovery.

## Acceptance and tests

- Partial and full fills preserve counts/coins; cancelled unmatched volume returns once; restart leaves open offers intact; price/cap validation and multiple concurrent matches cannot mint assets.
- Add focused tests near the cited code; check wire UI behavior with a revision 742 client/cache fixture before treating IDs or timings as exact.

## Evidence and open questions

- [Historical feature reference](https://wiki.darkan.org/Bank)
- Read [implementation conventions](implementation-conventions.md) before modifying runtime state or persistence.
