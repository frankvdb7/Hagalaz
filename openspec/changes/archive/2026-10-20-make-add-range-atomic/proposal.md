## Why

`BaseItemContainer.AddRange` can pass its capacity pre-check, add earlier items, and then fail on a later item. Callers receive `false` while the container has already changed, which violates the range operation's all-or-nothing contract.

## What Changes

- Make `HasSpaceForRange` model the complete incoming range, including stacks and slots created by earlier incoming items.
- Make `AddRange` reject the full range before mutating the real container when any item cannot fit or any combined stack would exceed `int.MaxValue`.
- Preserve the existing successful insertion rules, quantities, slot selection, notifications, and revision behavior.
- Add focused regression coverage for non-stackable counts, same-range stacking, overflow, later-item failure, and representative successful ranges.

## Capabilities

### New Capabilities

- `atomic-item-range-insertion`: Range insertion evaluates the complete range and either applies every item or leaves the container unchanged.

### Modified Capabilities

<!-- No existing capability specification covers item-container range insertion. -->

## Impact

- `Hagalaz.Game.Abstractions/Collections/BaseItemContainer.cs` will reuse the existing insertion rules against a temporary validation representation before applying the real range.
- `Hagalaz.Game.Abstractions.Tests/Collections/BaseItemContainerTests.cs` will gain focused MSTest regressions.
- No new packages, APIs, persistence, messaging, locking, transaction framework, money-pouch/shop behavior, or cross-container transfer behavior are required.

## Acceptance Criteria

- A failed `AddRange` leaves every existing slot and count unchanged.
- Incoming items are evaluated against stacks and slots created earlier in the same range.
- Non-stackable item counts consume one slot per instance.
- Combined stack overflow rejects the complete range without mutation.
- `HasSpaceForRange` agrees with `AddRange` for these cases.
- Existing successful range contents and slot usage remain unchanged.

## Stop Conditions

- Do not expand into cross-container atomic transfers from #437 or shop purchase atomicity from #449.
- Do not introduce a generic transaction, mutation-planning, command, locking, async, or container-contract redesign.
- If an unrelated caller has an independent failure-path issue after this fix, record it as follow-up work instead of expanding this change.
