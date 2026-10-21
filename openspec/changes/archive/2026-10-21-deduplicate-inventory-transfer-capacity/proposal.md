# Proposal

## Why

Bank withdrawals and reward claims independently calculate whether an item fits in a character inventory, including stack overflow and partial nonstack transfers. The duplicated decision can drift even though the inventory owns the available slots and existing stacks.

## What Changes

- Add a count-aware transfer-capacity operation beside `HasSpaceFor` on the existing read-only item-container contract and implement it in the composed item storage.
- Reuse it from bank withdrawals and reward claims while keeping each caller's messages, item transformation, and transaction behavior in place.
- Add direct tests for the inventory operation and retain the existing caller-level transfer tests.

## Scope and Acceptance Criteria

- Stackable and noted items need one free slot unless an equivalent stack already exists.
- An AlwaysStack container can extend an existing compatible stack without another free slot.
- Adding a stack that would exceed `int.MaxValue` remains rejected without reporting an inventory-full condition.
- Nonstackable transfers may be reduced to the available slot count; a full inventory rejects them.
- The capacity result indicates when free slots limited the request, so bank and reward callers preserve their current inventory-full message behavior.
- A nonpositive requested count returns false with a zero transfer count and does not report a free-slot limitation.
- Bank note conversion, transfer transactions, reward claim results, and item mutation remain unchanged.
- Direct tests cover nonpositive counts, partial and full nonstack capacity, stack slot requirements, AlwaysStack behavior, an existing stack at the integer boundary, and stack overflow.

## Non-Goals

- Do not change shop purchase rules, which use a distinct stackability check and purchase flow.
- Do not change item transfer mechanics, transaction ownership, persistence, or user messages.
- Do not introduce a separate helper service or generic transfer framework.

## Capabilities

### New Capabilities

None

### Modified Capabilities

None. This is a behavior-preserving implementation refactor.

## Impact

- `Hagalaz.Game.Abstractions/Collections/IReadOnlyItemContainer.cs`
- `Hagalaz.Game.Abstractions/Collections/ItemContainer.cs`
- `Hagalaz.Game.Abstractions/Collections/ItemContainerStorage.cs`
- `Hagalaz.Services.GameWorld/Model/Creatures/Characters/BankContainer.cs`
- `Hagalaz.Services.GameWorld/Model/Creatures/Characters/RewardContainer.cs`
- `Hagalaz.Services.GameWorld.Tests/CharacterItemTransferTests.cs`
