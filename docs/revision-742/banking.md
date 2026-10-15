# Banking and deposit boxes — revision 742

**Repository assessment:** Partial; tabbed interface exists. This is an implementation guide, not a claim of complete feature parity.

## Revision-specific behavior

Bank tabs, withdraw as note/item, placeholders, rearranging, deposit-all from inventory/equipment/familiar and coin-pouch deposit where available.

## Existing Hagalaz entry points

- `Hagalaz.Game.Scripts/Widgets/Bank/BankScreen.rsi.cs`
- `Hagalaz.Game.Scripts/Widgets/Bank/DepositBoxScreen.rsi.cs`
- `Hagalaz.Game.Abstractions/Collections/IBankContainer.cs`
- `Hagalaz.Services.GameWorld/Logic/Hydrators/ItemCollectionHydrator.cs`
- `Hagalaz.Services.GameWorld/Logic/Dehydrators/ItemCollectionDehydrator.cs`

## Build approach

1. Reuse the bank container and existing widget tab-offset logic. Verify the actual 742 interface/component/config IDs and maximum slots from revision fixtures; bank capacity is not universally fixed by historical examples.
2. Make note toggles, stack merges, placeholders and cross-container operations explicit and atomic. An open bank interface must never bypass current capacity checks; handle bank PIN rules at existing security/character boundary.

## Acceptance and tests

- Tab moves preserve order; full bank rejects excess without deleting; withdraw-as-note follows item definition; deposit-all from familiar and pouch is idempotent and restart-safe.
- Add focused tests near the cited code; check wire UI behavior with a revision 742 client/cache fixture before treating IDs or timings as exact.

## Evidence and open questions

- [Historical feature reference](https://wiki.darkan.org/Bank)
- Read [implementation conventions](implementation-conventions.md) before modifying runtime state or persistence.
