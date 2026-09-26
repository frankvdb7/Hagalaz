## 1. Regressions

- [x] 1.1 Add sparse slot round-trip tests for inventory, bank, equipment, familiar inventory, and rewards, plus money-pouch zero-coin round-trip coverage.
- [x] 1.2 Add extra-data, malformed slot/count, zero-coin, exact restoration, and backing-array ownership tests.
- [x] 1.3 Run the focused tests against the current implementation and record expected failures (11 GameWorld and 2 backing-array regressions failed before correction).

## 2. Correction

- [x] 2.1 Add the protected restoration boundary and make `SetItems` capacity-safe and array-owning.
- [x] 2.2 Correct all affected dehydrate/hydrate paths and retain money-pouch count semantics.
- [x] 2.3 Return the extra-data-configured item from `ItemBuilder.Build`.
- [x] 2.4 Replace the static persistence helper with protected occupied-slot enumeration and concrete DTO mapping; validate counts only during exact restoration.

## 3. Verification

- [x] 3.1 Run focused and broader relevant tests plus the full solution build.
- [x] 3.2 Validate OpenSpec strictly, pass the new-clone gate, and inspect the cumulative diff and remaining slot/index patterns.
