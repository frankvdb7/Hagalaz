# Direct player trading — revision 742

**Repository assessment:** Partial; two-party transaction and offer workflow exist. This is an implementation guide, not a claim of complete feature parity.

## Revision-specific behavior

Request/accept trade, offer items and pouch coins, acceptance reset on edit, confirmation, capacity preview, completion, cancellation and disconnect recovery.

## Existing Hagalaz entry points

- `Hagalaz.Game.Scripts/Characters/TradingCharacterScript.cs`
- `Hagalaz.Game.Scripts/Characters/TradeExchange.cs`
- `Hagalaz.Game.Scripts.Tests/Characters/TradeExchangeTests.cs`
- `Hagalaz.Game.Scripts.Tests/Characters/TradeInteractionRegressionTests.cs`
- `Hagalaz.Services.GameWorld/Services/CharacterLogoutService.cs`

## Build approach

1. Follow both characters through request, offer, confirmation, committed exchange and teardown. Centralize validation in the existing trade exchange and execute both recipients' inventory/pouch mutations in one transaction.
2. Review `CanReceive` against `Receive`: the former considers pouch overflow into inventory, while the latter currently fails if the pouch cannot accept all coins. Also verify destructor-driven recovery against logout snapshot order and full bank/rewards capacity. Do not document those paths as solved.

## Acceptance and tests

- Any offer/rule edit resets acceptance; no unilateral completion; insufficient space aborts without transfer; disconnect at every stage conserves escrow and produces durable saved state; no repeated completion.
- Add focused tests near the cited code; check wire UI behavior with a revision 742 client/cache fixture before treating IDs or timings as exact.

## Evidence and open questions

- Exact historical values and component mappings remain fixture-gated; do not replace unknowns with later-client defaults.
- Read [implementation conventions](implementation-conventions.md) before modifying runtime state or persistence.
