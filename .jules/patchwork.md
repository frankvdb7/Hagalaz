## 2025-05-14 - [GetStringInBetween Remainder Calculation Bug]
**Bug:** Characters were being skipped in the remainder string when `includeEnd` was set to true in `GetStringInBetween`.
**Cause:** The code was modifying the `iEnd` index variable when `includeEnd` was true, and then using `iEnd + strEnd.Length` to calculate the starting index for the remainder string. This resulted in skipping the delimiter's length twice.
**Fix:** Decoupled the extracted result's length from the remainder's starting index calculation.
**Prevention:** Avoid reusing and modifying index variables for different purposes (result extraction vs. remainder calculation). Added explicit unit tests for both parts of the returned array.

## 2026-02-28 - [UserStore loading state and timeout error handling]
**Bug:** The `UserStore` did not set the `loading` state to `true` when initiating a `getUserInfo` request, and timeout errors were not captured because the `timeout` operator was placed after `tapResponse`.
**Cause:** Missing `tap` operator to update state before the request, and incorrect RxJS operator ordering.
**Fix:** Added a `tap` operator before `switchMap` to set `loading: true` and reset `error`, and moved the `timeout` operator before `tapResponse`.
**Prevention:** Always ensure side-effecting state updates occur before long-running async operations and verify RxJS operator order for correct error propagation.
