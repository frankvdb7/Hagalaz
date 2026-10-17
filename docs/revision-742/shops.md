# NPC shops and price checking — revision 742

**Repository assessment:** Partial; shop definition service and widgets exist. This is an implementation guide, not a claim of complete feature parity.

## Revision-specific behavior

Shop stock, buy/sell price, general vs specialist stock, restocking, coin pouch payment and price checker.

## Existing Hagalaz entry points

- `Hagalaz.Services.GameWorld/Services/ShopService.cs`
- `Hagalaz.Game.Scripts/Widgets/Shop/ShopScreenScript.cs`
- `Hagalaz.Game.Scripts/Widgets/PriceCheck/PriceChecker.rsi.cs`
- `Hagalaz.Game.Abstractions/Features/Shops/IShop.cs`
- `Hagalaz.Game.Scripts.Tests/Widgets/PriceCheck/PriceCheckerTransferTests.cs`

## Build approach

1. Keep stock in existing shop service/container; obtain prices and stock policies from definitions. Process stock, inventory and pouch changes as one checked operation; preserve each shop's buy/sell restrictions.
2. Price checker must not duplicate items, especially on interface close. Verify stock replenishment scheduling and concurrent buyers without creating an unrelated global economy coordinator.

## Acceptance and tests

- Full recipient, insufficient funds and out-of-stock requests are no-ops; exact pricing and stock deltas; stable buy/sell quantities across concurrent requests and reconnect.
- Add focused tests near the cited code; check wire UI behavior with a revision 742 client/cache fixture before treating IDs or timings as exact.

## Evidence and open questions

- Exact historical values and component mappings remain fixture-gated; do not replace unknowns with later-client defaults.
- Read [implementation conventions](implementation-conventions.md) before modifying runtime state or persistence.

## P1 shop interface integration

[Client/server UI dossier](reference/deep-client-interface-cs2-protocol-integration.md) and [call-site catalog](data/catalogs/interface-cs2-var-state-source-calls.csv) map shop configuration and client script ID **149** requests, component actions and item-container updates. Verify index-3/12 definitions and [case P28](reference/client-protocol-integration-verification.md) with real buy/sell state mutation; do not infer exact script signature from number alone.
